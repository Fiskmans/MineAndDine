using Godot;
using System;

public partial class Chunk : Node3D
{
	public struct Pos
	{
		public int x; 
		public int y; 
		public int z;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
