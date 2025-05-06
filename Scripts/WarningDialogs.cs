using Godot;
using System;

public partial class WarningDialogs : AcceptDialog
{
    public static WarningDialogs Instance;

    public override void _Ready()
    {
        Instance = this;
    }

    public void DisplayMessage(string title, string message)
    {
        this.Title = title;
        this.DialogText = message;
        this.Show();
    }
}
