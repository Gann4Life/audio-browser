using Godot;
using System;

public partial class PlaybackSettings : Control
{
    public static PlaybackSettings Instance; 
    
    public bool Recursive => _recursive.ButtonPressed;
    public bool Loop => _loop.ButtonPressed;
    
    private CheckBox _recursive;
    private CheckBox _loop;
    
    public override void _Ready()
    {
        Instance = this;
        _recursive = GetNode<CheckBox>("Recursive");
        _loop = GetNode<CheckBox>("Loop");
    }
}
