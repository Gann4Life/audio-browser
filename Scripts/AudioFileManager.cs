using Godot;
using System;
using Godot.Collections;

public partial class AudioFileManager : VBoxContainer
{
	[Export] public AudioStreamPlayer StreamPlayer;
	[Export] private PackedScene ButtonTemplate;

	public static AudioFileManager Instance;

	public override void _Ready()
	{
		Instance = this;
		ClearList();
		
		StreamPlayer.Finished += StreamPlayerOnFinished;
	}

	public void AddToList(string newFile)
	{
		 var button = GD.Load<PackedScene>(ButtonTemplate.ResourcePath);
		 AudioFile instance = (AudioFile)button.Instantiate();
		 AddChild(instance);
		 instance.Initialize(newFile);
	}

	public void RemoveFromList()
	{
		
	}

	public void ClearList()
	{
		Array<Node> nodes = GetChildren();
		foreach (var node in nodes)
		{
			node.QueueFree();
		}
	}
	
	private void StreamPlayerOnFinished()
	{
		if (PlaybackSettings.Instance.Loop)
			StreamPlayer.Play();
	}
}
