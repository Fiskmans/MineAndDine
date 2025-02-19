using Godot;
using System;
using System.Collections.Generic;


public partial class TerrainGenerator : Node3D
{
	[Export]
	public double ChunkSize = 16;

	Dictionary<Chunk.Pos, Chunk> myLoadedChunks = new Dictionary<Chunk.Pos, Chunk>();

    [Export]
    PackedScene chunkScene = GD.Load<PackedScene>("res://Scenes/Fragments/chunk.tscn");

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private Chunk.Pos ChunkPosFromWorldPos(Vector3 aPosition)
	{
		return new Chunk.Pos
		{
			x = (int)(aPosition.X / ChunkSize),
			y = (int)(aPosition.Y / ChunkSize),
			z = (int)(aPosition.Z / ChunkSize)
		};
	}

	private Vector3 WorldPosFromChunkPos(Chunk.Pos aPosition)
	{
		return new Vector3
		{
			X = (int)(aPosition.x * ChunkSize),
			Y = (int)(aPosition.y * ChunkSize),
			Z = (int)(aPosition.z * ChunkSize)
		};
	}

	public Chunk GetChunkAt(Vector3 aPosition)
	{
		Chunk.Pos pos = ChunkPosFromWorldPos(aPosition);

		Chunk res;

		if (myLoadedChunks.TryGetValue(pos, out res))
			return res;

		res = (Chunk)chunkScene.Instantiate();
		res.Position = WorldPosFromChunkPos(pos);

		myLoadedChunks.Add(pos, res);

		AddChild(res);

		return res;
	}
}
