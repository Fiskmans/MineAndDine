using Godot;
using Godot.Collections;
using MineAndDine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static Godot.TextServer;
using static MineAndDine.PickupObject;
using MineAndDine.Code.Extensions;


public partial class PlayerController : CharacterBody3D
{
    [Export]
    public int mySpeed { get; set; } = 14;
    [Export]
    public float mySprintMultiplier { get; set; } = 1.5f;
    [Export]
    public float myGravity { get; set; } = 16;

    [Export]
    public float myReach { get; set; } = 16;

    [Export]
    public float myJumpStrength { get; set; } = 5;

    [Export(PropertyHint.Range, "0.f,1.f,0.01f")]
    float myCameraSensitivity = 0.005f;
    [Export(PropertyHint.Range, "0.f,360.f,1.f,radians_as_degrees")]
    float myCameraTiltLimit = Mathf.DegToRad(75);

    private Camera3D myCamera;
    private Node3D myCameraPivot;
    public Node3D myHand { get; private set; }
    [Export]
    private Label myHUDText;

    private Vector3 myTargetVelocity = Vector3.Zero;
    private bool myIsSprinting = false;
    private bool myIsHoldingObject = false; //Use func pls :)

    private PickupObject myHeldObject;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        myCameraPivot = GetNode<Node3D>("cameraPivot"); //Spawn this stuff instead?
        myCamera = GetNode<Camera3D>("cameraPivot/playerCamera");

        myHand = GetNode<Node3D>("cameraPivot/playerCamera/hand");

		Input.MouseMode = Input.MouseModeEnum.Captured;
        myHUDText.Visible = false;
	}

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        Terrain.ourInstance.Touch(new Aabb(Position - new Vector3(30, 10, 30), new Vector3(60, 20, 60)));

        Dictionary intersection = DoRayCast((uint)MineAndDine.Code.Constants.CollisionLayer.Interaction);
        if(intersection.Count > 0)
        {
            GodotObject obj = intersection["collider"].AsGodotObject();
            
            if(obj is PickupObject)
            {
                myHUDText.Visible = true;
                myHUDText.Text = (obj as PickupObject)?.myName;
            }
            else
            {
                myHUDText.Visible = false;
            }
        }
        else
        {
            myHUDText.Visible = false;
        }

    }

	public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionJustPressed("main_interact"))
        {
            Interact((float)delta);
        }

        if (Input.IsActionJustPressed("secondary_interact"))
        {
            DropHeldObject();
        }

        Vector3 direction = HandleMovementInput();
        ApplyMovement(delta, direction);
    }

    private void ApplyMovement(double delta, Vector3 direction)
    {
        myTargetVelocity.X = direction.X * mySpeed * (myIsSprinting ? mySprintMultiplier : 1);
        myTargetVelocity.Z = direction.Z * mySpeed * (myIsSprinting ? mySprintMultiplier : 1);

        if (IsOnFloor())
        {
            if (myTargetVelocity.Y < 0.1f)
            {
                myTargetVelocity.Y = 0;
            }
            if (Input.IsActionPressed("jump"))
            {
                myTargetVelocity.Y = myJumpStrength;
            }
        }
        else
        {
            myTargetVelocity.Y -= myGravity * (float)delta;
        }

        Velocity = myTargetVelocity;

        if (MoveAndSlide())
        {
            HandleCollision();
        }
    }

    private Vector3 HandleMovementInput()
    {
        var direction = Vector3.Zero;

        Vector3 camRot = myCamera.GetGlobalRotation();

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

        if (Input.IsActionJustPressed("sprint"))
        {
            myIsSprinting = !myIsSprinting;
        }
		if (direction != Vector3.Zero)
		{
			direction = direction.Normalized();
			// Setting the basis property will affect the rotation of the node.
			GetNode<Node3D>("meshPivot").Basis = Basis.LookingAt(direction);
		}

        if (direction.Dot(forward) < 0.2f)
        {
            myIsSprinting = false;
        }

        return direction;
    }

    public override void _UnhandledInput(InputEvent anEvent) //Is there no other way to capture mouse movement????
    {
        if (anEvent is InputEventKey keyEvent)
        {
            if (keyEvent.Pressed && keyEvent.Keycode == Key.Escape && OS.HasFeature("editor"))
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
            if (anEvent is InputEventMouseMotion mouseEvent)
            {
                Vector3 cameraRot = myCameraPivot.GetRotation();

                cameraRot.X -= mouseEvent.GetRelative().Y * myCameraSensitivity;
                cameraRot.X = Mathf.Clamp(cameraRot.X, -myCameraTiltLimit, myCameraTiltLimit);
                cameraRot.Y -= mouseEvent.GetRelative().X * myCameraSensitivity;

                myCameraPivot.SetRotation(cameraRot);
            }
        }
    }

	private void Interact(float aDeltaTime)
    {
        if (myHeldObject != null)
        {
            myHeldObject.Use();
            return;
        }

        Dictionary intersection = DoRayCast((uint)MineAndDine.Code.Constants.CollisionLayer.Interaction);

        if (intersection.Count == 0) // Empty dictionary means no collision
        {
            return;
        }

        GodotObject clicked = intersection["collider"].AsGodotObject();

        (clicked as PickupObject)?.PickUp(this);
        (clicked as Interactable)?.Activate();
    }

    public void Hold(PickupObject aObject)
    {
        myHeldObject = aObject;
        myHand.AddChild(aObject);
        myIsHoldingObject = true;
    }

    private void DropHeldObject()
    {
        myHeldObject?.Drop();
        myHeldObject = null;
        myIsHoldingObject = false;
    }

    public Dictionary DoRayCast(uint aCollisionMask)
    {
        Vector2 mousePos = GetViewport().GetMousePosition();

        Vector3 origin = myCamera.ProjectRayOrigin(mousePos);

        PhysicsRayQueryParameters3D rayParams = new PhysicsRayQueryParameters3D();

        rayParams.From = origin;
        rayParams.To = origin + myCamera.ProjectRayNormal(mousePos) * myReach;
        rayParams.CollisionMask = aCollisionMask;

        return GetWorld3D().DirectSpaceState.IntersectRay(rayParams); //https://github.com/godotengine/godot-docs-user-notes/discussions/100#discussioncomment-10655180 Probs won't matter tho
    }

	private void HandleCollision()
	{
	}
}
