using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{
    [Export]
    public int Speed { get; set; } = 14;
    [Export]
    public float gravity { get; set; } = 16.f;
    private Vector3 _targetVelocity = Vector3.Zero;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override void _PhysicsProcess(double delta)
    {
        var direction = Vector3.Zero;

        if (Input.IsActionPressed("move_right"))
        {
            direction.X += 1.0f;
        }
        if (Input.IsActionPressed("move_left"))
        {
            direction.X -= 1.0f;
        }
        if (Input.IsActionPressed("move_back"))
        {
            direction.Z += 1.0f;
        }
        if (Input.IsActionPressed("move_forward"))
        {
            direction.Z -= 1.0f;
        }

        if (direction != Vector3.Zero)
        {
            direction = direction.Normalized();
            // Setting the basis property will affect the rotation of the node.
            GetNode<Node3D>("Pivot").Basis = Basis.LookingAt(direction);
        }

        _targetVelocity.X = direction.X * Speed;
        _targetVelocity.Z = direction.Z * Speed;

        if (IsOnFloor())
        {
            if (_targetVelocity.Y < 0.1f)
            {
                _targetVelocity.Y = 0;
            }
            if (Input.IsActionPressed("jump"))
            {
                _targetVelocity.Y = 5;
            }
        }
        else
        {
            _targetVelocity.Y -= gravity * (float)delta;
        }

        Velocity = _targetVelocity;

        MoveAndSlide();


    }
}
