using Godot;
using System;
using static Godot.TextServer;

public partial class Freecam : Camera3D
{
    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Input.IsActionPressed("move_right"))
        {
            Position += Vector3.Right * (float)delta;
        }
        if (Input.IsActionPressed("move_left"))
        {
            Position += Vector3.Left * (float)delta;
        }
        if (Input.IsActionPressed("move_back"))
        {
            Position += Vector3.Back * (float)delta;
        }
        if (Input.IsActionPressed("move_forward"))
        {
            Position += Vector3.Forward * (float)delta;
        }
    }
}
