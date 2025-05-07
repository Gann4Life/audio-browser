using Godot;
using System;
using System.IO;

public partial class Main : Control
{
    [Export] private FileDialog FileBrowser;
    [Export] private Button BtnBrowse;

    private string[] COMPATIBLE_FORMATS = new[] { "mp3", "wav" };
    
    public override void _Ready()
    {
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
        
        FileBrowser.DirSelected += FileBrowserOnDirSelected;
        BtnBrowse.Pressed += () => FileBrowser.Show();
    }

    private void FileBrowserOnDirSelected(string dir)
    {
        if (Path.Exists(dir))
        {
            AudioFileManager.Instance.ClearList();
            if (PlaybackSettings.Instance.Recursive)
                LoadRecursively(dir);
            else
                LoadSimple(dir);
        }
    }

    private void LoadSimple(string path)
    {
        GD.Print("LOADING SIMPLE");
        string[] files = Directory.GetFiles(path);
        foreach (string file in files)
        {
            if (file.EndsWith("wav"))
            {
                GD.Print("Adding", file);
                AudioFileManager.Instance.AddToList(file);
            }
        }
    }

    private void LoadRecursively(string path)
    {
        GD.Print("LOADING RECURSIVELY");

        try
        {
            string[] files = Directory.GetFiles(path, "*.wav", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                GD.Print("Adding", file);
                AudioFileManager.Instance.AddToList(file);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error loading files recursively from {path}: {ex.Message}");
        }
    }
}
