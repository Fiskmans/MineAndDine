using Godot;
using MineAndDine;
using System;
using System.Collections.Generic;
using System.Linq;
using static Godot.TabContainer;


public partial class TerrainGenerator : Node3D
{
	[Export]
	public float ChunkSize = 16;

	Dictionary<Vector3I, Chunk> myLoadedChunks = new Dictionary<Vector3I, Chunk>();

    [Export]
    PackedScene chunkScene = GD.Load<PackedScene>("res://Scenes/Fragments/chunk.tscn");

	public delegate void ChunkedChangeHandler(Chunk aChunk);

	public event ChunkedChangeHandler OnChunkChange;

    HashSet<Chunk> myModifiedChunks = new HashSet<Chunk>();
    HashSet<Chunk> myChunksToRemesh = new HashSet<Chunk>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
		HashSet<Chunk> chunks = new HashSet<Chunk>(myModifiedChunks);
        myModifiedChunks.Clear();

        foreach (Chunk c in chunks)
		{
			c.Update();
			OnChunkChange?.Invoke(c);
        }

        foreach (Chunk chunk in myChunksToRemesh)
        {
            chunk.RegenerateMesh();
        }

        myChunksToRemesh.Clear();
    }

    public void RegisterModification(Chunk aChunk)
	{
		myModifiedChunks.Add(aChunk);

		for (int x = -1; x <= 0; x++)
		{
			for (int y = -1; y <= 0; y++)
			{
				for (int z = -1; z <= 0; z++)
				{
					Chunk c = TryGetChunk(aChunk.ChunkPos + new Vector3I(x, y, z));

					if (c != null)
						myChunksToRemesh.Add(c);
				}
			}
		}
	}

	public Vector3I ChunkPosFromWorldPos(Vector3 aPosition)
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

	public IEnumerable<Chunk> AffectedChunks(Aabb aArea)
    {
		foreach(Vector3I pos in Utils.Every(ChunkPosFromWorldPos(aArea.Position) - new Vector3I(1, 1, 1), ChunkPosFromWorldPos(aArea.End)))
        {
            Chunk res;

            if (myLoadedChunks.TryGetValue(pos, out res))
                yield return res;
        }
    }

    public void Touch(Aabb aArea)
    {
        Touch(Utils.Every(ChunkPosFromWorldPos(aArea.Position) - new Vector3I(1, 1, 1), ChunkPosFromWorldPos(aArea.End)));
    }

    public void Touch(IEnumerable<Vector3I> aChunkPositions)
	{
		foreach (Vector3I pos in aChunkPositions)
		{
			Touch(pos);
		}
    }

	public void Touch(Vector3I aChunkPos)
	{
		ChunkAt(aChunkPos);
    }

	public Chunk ChunkAt(Vector3I aPosition)
	{
		Chunk res;

		if (myLoadedChunks.TryGetValue(aPosition, out res))
			return res;

		res = (Chunk)chunkScene.Instantiate();
		res.ChunkPos = aPosition;
		res.Size = ChunkSize;
		res.OwningTerrain = this;
        res.Position = WorldPosFromChunkPos(aPosition);

		myLoadedChunks.Add(aPosition, res);

		AddChild(res);

		return res;
	}
}
