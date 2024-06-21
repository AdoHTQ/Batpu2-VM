using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class Fullscreen : CheckBox
{
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("fullscreen")) return;

        ButtonPressed = !ButtonPressed;
    }
    public void Toggle(bool state)
    {
        if (state) DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        else DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
    }
}
