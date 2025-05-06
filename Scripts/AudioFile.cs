using System;
using Godot;
using System.IO;
using System.Text;

public partial class AudioFile : Button
{
    public static AudioFile LastPlayedSound;
    
    private Label _label;
    private ProgressBar _progress;
    
    private string _audioPath;
    private AudioStreamWav _streamWav;

    private StringBuilder _msg;

    private float[] _waveformData;

    public void Initialize(string audioPath)
    {
        _audioPath = audioPath;
        string filename = Path.GetFileName(_audioPath);
        
        if (File.Exists(_audioPath))
        {
            try
            {
                _streamWav = LoadWavFile(_audioPath);
                _label?.SetText(filename);
                ExtractWaveform(_streamWav);
                QueueRedraw(); // trigger _Draw()
            }
            catch (Exception e)
            {
                _label?.SetText($"[Failed] {filename}");
                _msg ??= new StringBuilder();
                _msg.AppendLine(e.Message);
                WarningDialogs.Instance.DisplayMessage("Warning", _msg.ToString());
            }
        }
    }

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        _progress = GetNode<ProgressBar>("ProgressBar");
        Pressed += Play;
    }

    public override void _ExitTree()
    {
        Pressed -= Play;
    }

    public override void _Process(double delta)
    {
        if (LastPlayedSound != this)
        {
            _progress.SetValue(0);
            return;
        }
        
        AudioStreamPlayer stream = AudioFileManager.Instance.StreamPlayer;
        if (stream.Stream != null)
        {
            float playbackPosition = stream.GetPlaybackPosition();
            _progress.SetValue(playbackPosition / stream.Stream.GetLength());
        }
    }

    public override void _Draw()
    {
        if (_waveformData == null)
            return;

        float midY = Size.Y / 2;
        Color color = Colors.DarkSlateGray;

        for (int x = 0; x < _waveformData.Length; x++)
        {
            float amp = _waveformData[x] * (Size.Y / 2);
            DrawLine(new Vector2(x, midY - amp), new Vector2(x, midY + amp), color, Size.X);
        }
    }

    public void Play()
    {
        LastPlayedSound = this;
        AudioFileManager.Instance.StreamPlayer.SetStream(_streamWav);
        AudioFileManager.Instance.StreamPlayer.Play();
    }

    public void Stop()
    {
        AudioFileManager.Instance.StreamPlayer.Stop();
    }

    public void Pause()
    {
        bool isPaused = AudioFileManager.Instance.StreamPlayer.GetStreamPaused();
        AudioFileManager.Instance.StreamPlayer.SetStreamPaused(!isPaused);
    }

    public void SetLoop(bool value)
    {
        // Not implemented
    }

    private void ExtractWaveform(AudioStreamWav stream)
    {
        byte[] data = stream.Data;
        int sampleCount = data.Length / 2;
        int channels = stream.Stereo ? 2 : 1;

        short[] samples = new short[sampleCount];
        for (int i = 0; i < sampleCount; i++)
            samples[i] = BitConverter.ToInt16(data, i * 2);

        float[] monoSamples = new float[sampleCount / channels];
        for (int i = 0; i < monoSamples.Length; i++)
        {
            if (channels == 2)
                monoSamples[i] = (samples[i * 2] + samples[i * 2 + 1]) / 2f / short.MaxValue;
            else
                monoSamples[i] = samples[i] / (float)short.MaxValue;
        }

        int width = Mathf.Max((int)Size.X, 1);
        _waveformData = new float[width];
        int samplesPerPixel = Mathf.Max(monoSamples.Length / width, 1);

        for (int x = 0; x < width; x++)
        {
            float max = 0;
            int start = x * samplesPerPixel;
            int end = Mathf.Min(start + samplesPerPixel, monoSamples.Length);
            for (int i = start; i < end; i++)
                max = Mathf.Max(max, Mathf.Abs(monoSamples[i]));

            _waveformData[x] = max;
        }
    }

    private AudioStreamWav LoadWavFile(string path)
    {
        _msg = new StringBuilder();
        _msg.AppendLine($"File '{Path.GetFullPath(path)}'");

        using (var reader = new BinaryReader(File.OpenRead(path)))
        {
            string riff = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (riff != "RIFF") throw new Exception("Not a valid WAV file (missing RIFF header)");

            reader.ReadInt32();
            string wave = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (wave != "WAVE") throw new Exception("Not a valid WAV file (missing WAVE header)");

            short numChannels = 0;
            int sampleRate = 0;
            short bitsPerSample = 0;
            byte[] pcmData = null;

            bool fmtFound = false;
            bool dataFound = false;

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                string chunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                int chunkSize = reader.ReadInt32();

                if (chunkId == "fmt ")
                {
                    fmtFound = true;
                    short format = reader.ReadInt16();
                    numChannels = reader.ReadInt16();
                    sampleRate = reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadInt16();
                    bitsPerSample = reader.ReadInt16();

                    if (format != 1) throw new Exception("Unsupported format: only PCM supported.");
                    if (bitsPerSample != 16) throw new Exception("Unsupported bit depth: only 16-bit supported.");

                    int remaining = chunkSize - 16;
                    if (remaining > 0) reader.ReadBytes(remaining);
                }
                else if (chunkId == "data")
                {
                    dataFound = true;
                    pcmData = reader.ReadBytes(chunkSize);
                }
                else
                {
                    reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                }
            }

            if (!fmtFound) throw new Exception("Missing 'fmt ' chunk.");
            if (!dataFound || pcmData == null) throw new Exception("Missing 'data' chunk.");

            return new AudioStreamWav
            {
                Format = AudioStreamWav.FormatEnum.Format16Bits,
                MixRate = sampleRate,
                Stereo = (numChannels == 2),
                Data = pcmData,
                LoopMode = AudioStreamWav.LoopModeEnum.Disabled
            };
        }
    }
}
