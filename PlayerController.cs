using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{
	[Export]
	public int speed { get; set; } = 14;
	[Export]
	public float gravity { get; set; } = 16;
	
	[Export(PropertyHint.Range, "0.f,1.f,0.01f")]
	float cameraSensitivity = 0.01f;
	[Export(PropertyHint.Range, "0.f,360.f,1.f,radians_as_degrees")]
	float cameraTiltLimit = Mathf.DegToRad(75);
	
	private Vector3 targetVelocity = Vector3.Zero;
	private Camera3D camera;
	private Node3D cameraPivot;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		cameraPivot = GetNode<Node3D>("cameraPivot");
		camera = GetNode<Camera3D>("cameraPivot/cameraArm/playerCamera");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		var direction = Vector3.Zero;
		
		Vector3 camRot = camera.GetGlobalRotation();
		
		Vector3 forward = new Vector3(-(float)Mathf.Sin(camRot.Y), 0, -(float)Mathf.Cos(camRot.Y));
		Vector3 right = new Vector3(-(float)Mathf.Sin(camRot.Y - (Math.PI * 0.5f)), 0, -(float)Mathf.Cos(camRot.Y - (Math.PI * 0.5f)));

		if (Input.IsActionPressed("move_right"))
		{
			direction += right;
		}
		if (Input.IsActionPressed("move_left"))
		{
			direction -= right;
		}
		if (Input.IsActionPressed("move_back"))
		{
			direction -= forward;
		}
		if (Input.IsActionPressed("move_forward"))
		{
			direction += forward;
		}

		if (direction != Vector3.Zero)
		{
			direction = direction.Normalized();
			// Setting the basis property will affect the rotation of the node.
			GetNode<Node3D>("meshPivot").Basis = Basis.LookingAt(direction);
		}

		targetVelocity.X = direction.X * speed;
		targetVelocity.Z = direction.Z * speed;

		if (IsOnFloor())
		{
			if (targetVelocity.Y < 0.1f)
			{
				targetVelocity.Y = 0;
			}
			if (Input.IsActionPressed("jump"))
			{
				targetVelocity.Y = 5;
			}
		}
		else
		{
			targetVelocity.Y -= gravity * (float)delta;
		}

		Velocity = targetVelocity;

		if (MoveAndSlide())
			HandleCollision();
	}
	
	public override void _UnhandledInput(InputEvent @event) //0 clue what the @ means ¯\_(ツ)_/¯  TODO: make proper input events
	{
		if (@event is InputEventKey keyEvent)
		{
			if (keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
			{
				GetTree().Quit();
			}
		}
		
		if (@event is InputEventMouseMotion mouseEvent)
		{
			Vector3 cameraRot = cameraPivot.GetRotation();
			
			cameraRot.X -= mouseEvent.GetRelative().Y * cameraSensitivity;
			cameraRot.X = Mathf.Clamp(cameraRot.X, -cameraTiltLimit, cameraTiltLimit);
			cameraRot.Y -= mouseEvent.GetRelative().X * cameraSensitivity;
			
			cameraPivot.SetRotation(cameraRot);
		}
	}

	private void HandleCollision()
	{
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{

			KinematicCollision3D collision = GetSlideCollision(i);

			GodotObject obj = collision.GetCollider();

			TerrainGenerator terrain = obj as TerrainGenerator;

			if (terrain != null)
			{
				terrain.GetChunkAt(Position);
			}
		}
	}
}
