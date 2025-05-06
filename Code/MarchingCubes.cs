using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MineAndDine.Code.Extensions;
using System.Threading;
using System.ComponentModel;
using System.Collections.Concurrent;

namespace MineAndDine
{
    internal class MarchingCubes
    {
        Vector3I mySize;
        Thread myRenderThread;

        static readonly int TriCountSize = sizeof(uint);
        static readonly int StaticDataSize = sizeof(uint) * 4;
        static readonly int UniformDataSize = sizeof(uint) + sizeof(float);

        struct Job
        {
            public int[,,] Data;
            public uint Surface;
            public float Scale;
            public TaskCompletionSource<Godot.Vector3[]> TaskCompletionSource;
        }

        ConcurrentQueue<Job> myJobQueue = new ConcurrentQueue<Job>();

        public MarchingCubes(Vector3I aSize)
        {
            mySize = aSize;

            myRenderThread = new Thread(Run);
            myRenderThread.Start();
        }

        RenderingDevice myRenderDevice;
        uint myMaxTris;
        byte[] myCPUNodeData;
        int myDataSize;
        int myTriDataSize;

        Rid myComputeShaderId;

        Rid myGPUStaticBufferId;
        Rid myGPUUniformBufferId;
        Rid myGPUDataBufferId;

        Rid myGPUTriCountBufferOutputId;
        Rid myGPUTriBufferOutputId;
        Rid myUniformSetId;
        Rid myPipelineId;

        private void Run()
        {
            myRenderDevice = RenderingServer.CreateLocalRenderingDevice();

            myDataSize = Utils.CountPoints(mySize) * sizeof(int);
            myMaxTris = (uint)Utils.CountPoints(mySize - Vector3I.One) * 5;
            myTriDataSize = (int)myMaxTris * sizeof(float) * 3 * 3;
            
            GD.Print($"Size {mySize} TriSize {myTriDataSize}");
            GD.Print($"Size {mySize} DataSize {myDataSize}");
            
            myCPUNodeData = new byte[myDataSize];
            
            // Load GLSL shader
            RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://Shaders/Compute/MarchingCubes.glsl");
            RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
            myComputeShaderId = myRenderDevice.ShaderCreateFromSpirV(shaderBytecode);
            myPipelineId = myRenderDevice.ComputePipelineCreate(myComputeShaderId);

            uint[] staticData = [(uint)mySize.X, (uint)mySize.Y, (uint)mySize.Z, (uint)myMaxTris];
            byte[] staticDataRaw = new byte[StaticDataSize];
            Buffer.BlockCopy(staticData, 0, staticDataRaw, 0, StaticDataSize);
            
            myGPUStaticBufferId = myRenderDevice.StorageBufferCreate((uint)StaticDataSize, staticDataRaw);
            myGPUUniformBufferId = myRenderDevice.StorageBufferCreate((uint)UniformDataSize);
            myGPUDataBufferId = myRenderDevice.StorageBufferCreate((uint)myDataSize);
            myGPUTriCountBufferOutputId = myRenderDevice.StorageBufferCreate((uint)TriCountSize);
            myGPUTriBufferOutputId = myRenderDevice.StorageBufferCreate((uint)myTriDataSize);
            
            RDUniform staticDataBinding = new RDUniform { UniformType = RenderingDevice.UniformType.StorageBuffer, Binding = 0 };
            staticDataBinding.AddId(myGPUStaticBufferId);
            
            RDUniform uniformDatabinding = new RDUniform { UniformType = RenderingDevice.UniformType.StorageBuffer, Binding = 1 };
            uniformDatabinding.AddId(myGPUUniformBufferId);
            
            RDUniform dataBinding = new RDUniform { UniformType = RenderingDevice.UniformType.StorageBuffer, Binding = 2 };
            dataBinding.AddId(myGPUDataBufferId);
            
            RDUniform triCountOutputbinding = new RDUniform { UniformType = RenderingDevice.UniformType.StorageBuffer, Binding = 3 };
            triCountOutputbinding.AddId(myGPUTriCountBufferOutputId);
            
            RDUniform triOutputbinding = new RDUniform { UniformType = RenderingDevice.UniformType.StorageBuffer, Binding = 4 };
            triOutputbinding.AddId(myGPUTriBufferOutputId);
            
            myUniformSetId = myRenderDevice.UniformSetCreate([staticDataBinding, uniformDatabinding, dataBinding, triCountOutputbinding, triOutputbinding], myComputeShaderId, 0);

            while (true)
            {
                Job job;

                if (!myJobQueue.TryDequeue(out job))
                {
                    Thread.Yield();
                    continue;
                }

                byte[] uniformRaw = new byte[UniformDataSize];
                int at = 0;
                uniformRaw.Write(job.Surface, ref at);
                uniformRaw.Write(job.Scale, ref at);
                
                myRenderDevice.BufferUpdate(myGPUUniformBufferId, 0, (uint)UniformDataSize, uniformRaw);
                myRenderDevice.BufferUpdate(myGPUTriCountBufferOutputId, 0, sizeof(uint), [0, 0, 0, 0]);
                
                Buffer.BlockCopy(job.Data, 0, myCPUNodeData, 0, myDataSize);
                myRenderDevice.BufferUpdate(myGPUDataBufferId, 0, (uint)myCPUNodeData.Length, myCPUNodeData);
                
                long computeListIndex = myRenderDevice.ComputeListBegin();
                myRenderDevice.ComputeListBindComputePipeline(computeListIndex, myPipelineId);
                myRenderDevice.ComputeListBindUniformSet(computeListIndex, myUniformSetId, 0);
                myRenderDevice.ComputeListDispatch(computeListIndex, xGroups: (uint)mySize.X - 1, yGroups: (uint)mySize.Y - 1, zGroups: (uint)mySize.Z - 1);
                myRenderDevice.ComputeListEnd();
                
                myRenderDevice.Submit();
                myRenderDevice.Sync();
                
                byte[] triCountBytes = myRenderDevice.BufferGetData(myGPUTriCountBufferOutputId);
                uint triCount =
                        (uint)triCountBytes[0] << 0
                    | (uint)triCountBytes[1] << 8
                    | (uint)triCountBytes[2] << 16
                    | (uint)triCountBytes[3] << 24;

                triCount = Math.Min(triCount, myMaxTris);
                
                float[] resultFloats = new float[triCount * 12];
                byte[] outputBytes = myRenderDevice.BufferGetData(myGPUTriBufferOutputId, 0, (uint)resultFloats.Length * sizeof(float));
                
                Buffer.BlockCopy(outputBytes, 0, resultFloats, 0, resultFloats.Length * sizeof(float));
                
                Godot.Vector3[] result = new Godot.Vector3[resultFloats.Length / 4];
                
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = new Godot.Vector3(resultFloats[i * 4 + 0], resultFloats[i * 4 + 1], resultFloats[i * 4 + 2]);
                }
                
                RenderingServer.CallOnRenderThread(Callable.From(() =>
                {
                    job.TaskCompletionSource.SetResult(result);
                }));
            }
        }

        public Task<Godot.Vector3[]> Calculate(int[,,] aValues, uint aSurface = 128, float aScale = 1.0f)
        {
            if (aValues.GetLength(0) != mySize.X) { throw new ArgumentException("X-Axis size missmatch"); }
            if (aValues.GetLength(1) != mySize.Y) { throw new ArgumentException("Y-Axis size missmatch"); }
            if (aValues.GetLength(2) != mySize.Z) { throw new ArgumentException("Z-Axis size missmatch"); }

            TaskCompletionSource<Godot.Vector3[]> taskSource = new TaskCompletionSource<Godot.Vector3[]>();

            myJobQueue.Enqueue(
                new Job
                {
                    Data = aValues,
                    Surface = aSurface,
                    Scale = aScale,
                    TaskCompletionSource = taskSource
                });

            return taskSource.Task;
        }

    }
}
