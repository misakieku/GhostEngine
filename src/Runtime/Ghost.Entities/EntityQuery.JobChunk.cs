using Ghost.Core;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public interface IJobChunk
{
    void Execute(ChunkView view, ref readonly JobExecutionContext ctx);
}

internal unsafe struct ChunkInfo
{
    public Chunk* pChunk;
    public Archetype* pArchetype;
}

internal unsafe struct JobChunkBatch<TJob> : IJobParallelFor
    where TJob : unmanaged, IJobChunk
{

    public TJob userJob;
    public ReadOnlyUnsafeCollection<ChunkInfo> chunkInfos;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        var info = chunkInfos[loopIndex];
        var view = new ChunkView(in *info.pArchetype, in *info.pChunk);

        userJob.Execute(view, in ctx);
    }
}

internal struct DisposeJobChunk : IJob
{
    public UnsafeList<ChunkInfo> list;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        list.Dispose();
    }
}

public unsafe partial struct EntityQuery
{
    public JobHandle ScheduleChunkParallel<TJob>(TJob job, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobChunk
    {
        var world = World.GetWorld(_worldID);
        if (world is null)
        {
            return JobHandle.Invalid;
        }

        if (world.JobScheduler == null)
        {
            throw new InvalidOperationException("The World has no JobScheduler assigned.");
        }

        var chunkInfos = new UnsafeList<ChunkInfo>(_matchingArchetypes.Count * 2, TempJobAllocator.AllocationHandle);

        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            for (var i = 0; i < arch.ChunkCount; i++)
            {
                var pChunk = (Chunk*)arch._chunks.GetUnsafePtr() + i;

                chunkInfos.Add(new ChunkInfo
                {
                    pArchetype = (Archetype*)Unsafe.AsPointer(ref arch),
                    pChunk = pChunk
                });
            }
        }

        var batchJob = new JobChunkBatch<TJob>
        {
            userJob = job,
            chunkInfos = chunkInfos.AsReadOnly()
        };

        var handle = world.JobScheduler.ScheduleParallelFor(ref batchJob, chunkInfos.Count, batchSize, dependency);

        var disposeJob = new DisposeJobChunk
        {
            list = chunkInfos
        };

        world.JobScheduler.Schedule(ref disposeJob, handle);

        return handle;
    }
}
