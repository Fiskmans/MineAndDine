using Godot;
using Microsoft.VisualBasic;
using MineAndDine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;


public partial class Terrain : Node3D
{
    [Export]
    PackedScene myChunkScene = GD.Load<PackedScene>("res://Scenes/Fragments/chunk.tscn");

	[Export]
	public int myThreads = 4;

    public static Terrain ourInstance { get; private set; } = null;

	ConcurrentDictionary<Vector3I, Chunk> myChunks = new ConcurrentDictionary<Vector3I, Chunk>();

	public delegate void ChunkedChangeHandler(Chunk aChunk);

	Godot.Timer myPulse = new Godot.Timer();

	List<Thread> myWorkerThreads = new List<Thread>();

	struct ChunkTask
	{
		public ChunkTask(Chunk aChunk)
		{
			Chunk = aChunk;
		}

		public Chunk Chunk;
		public bool Update = false;
		public bool Remesh = false;
	}

    ConcurrentQueue<ChunkTask> myTaskList = new ConcurrentQueue<ChunkTask>();

    ConcurrentQueue<Chunk> myModifiedChunks = new ConcurrentQueue<Chunk>();

    ConcurrentQueue<Chunk> myChunksToRemesh = new ConcurrentQueue<Chunk>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (ourInstance != null) throw new Exception("Multiple terrains added to world");

		MaterialGroups.GenerateTables();

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
		if (!myTaskList.IsEmpty)
		{
			return;
		}

        HashSet<Chunk> updates = DequeueChunks(myModifiedChunks);
		HashSet<Chunk> remesh = DequeueChunks(myChunksToRemesh);

		HashSet<Chunk> all = new HashSet<Chunk>();

		all.UnionWith(updates);
		all.UnionWith(remesh);

		if (all.Count == 0)
		{
			return;
		}

        foreach (Chunk c in all)
        {
			ChunkTask task = new ChunkTask(c);

			task.Update = updates.Contains(c);
			task.Remesh = remesh.Contains(c);

            myTaskList.Enqueue(task);
        }

		GD.Print("Scheduled ", all.Count, " chunk tasks");
    }

    private HashSet<Chunk> DequeueChunks(ConcurrentQueue<Chunk> aChunkQueue)
    {
        aChunkQueue.Enqueue(null); // Fence
        HashSet<Chunk> chunks = new HashSet<Chunk>();

        while (true)
        {
            Chunk c;
            aChunkQueue.TryDequeue(out c);

            if (c == null)
            {
                break; // Fence reached
            }

            chunks.Add(c);
        }

        return chunks;
    }

    private void DoTerrainUpdates()
	{
		while (true) // TODO, prolly clean this thread up lol
		{
			ChunkTask task;
			if (!myTaskList.TryDequeue(out task))
			{
                Thread.Yield();
				continue;
			}

			Chunk chunk = task.Chunk;

            if (task.Update)
            {
                chunk.Update();
            }

			if (task.Remesh)
			{
				chunk.RegenerateMesh();
			}
        }
    }

    public void RegisterModification(Chunk aChunk)
	{
		myModifiedChunks.Enqueue(aChunk);

		foreach(Vector3I pos in Utils.Every(aChunk.ChunkIndex - new Vector3I(1,1,1), aChunk.ChunkIndex))
        {
            Chunk c = TryGetChunk(pos);
			if (c == null)
				continue;

			c.MarkDirty();
            myChunksToRemesh.Enqueue(c);
        }
	}

	public Chunk TryGetChunk(Vector3I aChunkIndex)
	{
		Chunk c = null;
		if (!myChunks.TryGetValue(aChunkIndex, out c))
			c = null;
		return c;
    }

	public IEnumerable<Chunk> AffectedChunks(Aabb aArea)
    {
		foreach(Vector3I pos in Utils.Every(Chunk.IndexFromPos(aArea.Position) - new Vector3I(1, 1, 1), Chunk.IndexFromPos(aArea.End)))
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
        Touch(Utils.Every(Chunk.IndexFromPos(aArea.Position) - new Vector3I(1, 1, 1), Chunk.IndexFromPos(aArea.End)));
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

	public Chunk ChunkAt(Vector3I aChunkIndex)
	{
		Chunk res;

		if (myChunks.TryGetValue(aChunkIndex, out res))
		{
			return res;
		}

		res = (Chunk)myChunkScene.Instantiate();
		res.ChunkIndex = aChunkIndex;

		AddChild(res);

		myChunks.TryAdd(aChunkIndex, res);

		return res;
	}
}
