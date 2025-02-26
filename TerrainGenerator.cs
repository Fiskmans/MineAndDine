using Godot;
using System;
using System.Collections.Generic;


public partial class TerrainGenerator : Node3D
{
	[Export]
	public float ChunkSize = 16;

	Dictionary<Vector3I, Chunk> myLoadedChunks = new Dictionary<Vector3I, Chunk>();

    [Export]
    PackedScene chunkScene = GD.Load<PackedScene>("res://Scenes/Fragments/chunk.tscn");

	public delegate void ChunkedChangeHandler(Chunk aChunk);

	public event ChunkedChangeHandler OnChunkChange;

    List<Chunk> _myModifiedChunks = new List<Chunk>();
    List<Chunk> myModifiedChunks = new List<Chunk>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// swap buffers so we don't end up in infinite loops
		(_myModifiedChunks, myModifiedChunks) = (myModifiedChunks, _myModifiedChunks);

		if (OnChunkChange != null)
            _myModifiedChunks.ForEach(OnChunkChange.Invoke);

        _myModifiedChunks.Clear();
	}

	public void RegisterModification(Chunk aChunk)
	{
		myModifiedChunks.Add(aChunk);
	}

	private Vector3I ChunkPosFromWorldPos(Vector3 aPosition)
	{
		return new Vector3I(
			(int)(aPosition.X / ChunkSize),
			(int)(aPosition.Y / ChunkSize),
			(int)(aPosition.Z / ChunkSize));
	}

	private Vector3 WorldPosFromChunkPos(Vector3I aPosition)
	{
		return new Vector3
		{
			X = (int)(aPosition.X * ChunkSize),
			Y = (int)(aPosition.Y * ChunkSize),
			Z = (int)(aPosition.Z * ChunkSize)
		};
	}

	public Chunk TryGetChunk(Vector3I aChunkPos)
	{
		Chunk c = null;
		myLoadedChunks.TryGetValue(aChunkPos, out c);
		return c;
    }

	public Chunk GetChunkAt(Vector3 aPosition)
	{
		Vector3I pos = ChunkPosFromWorldPos(aPosition);

		Chunk res;

		if (myLoadedChunks.TryGetValue(pos, out res))
			return res;

		res = (Chunk)chunkScene.Instantiate();
		res.ChunkPos = pos;
		res.OwningTerrain = this;
        res.Position = WorldPosFromChunkPos(pos);

		myLoadedChunks.Add(pos, res);

		AddChild(res);

		return res;
	}
}
