using Godot;
using Godot.Collections;
using System;
using static Godot.TextServer;

public partial class PlayerController : CharacterBody3D
{
	[Export]
	public int speed { get; set; } = 14;
	[Export]
	public float gravity { get; set; } = 16;

	[Export]
	public float Reach { get; set; } = 16;
	
	[Export(PropertyHint.Range, "0.f,1.f,0.01f")]
	float cameraSensitivity = 0.01f;
	[Export(PropertyHint.Range, "0.f,360.f,1.f,radians_as_degrees")]
	float cameraTiltLimit = Mathf.DegToRad(75);
	
	private Vector3 targetVelocity = Vector3.Zero;
	private Camera3D camera;
	private Node3D cameraPivot;
	private TerrainGenerator terrainGenerator;

	public override void _Ready()
	// Called when the node enters the scene tree for the first time.
	{
		cameraPivot = GetNode<Node3D>("cameraPivot");
		camera = GetNode<Camera3D>("cameraPivot/cameraArm/playerCamera");
		terrainGenerator = GetParent().GetNode<TerrainGenerator>("Terrain");

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Vector3I center = terrainGenerator.ChunkPosFromWorldPos(Position);

		terrainGenerator.Touch(center - new Vector3I(2, 1, 2), center + new Vector3I(2,1,2));
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

        if (Input.IsActionJustPressed("main_interact"))
            Interact();
    }
	
	public override void _UnhandledInput(InputEvent @event) //0 clue what the @ means ¯\_(ツ)_/¯  TODO: make proper input events
	{
		if (@event is InputEventKey keyEvent)
		{
			if (keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
			{
				GetTree().Quit();
			}

			if (keyEvent.Keycode == Key.Alt)
			{
				if (keyEvent.IsPressed())
				{
					Input.MouseMode = Input.MouseModeEnum.Visible;
				}
				else
				{
					Input.MouseMode = Input.MouseModeEnum.Captured;
				}
			}
		}

		if (Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			if (@event is InputEventMouseMotion mouseEvent)
			{
				Vector3 cameraRot = cameraPivot.GetRotation();
			
				cameraRot.X -= mouseEvent.GetRelative().Y * cameraSensitivity;
				cameraRot.X = Mathf.Clamp(cameraRot.X, -cameraTiltLimit, cameraTiltLimit);
				cameraRot.Y -= mouseEvent.GetRelative().X * cameraSensitivity;
			
				cameraPivot.SetRotation(cameraRot);
			}
		}
	}

	private void Interact()
	{
		GD.Print("Interact");

		Vector2 mousePos = GetViewport().GetMousePosition();

		Vector3 origin = camera.ProjectRayOrigin(mousePos);

		PhysicsRayQueryParameters3D rayParams = new PhysicsRayQueryParameters3D();

		rayParams.From = origin;
		rayParams.To = origin + camera.ProjectRayNormal(mousePos) * Reach;
		rayParams.CollisionMask = 2;

		Dictionary intersect = GetWorld3D().DirectSpaceState.IntersectRay(rayParams);

		if (intersect.Count == 0) // Empty dictionary means no collision
			return;

        GD.Print("Intersect");

        Vector3 pos = ((Vector3)intersect["position"]);

		Node3D dig = new Node3D { Position = pos };
		MeshInstance3D visual = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.6f, Height = 0.3f } };

		dig.AddChild(visual);
		GetParent().AddChild(dig);
	}

	private void HandleCollision()
	{
	}
}
