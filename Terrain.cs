using Godot;
using MineAndDine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;


public partial class Terrain : Node3D
{
	[Export]
	public float myChunkSize = 16;

    [Export]
    PackedScene myChunkScene = GD.Load<PackedScene>("res://Scenes/Fragments/chunk.tscn");

	[Export]
	public int myThreads = 4;

    public static Terrain ourInstance { get; private set; } = null;

	ConcurrentDictionary<Vector3I, Chunk> myChunks = new ConcurrentDictionary<Vector3I, Chunk>();

	public delegate void ChunkedChangeHandler(Chunk aChunk);

	public event ChunkedChangeHandler OnChunkChange;

	Godot.Timer myPulse = new Godot.Timer();

	List<Thread> myWorkerThreads = new List<Thread>();

	ConcurrentDictionary<Chunk, bool> myModifiedChunks = new ConcurrentDictionary<Chunk, bool>();

    ConcurrentQueue<Chunk> myTaskList = new ConcurrentQueue<Chunk>();
    ConcurrentQueue<Chunk> myChunksToRemesh = new ConcurrentQueue<Chunk>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (ourInstance != null) throw new Exception("Multiple terrains added to world");

		ourInstance = this;

		for (int i = 0;	i < myThreads; i++)
            myWorkerThreads.Add(new Thread(DoTerrainUpdates));

		foreach (Thread thread in myWorkerThreads)
			thread.Start();

		myPulse.Timeout += FlushChanges;
		myPulse.OneShot = false;
		
		AddChild(myPulse);
		myPulse.Start(0.3); //200bpm
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
    }

	private void FlushChanges()
	{
        foreach (Chunk c in myModifiedChunks.Keys)
        {
            myTaskList.Enqueue(c);
			// This leaks some updates, cant be bothered to fix it right now
        }

        myModifiedChunks.Clear();
    }

	private void DoTerrainUpdates()
	{
		while (true) // TODO, prolly clean this thread up lol
		{
			Chunk chunk = null;
			bool yield = true;

            if (myTaskList.TryDequeue(out chunk))
            {
				yield = false;
                chunk.Update();
                OnChunkChange?.Invoke(chunk);
            }

			if (myChunksToRemesh.TryDequeue(out chunk))
			{
				yield = false;
				chunk.RegenerateMesh();
			}

			if (yield)	
	            Thread.Yield();
        }
    }

    public void RegisterModification(Chunk aChunk)
	{
		myModifiedChunks.TryAdd(aChunk, true);

		foreach(Vector3I pos in Utils.Every(aChunk.myChunkPos - new Vector3I(1,1,1), aChunk.myChunkPos))
        {
            Chunk c = TryGetChunk(pos);
			if (c == null)
				continue;

			c.MarkDirty();
            myChunksToRemesh.Enqueue(c);
        }
	}

	public Vector3I ChunkPosFromWorldPos(Vector3 aPosition)
	{
		return new Vector3I(
			(int)(aPosition.X / myChunkSize),
			(int)(aPosition.Y / myChunkSize),
			(int)(aPosition.Z / myChunkSize));
	}

	private Vector3 WorldPosFromChunkPos(Vector3I aPosition)
	{
		return new Vector3
		{
			X = (int)(aPosition.X * myChunkSize),
			Y = (int)(aPosition.Y * myChunkSize),
			Z = (int)(aPosition.Z * myChunkSize)
		};
	}

	public Chunk TryGetChunk(Vector3I aChunkPos)
	{
		Chunk c = null;
		if (!myChunks.TryGetValue(aChunkPos, out c))
			c = null;
		return c;
    }

	public IEnumerable<Chunk> AffectedChunks(Aabb aArea)
    {
		foreach(Vector3I pos in Utils.Every(ChunkPosFromWorldPos(aArea.Position) - new Vector3I(1, 1, 1), ChunkPosFromWorldPos(aArea.End)))
        {
            Chunk res;

            if (myChunks.TryGetValue(pos, out res))
			{
                yield return res;
			}
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

		if (myChunks.TryGetValue(aPosition, out res))
		{
			return res;
		}

		res = (Chunk)myChunkScene.Instantiate();
		res.myChunkPos = aPosition;
		res.mySize = myChunkSize;
        res.Position = WorldPosFromChunkPos(aPosition);

		AddChild(res);

		myChunks.TryAdd(aPosition, res);

		return res;
	}
}
