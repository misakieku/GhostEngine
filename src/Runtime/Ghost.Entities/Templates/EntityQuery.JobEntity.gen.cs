using Ghost.Core;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Entities;

public interface IJobEntity<T0>
    where T0 : unmanaged, IComponent
{
    void Execute(Entity entity, ref T0 component0, ref readonly JobExecutionContext ctx);
}

internal unsafe struct JobBatchContext1
{
    public byte* chunk;
    public uint* chunkVersions;
    public byte* sharedDataBlob;
    public int chunkCount;
    public int entityOffset;

    public int offset0;
    public int enableOff0;
    public int versionIndex0;

    public int hiddenEnableCount;
    public fixed int hiddenEnableOffsets[16];
}

internal unsafe struct JobEntityBatch<TJob, T0> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0>
    where T0 : unmanaged, IComponent
{
    public fixed int componentIDs[1];
    public fixed bool componentRW[1];

    public TJob userJob;
    public UnsafeList<JobBatchContext1> batches;
    public EntityQueryMask mask;
    public uint version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        ref var batch = ref ((JobBatchContext1*)batches.GetUnsafePtr())[loopIndex];

        var pChunk = batch.chunk;
        var pVersions = batch.chunkVersions;
        var pSharedBlob = batch.sharedDataBlob;
        var count = batch.chunkCount;

        var off0 = batch.offset0;
        var enableOff0 = batch.enableOff0;
        var versionIndex0 = batch.versionIndex0;

        var pEntity = (Entity*)(pChunk + batch.entityOffset);

        var ptr0 = (T0*)(ComponentTypeID<T0>.IsShared ? (pSharedBlob + off0) : (pChunk + off0));

        if (componentRW[0])
        {
            pVersions[versionIndex0] = version;
        }


        // Execute batch
        var ulongCount = (count + 63) / 64;
        for (var block = 0; block < ulongCount; block++)
        {
            var validMask = ulong.MaxValue;
            var remaining = count - (block * 64);
            if (remaining < 64) validMask = (1UL << remaining) - 1UL;

            // Enforce enableable bits checking based on components required by Job Signature
            if (enableOff0 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff0);
                validMask &= pMask[block];
            }

            // Enforce EntityQuery Mask hidden enableable constraints
            for (var h = 0; h < batch.hiddenEnableCount; h++)
            {
                var pMask = (ulong*)(pChunk + batch.hiddenEnableOffsets[h]);
                validMask &= pMask[block];
            }

            while (validMask != 0)
            {
                var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                var i_ent = (block * 64) + bit;

                userJob.Execute(pEntity[i_ent],
                    ref (ComponentTypeID<T0>.IsShared ? ref ptr0[0] : ref ptr0[i_ent])
, in ctx);

                validMask ^= (1UL << bit);
            }
        }
    }
}

public interface IJobEntity<T0, T1>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
{
    void Execute(Entity entity, ref T0 component0, ref T1 component1, ref readonly JobExecutionContext ctx);
}

internal unsafe struct JobBatchContext2
{
    public byte* chunk;
    public uint* chunkVersions;
    public byte* sharedDataBlob;
    public int chunkCount;
    public int entityOffset;

    public int offset0;
    public int enableOff0;
    public int versionIndex0;

    public int offset1;
    public int enableOff1;
    public int versionIndex1;

    public int hiddenEnableCount;
    public fixed int hiddenEnableOffsets[16];
}

internal unsafe struct JobEntityBatch<TJob, T0, T1> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0, T1>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
{
    public fixed int componentIDs[2];
    public fixed bool componentRW[2];

    public TJob userJob;
    public UnsafeList<JobBatchContext2> batches;
    public EntityQueryMask mask;
    public uint version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        ref var batch = ref ((JobBatchContext2*)batches.GetUnsafePtr())[loopIndex];

        var pChunk = batch.chunk;
        var pVersions = batch.chunkVersions;
        var pSharedBlob = batch.sharedDataBlob;
        var count = batch.chunkCount;

        var off0 = batch.offset0;
        var enableOff0 = batch.enableOff0;
        var versionIndex0 = batch.versionIndex0;
        var off1 = batch.offset1;
        var enableOff1 = batch.enableOff1;
        var versionIndex1 = batch.versionIndex1;

        var pEntity = (Entity*)(pChunk + batch.entityOffset);

        var ptr0 = (T0*)(ComponentTypeID<T0>.IsShared ? (pSharedBlob + off0) : (pChunk + off0));
        var ptr1 = (T1*)(ComponentTypeID<T1>.IsShared ? (pSharedBlob + off1) : (pChunk + off1));

        if (componentRW[0])
        {
            pVersions[versionIndex0] = version;
        }

        if (componentRW[1])
        {
            pVersions[versionIndex1] = version;
        }


        // Execute batch
        var ulongCount = (count + 63) / 64;
        for (var block = 0; block < ulongCount; block++)
        {
            var validMask = ulong.MaxValue;
            var remaining = count - (block * 64);
            if (remaining < 64) validMask = (1UL << remaining) - 1UL;

            // Enforce enableable bits checking based on components required by Job Signature
            if (enableOff0 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff0);
                validMask &= pMask[block];
            }
            if (enableOff1 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff1);
                validMask &= pMask[block];
            }

            // Enforce EntityQuery Mask hidden enableable constraints
            for (var h = 0; h < batch.hiddenEnableCount; h++)
            {
                var pMask = (ulong*)(pChunk + batch.hiddenEnableOffsets[h]);
                validMask &= pMask[block];
            }

            while (validMask != 0)
            {
                var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                var i_ent = (block * 64) + bit;

                userJob.Execute(pEntity[i_ent],
                    ref (ComponentTypeID<T0>.IsShared ? ref ptr0[0] : ref ptr0[i_ent]),
                    ref (ComponentTypeID<T1>.IsShared ? ref ptr1[0] : ref ptr1[i_ent])
, in ctx);

                validMask ^= (1UL << bit);
            }
        }
    }
}

public interface IJobEntity<T0, T1, T2>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
{
    void Execute(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref readonly JobExecutionContext ctx);
}

internal unsafe struct JobBatchContext3
{
    public byte* chunk;
    public uint* chunkVersions;
    public byte* sharedDataBlob;
    public int chunkCount;
    public int entityOffset;

    public int offset0;
    public int enableOff0;
    public int versionIndex0;

    public int offset1;
    public int enableOff1;
    public int versionIndex1;

    public int offset2;
    public int enableOff2;
    public int versionIndex2;

    public int hiddenEnableCount;
    public fixed int hiddenEnableOffsets[16];
}

internal unsafe struct JobEntityBatch<TJob, T0, T1, T2> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0, T1, T2>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
{
    public fixed int componentIDs[3];
    public fixed bool componentRW[3];

    public TJob userJob;
    public UnsafeList<JobBatchContext3> batches;
    public EntityQueryMask mask;
    public uint version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        ref var batch = ref ((JobBatchContext3*)batches.GetUnsafePtr())[loopIndex];

        var pChunk = batch.chunk;
        var pVersions = batch.chunkVersions;
        var pSharedBlob = batch.sharedDataBlob;
        var count = batch.chunkCount;

        var off0 = batch.offset0;
        var enableOff0 = batch.enableOff0;
        var versionIndex0 = batch.versionIndex0;
        var off1 = batch.offset1;
        var enableOff1 = batch.enableOff1;
        var versionIndex1 = batch.versionIndex1;
        var off2 = batch.offset2;
        var enableOff2 = batch.enableOff2;
        var versionIndex2 = batch.versionIndex2;

        var pEntity = (Entity*)(pChunk + batch.entityOffset);

        var ptr0 = (T0*)(ComponentTypeID<T0>.IsShared ? (pSharedBlob + off0) : (pChunk + off0));
        var ptr1 = (T1*)(ComponentTypeID<T1>.IsShared ? (pSharedBlob + off1) : (pChunk + off1));
        var ptr2 = (T2*)(ComponentTypeID<T2>.IsShared ? (pSharedBlob + off2) : (pChunk + off2));

        if (componentRW[0])
        {
            pVersions[versionIndex0] = version;
        }

        if (componentRW[1])
        {
            pVersions[versionIndex1] = version;
        }

        if (componentRW[2])
        {
            pVersions[versionIndex2] = version;
        }


        // Execute batch
        var ulongCount = (count + 63) / 64;
        for (var block = 0; block < ulongCount; block++)
        {
            var validMask = ulong.MaxValue;
            var remaining = count - (block * 64);
            if (remaining < 64) validMask = (1UL << remaining) - 1UL;

            // Enforce enableable bits checking based on components required by Job Signature
            if (enableOff0 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff0);
                validMask &= pMask[block];
            }
            if (enableOff1 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff1);
                validMask &= pMask[block];
            }
            if (enableOff2 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff2);
                validMask &= pMask[block];
            }

            // Enforce EntityQuery Mask hidden enableable constraints
            for (var h = 0; h < batch.hiddenEnableCount; h++)
            {
                var pMask = (ulong*)(pChunk + batch.hiddenEnableOffsets[h]);
                validMask &= pMask[block];
            }

            while (validMask != 0)
            {
                var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                var i_ent = (block * 64) + bit;

                userJob.Execute(pEntity[i_ent],
                    ref (ComponentTypeID<T0>.IsShared ? ref ptr0[0] : ref ptr0[i_ent]),
                    ref (ComponentTypeID<T1>.IsShared ? ref ptr1[0] : ref ptr1[i_ent]),
                    ref (ComponentTypeID<T2>.IsShared ? ref ptr2[0] : ref ptr2[i_ent])
, in ctx);

                validMask ^= (1UL << bit);
            }
        }
    }
}

public interface IJobEntity<T0, T1, T2, T3>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
{
    void Execute(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref readonly JobExecutionContext ctx);
}

internal unsafe struct JobBatchContext4
{
    public byte* chunk;
    public uint* chunkVersions;
    public byte* sharedDataBlob;
    public int chunkCount;
    public int entityOffset;

    public int offset0;
    public int enableOff0;
    public int versionIndex0;

    public int offset1;
    public int enableOff1;
    public int versionIndex1;

    public int offset2;
    public int enableOff2;
    public int versionIndex2;

    public int offset3;
    public int enableOff3;
    public int versionIndex3;

    public int hiddenEnableCount;
    public fixed int hiddenEnableOffsets[16];
}

internal unsafe struct JobEntityBatch<TJob, T0, T1, T2, T3> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0, T1, T2, T3>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
{
    public fixed int componentIDs[4];
    public fixed bool componentRW[4];

    public TJob userJob;
    public UnsafeList<JobBatchContext4> batches;
    public EntityQueryMask mask;
    public uint version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        ref var batch = ref ((JobBatchContext4*)batches.GetUnsafePtr())[loopIndex];

        var pChunk = batch.chunk;
        var pVersions = batch.chunkVersions;
        var pSharedBlob = batch.sharedDataBlob;
        var count = batch.chunkCount;

        var off0 = batch.offset0;
        var enableOff0 = batch.enableOff0;
        var versionIndex0 = batch.versionIndex0;
        var off1 = batch.offset1;
        var enableOff1 = batch.enableOff1;
        var versionIndex1 = batch.versionIndex1;
        var off2 = batch.offset2;
        var enableOff2 = batch.enableOff2;
        var versionIndex2 = batch.versionIndex2;
        var off3 = batch.offset3;
        var enableOff3 = batch.enableOff3;
        var versionIndex3 = batch.versionIndex3;

        var pEntity = (Entity*)(pChunk + batch.entityOffset);

        var ptr0 = (T0*)(ComponentTypeID<T0>.IsShared ? (pSharedBlob + off0) : (pChunk + off0));
        var ptr1 = (T1*)(ComponentTypeID<T1>.IsShared ? (pSharedBlob + off1) : (pChunk + off1));
        var ptr2 = (T2*)(ComponentTypeID<T2>.IsShared ? (pSharedBlob + off2) : (pChunk + off2));
        var ptr3 = (T3*)(ComponentTypeID<T3>.IsShared ? (pSharedBlob + off3) : (pChunk + off3));

        if (componentRW[0])
        {
            pVersions[versionIndex0] = version;
        }

        if (componentRW[1])
        {
            pVersions[versionIndex1] = version;
        }

        if (componentRW[2])
        {
            pVersions[versionIndex2] = version;
        }

        if (componentRW[3])
        {
            pVersions[versionIndex3] = version;
        }


        // Execute batch
        var ulongCount = (count + 63) / 64;
        for (var block = 0; block < ulongCount; block++)
        {
            var validMask = ulong.MaxValue;
            var remaining = count - (block * 64);
            if (remaining < 64) validMask = (1UL << remaining) - 1UL;

            // Enforce enableable bits checking based on components required by Job Signature
            if (enableOff0 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff0);
                validMask &= pMask[block];
            }
            if (enableOff1 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff1);
                validMask &= pMask[block];
            }
            if (enableOff2 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff2);
                validMask &= pMask[block];
            }
            if (enableOff3 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff3);
                validMask &= pMask[block];
            }

            // Enforce EntityQuery Mask hidden enableable constraints
            for (var h = 0; h < batch.hiddenEnableCount; h++)
            {
                var pMask = (ulong*)(pChunk + batch.hiddenEnableOffsets[h]);
                validMask &= pMask[block];
            }

            while (validMask != 0)
            {
                var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                var i_ent = (block * 64) + bit;

                userJob.Execute(pEntity[i_ent],
                    ref (ComponentTypeID<T0>.IsShared ? ref ptr0[0] : ref ptr0[i_ent]),
                    ref (ComponentTypeID<T1>.IsShared ? ref ptr1[0] : ref ptr1[i_ent]),
                    ref (ComponentTypeID<T2>.IsShared ? ref ptr2[0] : ref ptr2[i_ent]),
                    ref (ComponentTypeID<T3>.IsShared ? ref ptr3[0] : ref ptr3[i_ent])
, in ctx);

                validMask ^= (1UL << bit);
            }
        }
    }
}

public interface IJobEntity<T0, T1, T2, T3, T4>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
{
    void Execute(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref readonly JobExecutionContext ctx);
}

internal unsafe struct JobBatchContext5
{
    public byte* chunk;
    public uint* chunkVersions;
    public byte* sharedDataBlob;
    public int chunkCount;
    public int entityOffset;

    public int offset0;
    public int enableOff0;
    public int versionIndex0;

    public int offset1;
    public int enableOff1;
    public int versionIndex1;

    public int offset2;
    public int enableOff2;
    public int versionIndex2;

    public int offset3;
    public int enableOff3;
    public int versionIndex3;

    public int offset4;
    public int enableOff4;
    public int versionIndex4;

    public int hiddenEnableCount;
    public fixed int hiddenEnableOffsets[16];
}

internal unsafe struct JobEntityBatch<TJob, T0, T1, T2, T3, T4> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0, T1, T2, T3, T4>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
{
    public fixed int componentIDs[5];
    public fixed bool componentRW[5];

    public TJob userJob;
    public UnsafeList<JobBatchContext5> batches;
    public EntityQueryMask mask;
    public uint version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        ref var batch = ref ((JobBatchContext5*)batches.GetUnsafePtr())[loopIndex];

        var pChunk = batch.chunk;
        var pVersions = batch.chunkVersions;
        var pSharedBlob = batch.sharedDataBlob;
        var count = batch.chunkCount;

        var off0 = batch.offset0;
        var enableOff0 = batch.enableOff0;
        var versionIndex0 = batch.versionIndex0;
        var off1 = batch.offset1;
        var enableOff1 = batch.enableOff1;
        var versionIndex1 = batch.versionIndex1;
        var off2 = batch.offset2;
        var enableOff2 = batch.enableOff2;
        var versionIndex2 = batch.versionIndex2;
        var off3 = batch.offset3;
        var enableOff3 = batch.enableOff3;
        var versionIndex3 = batch.versionIndex3;
        var off4 = batch.offset4;
        var enableOff4 = batch.enableOff4;
        var versionIndex4 = batch.versionIndex4;

        var pEntity = (Entity*)(pChunk + batch.entityOffset);

        var ptr0 = (T0*)(ComponentTypeID<T0>.IsShared ? (pSharedBlob + off0) : (pChunk + off0));
        var ptr1 = (T1*)(ComponentTypeID<T1>.IsShared ? (pSharedBlob + off1) : (pChunk + off1));
        var ptr2 = (T2*)(ComponentTypeID<T2>.IsShared ? (pSharedBlob + off2) : (pChunk + off2));
        var ptr3 = (T3*)(ComponentTypeID<T3>.IsShared ? (pSharedBlob + off3) : (pChunk + off3));
        var ptr4 = (T4*)(ComponentTypeID<T4>.IsShared ? (pSharedBlob + off4) : (pChunk + off4));

        if (componentRW[0])
        {
            pVersions[versionIndex0] = version;
        }

        if (componentRW[1])
        {
            pVersions[versionIndex1] = version;
        }

        if (componentRW[2])
        {
            pVersions[versionIndex2] = version;
        }

        if (componentRW[3])
        {
            pVersions[versionIndex3] = version;
        }

        if (componentRW[4])
        {
            pVersions[versionIndex4] = version;
        }


        // Execute batch
        var ulongCount = (count + 63) / 64;
        for (var block = 0; block < ulongCount; block++)
        {
            var validMask = ulong.MaxValue;
            var remaining = count - (block * 64);
            if (remaining < 64) validMask = (1UL << remaining) - 1UL;

            // Enforce enableable bits checking based on components required by Job Signature
            if (enableOff0 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff0);
                validMask &= pMask[block];
            }
            if (enableOff1 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff1);
                validMask &= pMask[block];
            }
            if (enableOff2 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff2);
                validMask &= pMask[block];
            }
            if (enableOff3 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff3);
                validMask &= pMask[block];
            }
            if (enableOff4 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff4);
                validMask &= pMask[block];
            }

            // Enforce EntityQuery Mask hidden enableable constraints
            for (var h = 0; h < batch.hiddenEnableCount; h++)
            {
                var pMask = (ulong*)(pChunk + batch.hiddenEnableOffsets[h]);
                validMask &= pMask[block];
            }

            while (validMask != 0)
            {
                var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                var i_ent = (block * 64) + bit;

                userJob.Execute(pEntity[i_ent],
                    ref (ComponentTypeID<T0>.IsShared ? ref ptr0[0] : ref ptr0[i_ent]),
                    ref (ComponentTypeID<T1>.IsShared ? ref ptr1[0] : ref ptr1[i_ent]),
                    ref (ComponentTypeID<T2>.IsShared ? ref ptr2[0] : ref ptr2[i_ent]),
                    ref (ComponentTypeID<T3>.IsShared ? ref ptr3[0] : ref ptr3[i_ent]),
                    ref (ComponentTypeID<T4>.IsShared ? ref ptr4[0] : ref ptr4[i_ent])
, in ctx);

                validMask ^= (1UL << bit);
            }
        }
    }
}

public interface IJobEntity<T0, T1, T2, T3, T4, T5>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
{
    void Execute(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5, ref readonly JobExecutionContext ctx);
}

internal unsafe struct JobBatchContext6
{
    public byte* chunk;
    public uint* chunkVersions;
    public byte* sharedDataBlob;
    public int chunkCount;
    public int entityOffset;

    public int offset0;
    public int enableOff0;
    public int versionIndex0;

    public int offset1;
    public int enableOff1;
    public int versionIndex1;

    public int offset2;
    public int enableOff2;
    public int versionIndex2;

    public int offset3;
    public int enableOff3;
    public int versionIndex3;

    public int offset4;
    public int enableOff4;
    public int versionIndex4;

    public int offset5;
    public int enableOff5;
    public int versionIndex5;

    public int hiddenEnableCount;
    public fixed int hiddenEnableOffsets[16];
}

internal unsafe struct JobEntityBatch<TJob, T0, T1, T2, T3, T4, T5> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0, T1, T2, T3, T4, T5>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
{
    public fixed int componentIDs[6];
    public fixed bool componentRW[6];

    public TJob userJob;
    public UnsafeList<JobBatchContext6> batches;
    public EntityQueryMask mask;
    public uint version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        ref var batch = ref ((JobBatchContext6*)batches.GetUnsafePtr())[loopIndex];

        var pChunk = batch.chunk;
        var pVersions = batch.chunkVersions;
        var pSharedBlob = batch.sharedDataBlob;
        var count = batch.chunkCount;

        var off0 = batch.offset0;
        var enableOff0 = batch.enableOff0;
        var versionIndex0 = batch.versionIndex0;
        var off1 = batch.offset1;
        var enableOff1 = batch.enableOff1;
        var versionIndex1 = batch.versionIndex1;
        var off2 = batch.offset2;
        var enableOff2 = batch.enableOff2;
        var versionIndex2 = batch.versionIndex2;
        var off3 = batch.offset3;
        var enableOff3 = batch.enableOff3;
        var versionIndex3 = batch.versionIndex3;
        var off4 = batch.offset4;
        var enableOff4 = batch.enableOff4;
        var versionIndex4 = batch.versionIndex4;
        var off5 = batch.offset5;
        var enableOff5 = batch.enableOff5;
        var versionIndex5 = batch.versionIndex5;

        var pEntity = (Entity*)(pChunk + batch.entityOffset);

        var ptr0 = (T0*)(ComponentTypeID<T0>.IsShared ? (pSharedBlob + off0) : (pChunk + off0));
        var ptr1 = (T1*)(ComponentTypeID<T1>.IsShared ? (pSharedBlob + off1) : (pChunk + off1));
        var ptr2 = (T2*)(ComponentTypeID<T2>.IsShared ? (pSharedBlob + off2) : (pChunk + off2));
        var ptr3 = (T3*)(ComponentTypeID<T3>.IsShared ? (pSharedBlob + off3) : (pChunk + off3));
        var ptr4 = (T4*)(ComponentTypeID<T4>.IsShared ? (pSharedBlob + off4) : (pChunk + off4));
        var ptr5 = (T5*)(ComponentTypeID<T5>.IsShared ? (pSharedBlob + off5) : (pChunk + off5));

        if (componentRW[0])
        {
            pVersions[versionIndex0] = version;
        }

        if (componentRW[1])
        {
            pVersions[versionIndex1] = version;
        }

        if (componentRW[2])
        {
            pVersions[versionIndex2] = version;
        }

        if (componentRW[3])
        {
            pVersions[versionIndex3] = version;
        }

        if (componentRW[4])
        {
            pVersions[versionIndex4] = version;
        }

        if (componentRW[5])
        {
            pVersions[versionIndex5] = version;
        }


        // Execute batch
        var ulongCount = (count + 63) / 64;
        for (var block = 0; block < ulongCount; block++)
        {
            var validMask = ulong.MaxValue;
            var remaining = count - (block * 64);
            if (remaining < 64) validMask = (1UL << remaining) - 1UL;

            // Enforce enableable bits checking based on components required by Job Signature
            if (enableOff0 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff0);
                validMask &= pMask[block];
            }
            if (enableOff1 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff1);
                validMask &= pMask[block];
            }
            if (enableOff2 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff2);
                validMask &= pMask[block];
            }
            if (enableOff3 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff3);
                validMask &= pMask[block];
            }
            if (enableOff4 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff4);
                validMask &= pMask[block];
            }
            if (enableOff5 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff5);
                validMask &= pMask[block];
            }

            // Enforce EntityQuery Mask hidden enableable constraints
            for (var h = 0; h < batch.hiddenEnableCount; h++)
            {
                var pMask = (ulong*)(pChunk + batch.hiddenEnableOffsets[h]);
                validMask &= pMask[block];
            }

            while (validMask != 0)
            {
                var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                var i_ent = (block * 64) + bit;

                userJob.Execute(pEntity[i_ent],
                    ref (ComponentTypeID<T0>.IsShared ? ref ptr0[0] : ref ptr0[i_ent]),
                    ref (ComponentTypeID<T1>.IsShared ? ref ptr1[0] : ref ptr1[i_ent]),
                    ref (ComponentTypeID<T2>.IsShared ? ref ptr2[0] : ref ptr2[i_ent]),
                    ref (ComponentTypeID<T3>.IsShared ? ref ptr3[0] : ref ptr3[i_ent]),
                    ref (ComponentTypeID<T4>.IsShared ? ref ptr4[0] : ref ptr4[i_ent]),
                    ref (ComponentTypeID<T5>.IsShared ? ref ptr5[0] : ref ptr5[i_ent])
, in ctx);

                validMask ^= (1UL << bit);
            }
        }
    }
}

public interface IJobEntity<T0, T1, T2, T3, T4, T5, T6>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where T6 : unmanaged, IComponent
{
    void Execute(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5, ref T6 component6, ref readonly JobExecutionContext ctx);
}

internal unsafe struct JobBatchContext7
{
    public byte* chunk;
    public uint* chunkVersions;
    public byte* sharedDataBlob;
    public int chunkCount;
    public int entityOffset;

    public int offset0;
    public int enableOff0;
    public int versionIndex0;

    public int offset1;
    public int enableOff1;
    public int versionIndex1;

    public int offset2;
    public int enableOff2;
    public int versionIndex2;

    public int offset3;
    public int enableOff3;
    public int versionIndex3;

    public int offset4;
    public int enableOff4;
    public int versionIndex4;

    public int offset5;
    public int enableOff5;
    public int versionIndex5;

    public int offset6;
    public int enableOff6;
    public int versionIndex6;

    public int hiddenEnableCount;
    public fixed int hiddenEnableOffsets[16];
}

internal unsafe struct JobEntityBatch<TJob, T0, T1, T2, T3, T4, T5, T6> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0, T1, T2, T3, T4, T5, T6>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where T6 : unmanaged, IComponent
{
    public fixed int componentIDs[7];
    public fixed bool componentRW[7];

    public TJob userJob;
    public UnsafeList<JobBatchContext7> batches;
    public EntityQueryMask mask;
    public uint version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        ref var batch = ref ((JobBatchContext7*)batches.GetUnsafePtr())[loopIndex];

        var pChunk = batch.chunk;
        var pVersions = batch.chunkVersions;
        var pSharedBlob = batch.sharedDataBlob;
        var count = batch.chunkCount;

        var off0 = batch.offset0;
        var enableOff0 = batch.enableOff0;
        var versionIndex0 = batch.versionIndex0;
        var off1 = batch.offset1;
        var enableOff1 = batch.enableOff1;
        var versionIndex1 = batch.versionIndex1;
        var off2 = batch.offset2;
        var enableOff2 = batch.enableOff2;
        var versionIndex2 = batch.versionIndex2;
        var off3 = batch.offset3;
        var enableOff3 = batch.enableOff3;
        var versionIndex3 = batch.versionIndex3;
        var off4 = batch.offset4;
        var enableOff4 = batch.enableOff4;
        var versionIndex4 = batch.versionIndex4;
        var off5 = batch.offset5;
        var enableOff5 = batch.enableOff5;
        var versionIndex5 = batch.versionIndex5;
        var off6 = batch.offset6;
        var enableOff6 = batch.enableOff6;
        var versionIndex6 = batch.versionIndex6;

        var pEntity = (Entity*)(pChunk + batch.entityOffset);

        var ptr0 = (T0*)(ComponentTypeID<T0>.IsShared ? (pSharedBlob + off0) : (pChunk + off0));
        var ptr1 = (T1*)(ComponentTypeID<T1>.IsShared ? (pSharedBlob + off1) : (pChunk + off1));
        var ptr2 = (T2*)(ComponentTypeID<T2>.IsShared ? (pSharedBlob + off2) : (pChunk + off2));
        var ptr3 = (T3*)(ComponentTypeID<T3>.IsShared ? (pSharedBlob + off3) : (pChunk + off3));
        var ptr4 = (T4*)(ComponentTypeID<T4>.IsShared ? (pSharedBlob + off4) : (pChunk + off4));
        var ptr5 = (T5*)(ComponentTypeID<T5>.IsShared ? (pSharedBlob + off5) : (pChunk + off5));
        var ptr6 = (T6*)(ComponentTypeID<T6>.IsShared ? (pSharedBlob + off6) : (pChunk + off6));

        if (componentRW[0])
        {
            pVersions[versionIndex0] = version;
        }

        if (componentRW[1])
        {
            pVersions[versionIndex1] = version;
        }

        if (componentRW[2])
        {
            pVersions[versionIndex2] = version;
        }

        if (componentRW[3])
        {
            pVersions[versionIndex3] = version;
        }

        if (componentRW[4])
        {
            pVersions[versionIndex4] = version;
        }

        if (componentRW[5])
        {
            pVersions[versionIndex5] = version;
        }

        if (componentRW[6])
        {
            pVersions[versionIndex6] = version;
        }


        // Execute batch
        var ulongCount = (count + 63) / 64;
        for (var block = 0; block < ulongCount; block++)
        {
            var validMask = ulong.MaxValue;
            var remaining = count - (block * 64);
            if (remaining < 64) validMask = (1UL << remaining) - 1UL;

            // Enforce enableable bits checking based on components required by Job Signature
            if (enableOff0 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff0);
                validMask &= pMask[block];
            }
            if (enableOff1 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff1);
                validMask &= pMask[block];
            }
            if (enableOff2 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff2);
                validMask &= pMask[block];
            }
            if (enableOff3 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff3);
                validMask &= pMask[block];
            }
            if (enableOff4 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff4);
                validMask &= pMask[block];
            }
            if (enableOff5 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff5);
                validMask &= pMask[block];
            }
            if (enableOff6 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff6);
                validMask &= pMask[block];
            }

            // Enforce EntityQuery Mask hidden enableable constraints
            for (var h = 0; h < batch.hiddenEnableCount; h++)
            {
                var pMask = (ulong*)(pChunk + batch.hiddenEnableOffsets[h]);
                validMask &= pMask[block];
            }

            while (validMask != 0)
            {
                var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                var i_ent = (block * 64) + bit;

                userJob.Execute(pEntity[i_ent],
                    ref (ComponentTypeID<T0>.IsShared ? ref ptr0[0] : ref ptr0[i_ent]),
                    ref (ComponentTypeID<T1>.IsShared ? ref ptr1[0] : ref ptr1[i_ent]),
                    ref (ComponentTypeID<T2>.IsShared ? ref ptr2[0] : ref ptr2[i_ent]),
                    ref (ComponentTypeID<T3>.IsShared ? ref ptr3[0] : ref ptr3[i_ent]),
                    ref (ComponentTypeID<T4>.IsShared ? ref ptr4[0] : ref ptr4[i_ent]),
                    ref (ComponentTypeID<T5>.IsShared ? ref ptr5[0] : ref ptr5[i_ent]),
                    ref (ComponentTypeID<T6>.IsShared ? ref ptr6[0] : ref ptr6[i_ent])
, in ctx);

                validMask ^= (1UL << bit);
            }
        }
    }
}

public interface IJobEntity<T0, T1, T2, T3, T4, T5, T6, T7>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where T6 : unmanaged, IComponent
    where T7 : unmanaged, IComponent
{
    void Execute(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5, ref T6 component6, ref T7 component7, ref readonly JobExecutionContext ctx);
}

internal unsafe struct JobBatchContext8
{
    public byte* chunk;
    public uint* chunkVersions;
    public byte* sharedDataBlob;
    public int chunkCount;
    public int entityOffset;

    public int offset0;
    public int enableOff0;
    public int versionIndex0;

    public int offset1;
    public int enableOff1;
    public int versionIndex1;

    public int offset2;
    public int enableOff2;
    public int versionIndex2;

    public int offset3;
    public int enableOff3;
    public int versionIndex3;

    public int offset4;
    public int enableOff4;
    public int versionIndex4;

    public int offset5;
    public int enableOff5;
    public int versionIndex5;

    public int offset6;
    public int enableOff6;
    public int versionIndex6;

    public int offset7;
    public int enableOff7;
    public int versionIndex7;

    public int hiddenEnableCount;
    public fixed int hiddenEnableOffsets[16];
}

internal unsafe struct JobEntityBatch<TJob, T0, T1, T2, T3, T4, T5, T6, T7> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0, T1, T2, T3, T4, T5, T6, T7>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where T6 : unmanaged, IComponent
    where T7 : unmanaged, IComponent
{
    public fixed int componentIDs[8];
    public fixed bool componentRW[8];

    public TJob userJob;
    public UnsafeList<JobBatchContext8> batches;
    public EntityQueryMask mask;
    public uint version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        ref var batch = ref ((JobBatchContext8*)batches.GetUnsafePtr())[loopIndex];

        var pChunk = batch.chunk;
        var pVersions = batch.chunkVersions;
        var pSharedBlob = batch.sharedDataBlob;
        var count = batch.chunkCount;

        var off0 = batch.offset0;
        var enableOff0 = batch.enableOff0;
        var versionIndex0 = batch.versionIndex0;
        var off1 = batch.offset1;
        var enableOff1 = batch.enableOff1;
        var versionIndex1 = batch.versionIndex1;
        var off2 = batch.offset2;
        var enableOff2 = batch.enableOff2;
        var versionIndex2 = batch.versionIndex2;
        var off3 = batch.offset3;
        var enableOff3 = batch.enableOff3;
        var versionIndex3 = batch.versionIndex3;
        var off4 = batch.offset4;
        var enableOff4 = batch.enableOff4;
        var versionIndex4 = batch.versionIndex4;
        var off5 = batch.offset5;
        var enableOff5 = batch.enableOff5;
        var versionIndex5 = batch.versionIndex5;
        var off6 = batch.offset6;
        var enableOff6 = batch.enableOff6;
        var versionIndex6 = batch.versionIndex6;
        var off7 = batch.offset7;
        var enableOff7 = batch.enableOff7;
        var versionIndex7 = batch.versionIndex7;

        var pEntity = (Entity*)(pChunk + batch.entityOffset);

        var ptr0 = (T0*)(ComponentTypeID<T0>.IsShared ? (pSharedBlob + off0) : (pChunk + off0));
        var ptr1 = (T1*)(ComponentTypeID<T1>.IsShared ? (pSharedBlob + off1) : (pChunk + off1));
        var ptr2 = (T2*)(ComponentTypeID<T2>.IsShared ? (pSharedBlob + off2) : (pChunk + off2));
        var ptr3 = (T3*)(ComponentTypeID<T3>.IsShared ? (pSharedBlob + off3) : (pChunk + off3));
        var ptr4 = (T4*)(ComponentTypeID<T4>.IsShared ? (pSharedBlob + off4) : (pChunk + off4));
        var ptr5 = (T5*)(ComponentTypeID<T5>.IsShared ? (pSharedBlob + off5) : (pChunk + off5));
        var ptr6 = (T6*)(ComponentTypeID<T6>.IsShared ? (pSharedBlob + off6) : (pChunk + off6));
        var ptr7 = (T7*)(ComponentTypeID<T7>.IsShared ? (pSharedBlob + off7) : (pChunk + off7));

        if (componentRW[0])
        {
            pVersions[versionIndex0] = version;
        }

        if (componentRW[1])
        {
            pVersions[versionIndex1] = version;
        }

        if (componentRW[2])
        {
            pVersions[versionIndex2] = version;
        }

        if (componentRW[3])
        {
            pVersions[versionIndex3] = version;
        }

        if (componentRW[4])
        {
            pVersions[versionIndex4] = version;
        }

        if (componentRW[5])
        {
            pVersions[versionIndex5] = version;
        }

        if (componentRW[6])
        {
            pVersions[versionIndex6] = version;
        }

        if (componentRW[7])
        {
            pVersions[versionIndex7] = version;
        }


        // Execute batch
        var ulongCount = (count + 63) / 64;
        for (var block = 0; block < ulongCount; block++)
        {
            var validMask = ulong.MaxValue;
            var remaining = count - (block * 64);
            if (remaining < 64) validMask = (1UL << remaining) - 1UL;

            // Enforce enableable bits checking based on components required by Job Signature
            if (enableOff0 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff0);
                validMask &= pMask[block];
            }
            if (enableOff1 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff1);
                validMask &= pMask[block];
            }
            if (enableOff2 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff2);
                validMask &= pMask[block];
            }
            if (enableOff3 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff3);
                validMask &= pMask[block];
            }
            if (enableOff4 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff4);
                validMask &= pMask[block];
            }
            if (enableOff5 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff5);
                validMask &= pMask[block];
            }
            if (enableOff6 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff6);
                validMask &= pMask[block];
            }
            if (enableOff7 != -1)
            {
                var pMask = (ulong*)(pChunk + enableOff7);
                validMask &= pMask[block];
            }

            // Enforce EntityQuery Mask hidden enableable constraints
            for (var h = 0; h < batch.hiddenEnableCount; h++)
            {
                var pMask = (ulong*)(pChunk + batch.hiddenEnableOffsets[h]);
                validMask &= pMask[block];
            }

            while (validMask != 0)
            {
                var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                var i_ent = (block * 64) + bit;

                userJob.Execute(pEntity[i_ent],
                    ref (ComponentTypeID<T0>.IsShared ? ref ptr0[0] : ref ptr0[i_ent]),
                    ref (ComponentTypeID<T1>.IsShared ? ref ptr1[0] : ref ptr1[i_ent]),
                    ref (ComponentTypeID<T2>.IsShared ? ref ptr2[0] : ref ptr2[i_ent]),
                    ref (ComponentTypeID<T3>.IsShared ? ref ptr3[0] : ref ptr3[i_ent]),
                    ref (ComponentTypeID<T4>.IsShared ? ref ptr4[0] : ref ptr4[i_ent]),
                    ref (ComponentTypeID<T5>.IsShared ? ref ptr5[0] : ref ptr5[i_ent]),
                    ref (ComponentTypeID<T6>.IsShared ? ref ptr6[0] : ref ptr6[i_ent]),
                    ref (ComponentTypeID<T7>.IsShared ? ref ptr7[0] : ref ptr7[i_ent])
, in ctx);

                validMask ^= (1UL << bit);
            }
        }
    }
}

public unsafe partial struct EntityQuery
{
    private struct DisposeJobEntity1 : IJob
    {
        public UnsafeList<JobBatchContext1> batches;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            batches.Dispose();
        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0>(TJob jobData, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobEntity<T0>
        where T0 : unmanaged, IComponent
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

        var batches = new UnsafeList<JobBatchContext1>(128, TempJobAllocator.AllocationHandle);
        var hiddenOffsets = stackalloc int[16];

        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            int off0;
            int enableOff0;
            int versionIdx0;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = -1;
                versionIdx0 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = layout.enableBitsOffset;
                versionIdx0 = layout.versionIndex;
            }

            var hiddenCount = 0;
            var itE = _mask.requireEnabled.GetIterator();
            while (itE.Next(out var id) && hiddenCount < 16)
            {
                var found = false;
                if (id == ComponentTypeID<T0>.Value) found = true;
                if (!found)
                {
                    var layout = arch.GetLayout(id);
                    if (layout.Error == Error.None && layout.Value.enableBitsOffset != -1)
                    {
                        hiddenOffsets[hiddenCount++] = layout.Value.enableBitsOffset;
                    }
                }
            }

            for (var chunkIdx = 0; chunkIdx < arch.ChunkCount; chunkIdx++)
            {
                ref var chunkRef = ref arch.GetChunkReference(chunkIdx);

                byte* pSharedBlob = null;
                if (arch._chunkGroups.Count > 0 && chunkRef._groupIndex >= 0 && chunkRef._groupIndex < arch._chunkGroups.Count)
                {
                    var sharedSpan = arch._chunkGroups[chunkRef._groupIndex].sharedData;
                    if (sharedSpan.IsCreated)
                    {
                        pSharedBlob = (byte*)sharedSpan.GetUnsafePtr();
                    }
                }

                var ctx = new JobBatchContext1
                {
                    chunk = chunkRef.GetUnsafePtr(),
                    chunkVersions = chunkRef.GetVersionUnsafePtr(),
                    chunkCount = chunkRef._count,
                    entityOffset = arch.EntityIDsOffset,
                    sharedDataBlob = pSharedBlob,

                    offset0 = off0,
                    enableOff0 = enableOff0,
                    versionIndex0 = versionIdx0,
                    hiddenEnableCount = hiddenCount,
                };

                for (var h = 0; h < hiddenCount; h++)
                {
                    ctx.hiddenEnableOffsets[h] = hiddenOffsets[h];
                }
                batches.Add(ctx);
            }
        }

        var runner = new JobEntityBatch<TJob, T0>
        {
            userJob = jobData,
            batches = batches,
            mask = _mask,
            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var idx = 0; idx < 1; idx++)
            {
                if (id == runner.componentIDs[idx])
                {
                    runner.componentRW[idx] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, batches.Count, batchSize, dependency);

        var disposeJob = new DisposeJobEntity1
        {
            batches = batches,
        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity2 : IJob
    {
        public UnsafeList<JobBatchContext2> batches;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            batches.Dispose();
        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0, T1>(TJob jobData, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobEntity<T0, T1>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
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

        var batches = new UnsafeList<JobBatchContext2>(128, TempJobAllocator.AllocationHandle);
        var hiddenOffsets = stackalloc int[16];

        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            int off0;
            int enableOff0;
            int versionIdx0;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = -1;
                versionIdx0 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = layout.enableBitsOffset;
                versionIdx0 = layout.versionIndex;
            }
            int off1;
            int enableOff1;
            int versionIdx1;
            if (ComponentTypeID<T1>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
                off1 = layout.offset;
                enableOff1 = -1;
                versionIdx1 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
                off1 = layout.offset;
                enableOff1 = layout.enableBitsOffset;
                versionIdx1 = layout.versionIndex;
            }

            var hiddenCount = 0;
            var itE = _mask.requireEnabled.GetIterator();
            while (itE.Next(out var id) && hiddenCount < 16)
            {
                var found = false;
                if (id == ComponentTypeID<T0>.Value) found = true;
                if (id == ComponentTypeID<T1>.Value) found = true;
                if (!found)
                {
                    var layout = arch.GetLayout(id);
                    if (layout.Error == Error.None && layout.Value.enableBitsOffset != -1)
                    {
                        hiddenOffsets[hiddenCount++] = layout.Value.enableBitsOffset;
                    }
                }
            }

            for (var chunkIdx = 0; chunkIdx < arch.ChunkCount; chunkIdx++)
            {
                ref var chunkRef = ref arch.GetChunkReference(chunkIdx);

                byte* pSharedBlob = null;
                if (arch._chunkGroups.Count > 0 && chunkRef._groupIndex >= 0 && chunkRef._groupIndex < arch._chunkGroups.Count)
                {
                    var sharedSpan = arch._chunkGroups[chunkRef._groupIndex].sharedData;
                    if (sharedSpan.IsCreated)
                    {
                        pSharedBlob = (byte*)sharedSpan.GetUnsafePtr();
                    }
                }

                var ctx = new JobBatchContext2
                {
                    chunk = chunkRef.GetUnsafePtr(),
                    chunkVersions = chunkRef.GetVersionUnsafePtr(),
                    chunkCount = chunkRef._count,
                    entityOffset = arch.EntityIDsOffset,
                    sharedDataBlob = pSharedBlob,

                    offset0 = off0,
                    enableOff0 = enableOff0,
                    versionIndex0 = versionIdx0,
                    offset1 = off1,
                    enableOff1 = enableOff1,
                    versionIndex1 = versionIdx1,
                    hiddenEnableCount = hiddenCount,
                };

                for (var h = 0; h < hiddenCount; h++)
                {
                    ctx.hiddenEnableOffsets[h] = hiddenOffsets[h];
                }
                batches.Add(ctx);
            }
        }

        var runner = new JobEntityBatch<TJob, T0, T1>
        {
            userJob = jobData,
            batches = batches,
            mask = _mask,
            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;
        runner.componentIDs[1] = ComponentTypeID<T1>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var idx = 0; idx < 2; idx++)
            {
                if (id == runner.componentIDs[idx])
                {
                    runner.componentRW[idx] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, batches.Count, batchSize, dependency);

        var disposeJob = new DisposeJobEntity2
        {
            batches = batches,
        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity3 : IJob
    {
        public UnsafeList<JobBatchContext3> batches;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            batches.Dispose();
        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0, T1, T2>(TJob jobData, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobEntity<T0, T1, T2>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
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

        var batches = new UnsafeList<JobBatchContext3>(128, TempJobAllocator.AllocationHandle);
        var hiddenOffsets = stackalloc int[16];

        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            int off0;
            int enableOff0;
            int versionIdx0;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = -1;
                versionIdx0 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = layout.enableBitsOffset;
                versionIdx0 = layout.versionIndex;
            }
            int off1;
            int enableOff1;
            int versionIdx1;
            if (ComponentTypeID<T1>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
                off1 = layout.offset;
                enableOff1 = -1;
                versionIdx1 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
                off1 = layout.offset;
                enableOff1 = layout.enableBitsOffset;
                versionIdx1 = layout.versionIndex;
            }
            int off2;
            int enableOff2;
            int versionIdx2;
            if (ComponentTypeID<T2>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
                off2 = layout.offset;
                enableOff2 = -1;
                versionIdx2 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
                off2 = layout.offset;
                enableOff2 = layout.enableBitsOffset;
                versionIdx2 = layout.versionIndex;
            }

            var hiddenCount = 0;
            var itE = _mask.requireEnabled.GetIterator();
            while (itE.Next(out var id) && hiddenCount < 16)
            {
                var found = false;
                if (id == ComponentTypeID<T0>.Value) found = true;
                if (id == ComponentTypeID<T1>.Value) found = true;
                if (id == ComponentTypeID<T2>.Value) found = true;
                if (!found)
                {
                    var layout = arch.GetLayout(id);
                    if (layout.Error == Error.None && layout.Value.enableBitsOffset != -1)
                    {
                        hiddenOffsets[hiddenCount++] = layout.Value.enableBitsOffset;
                    }
                }
            }

            for (var chunkIdx = 0; chunkIdx < arch.ChunkCount; chunkIdx++)
            {
                ref var chunkRef = ref arch.GetChunkReference(chunkIdx);

                byte* pSharedBlob = null;
                if (arch._chunkGroups.Count > 0 && chunkRef._groupIndex >= 0 && chunkRef._groupIndex < arch._chunkGroups.Count)
                {
                    var sharedSpan = arch._chunkGroups[chunkRef._groupIndex].sharedData;
                    if (sharedSpan.IsCreated)
                    {
                        pSharedBlob = (byte*)sharedSpan.GetUnsafePtr();
                    }
                }

                var ctx = new JobBatchContext3
                {
                    chunk = chunkRef.GetUnsafePtr(),
                    chunkVersions = chunkRef.GetVersionUnsafePtr(),
                    chunkCount = chunkRef._count,
                    entityOffset = arch.EntityIDsOffset,
                    sharedDataBlob = pSharedBlob,

                    offset0 = off0,
                    enableOff0 = enableOff0,
                    versionIndex0 = versionIdx0,
                    offset1 = off1,
                    enableOff1 = enableOff1,
                    versionIndex1 = versionIdx1,
                    offset2 = off2,
                    enableOff2 = enableOff2,
                    versionIndex2 = versionIdx2,
                    hiddenEnableCount = hiddenCount,
                };

                for (var h = 0; h < hiddenCount; h++)
                {
                    ctx.hiddenEnableOffsets[h] = hiddenOffsets[h];
                }
                batches.Add(ctx);
            }
        }

        var runner = new JobEntityBatch<TJob, T0, T1, T2>
        {
            userJob = jobData,
            batches = batches,
            mask = _mask,
            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;
        runner.componentIDs[1] = ComponentTypeID<T1>.Value;
        runner.componentIDs[2] = ComponentTypeID<T2>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var idx = 0; idx < 3; idx++)
            {
                if (id == runner.componentIDs[idx])
                {
                    runner.componentRW[idx] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, batches.Count, batchSize, dependency);

        var disposeJob = new DisposeJobEntity3
        {
            batches = batches,
        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity4 : IJob
    {
        public UnsafeList<JobBatchContext4> batches;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            batches.Dispose();
        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0, T1, T2, T3>(TJob jobData, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobEntity<T0, T1, T2, T3>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
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

        var batches = new UnsafeList<JobBatchContext4>(128, TempJobAllocator.AllocationHandle);
        var hiddenOffsets = stackalloc int[16];

        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            int off0;
            int enableOff0;
            int versionIdx0;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = -1;
                versionIdx0 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = layout.enableBitsOffset;
                versionIdx0 = layout.versionIndex;
            }
            int off1;
            int enableOff1;
            int versionIdx1;
            if (ComponentTypeID<T1>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
                off1 = layout.offset;
                enableOff1 = -1;
                versionIdx1 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
                off1 = layout.offset;
                enableOff1 = layout.enableBitsOffset;
                versionIdx1 = layout.versionIndex;
            }
            int off2;
            int enableOff2;
            int versionIdx2;
            if (ComponentTypeID<T2>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
                off2 = layout.offset;
                enableOff2 = -1;
                versionIdx2 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
                off2 = layout.offset;
                enableOff2 = layout.enableBitsOffset;
                versionIdx2 = layout.versionIndex;
            }
            int off3;
            int enableOff3;
            int versionIdx3;
            if (ComponentTypeID<T3>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T3>.Value).GetValueOrThrow();
                off3 = layout.offset;
                enableOff3 = -1;
                versionIdx3 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T3>.Value).GetValueOrThrow();
                off3 = layout.offset;
                enableOff3 = layout.enableBitsOffset;
                versionIdx3 = layout.versionIndex;
            }

            var hiddenCount = 0;
            var itE = _mask.requireEnabled.GetIterator();
            while (itE.Next(out var id) && hiddenCount < 16)
            {
                var found = false;
                if (id == ComponentTypeID<T0>.Value) found = true;
                if (id == ComponentTypeID<T1>.Value) found = true;
                if (id == ComponentTypeID<T2>.Value) found = true;
                if (id == ComponentTypeID<T3>.Value) found = true;
                if (!found)
                {
                    var layout = arch.GetLayout(id);
                    if (layout.Error == Error.None && layout.Value.enableBitsOffset != -1)
                    {
                        hiddenOffsets[hiddenCount++] = layout.Value.enableBitsOffset;
                    }
                }
            }

            for (var chunkIdx = 0; chunkIdx < arch.ChunkCount; chunkIdx++)
            {
                ref var chunkRef = ref arch.GetChunkReference(chunkIdx);

                byte* pSharedBlob = null;
                if (arch._chunkGroups.Count > 0 && chunkRef._groupIndex >= 0 && chunkRef._groupIndex < arch._chunkGroups.Count)
                {
                    var sharedSpan = arch._chunkGroups[chunkRef._groupIndex].sharedData;
                    if (sharedSpan.IsCreated)
                    {
                        pSharedBlob = (byte*)sharedSpan.GetUnsafePtr();
                    }
                }

                var ctx = new JobBatchContext4
                {
                    chunk = chunkRef.GetUnsafePtr(),
                    chunkVersions = chunkRef.GetVersionUnsafePtr(),
                    chunkCount = chunkRef._count,
                    entityOffset = arch.EntityIDsOffset,
                    sharedDataBlob = pSharedBlob,

                    offset0 = off0,
                    enableOff0 = enableOff0,
                    versionIndex0 = versionIdx0,
                    offset1 = off1,
                    enableOff1 = enableOff1,
                    versionIndex1 = versionIdx1,
                    offset2 = off2,
                    enableOff2 = enableOff2,
                    versionIndex2 = versionIdx2,
                    offset3 = off3,
                    enableOff3 = enableOff3,
                    versionIndex3 = versionIdx3,
                    hiddenEnableCount = hiddenCount,
                };

                for (var h = 0; h < hiddenCount; h++)
                {
                    ctx.hiddenEnableOffsets[h] = hiddenOffsets[h];
                }
                batches.Add(ctx);
            }
        }

        var runner = new JobEntityBatch<TJob, T0, T1, T2, T3>
        {
            userJob = jobData,
            batches = batches,
            mask = _mask,
            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;
        runner.componentIDs[1] = ComponentTypeID<T1>.Value;
        runner.componentIDs[2] = ComponentTypeID<T2>.Value;
        runner.componentIDs[3] = ComponentTypeID<T3>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var idx = 0; idx < 4; idx++)
            {
                if (id == runner.componentIDs[idx])
                {
                    runner.componentRW[idx] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, batches.Count, batchSize, dependency);

        var disposeJob = new DisposeJobEntity4
        {
            batches = batches,
        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity5 : IJob
    {
        public UnsafeList<JobBatchContext5> batches;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            batches.Dispose();
        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0, T1, T2, T3, T4>(TJob jobData, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobEntity<T0, T1, T2, T3, T4>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
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

        var batches = new UnsafeList<JobBatchContext5>(128, TempJobAllocator.AllocationHandle);
        var hiddenOffsets = stackalloc int[16];

        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            int off0;
            int enableOff0;
            int versionIdx0;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = -1;
                versionIdx0 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = layout.enableBitsOffset;
                versionIdx0 = layout.versionIndex;
            }
            int off1;
            int enableOff1;
            int versionIdx1;
            if (ComponentTypeID<T1>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
                off1 = layout.offset;
                enableOff1 = -1;
                versionIdx1 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
                off1 = layout.offset;
                enableOff1 = layout.enableBitsOffset;
                versionIdx1 = layout.versionIndex;
            }
            int off2;
            int enableOff2;
            int versionIdx2;
            if (ComponentTypeID<T2>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
                off2 = layout.offset;
                enableOff2 = -1;
                versionIdx2 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
                off2 = layout.offset;
                enableOff2 = layout.enableBitsOffset;
                versionIdx2 = layout.versionIndex;
            }
            int off3;
            int enableOff3;
            int versionIdx3;
            if (ComponentTypeID<T3>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T3>.Value).GetValueOrThrow();
                off3 = layout.offset;
                enableOff3 = -1;
                versionIdx3 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T3>.Value).GetValueOrThrow();
                off3 = layout.offset;
                enableOff3 = layout.enableBitsOffset;
                versionIdx3 = layout.versionIndex;
            }
            int off4;
            int enableOff4;
            int versionIdx4;
            if (ComponentTypeID<T4>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T4>.Value).GetValueOrThrow();
                off4 = layout.offset;
                enableOff4 = -1;
                versionIdx4 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T4>.Value).GetValueOrThrow();
                off4 = layout.offset;
                enableOff4 = layout.enableBitsOffset;
                versionIdx4 = layout.versionIndex;
            }

            var hiddenCount = 0;
            var itE = _mask.requireEnabled.GetIterator();
            while (itE.Next(out var id) && hiddenCount < 16)
            {
                var found = false;
                if (id == ComponentTypeID<T0>.Value) found = true;
                if (id == ComponentTypeID<T1>.Value) found = true;
                if (id == ComponentTypeID<T2>.Value) found = true;
                if (id == ComponentTypeID<T3>.Value) found = true;
                if (id == ComponentTypeID<T4>.Value) found = true;
                if (!found)
                {
                    var layout = arch.GetLayout(id);
                    if (layout.Error == Error.None && layout.Value.enableBitsOffset != -1)
                    {
                        hiddenOffsets[hiddenCount++] = layout.Value.enableBitsOffset;
                    }
                }
            }

            for (var chunkIdx = 0; chunkIdx < arch.ChunkCount; chunkIdx++)
            {
                ref var chunkRef = ref arch.GetChunkReference(chunkIdx);

                byte* pSharedBlob = null;
                if (arch._chunkGroups.Count > 0 && chunkRef._groupIndex >= 0 && chunkRef._groupIndex < arch._chunkGroups.Count)
                {
                    var sharedSpan = arch._chunkGroups[chunkRef._groupIndex].sharedData;
                    if (sharedSpan.IsCreated)
                    {
                        pSharedBlob = (byte*)sharedSpan.GetUnsafePtr();
                    }
                }

                var ctx = new JobBatchContext5
                {
                    chunk = chunkRef.GetUnsafePtr(),
                    chunkVersions = chunkRef.GetVersionUnsafePtr(),
                    chunkCount = chunkRef._count,
                    entityOffset = arch.EntityIDsOffset,
                    sharedDataBlob = pSharedBlob,

                    offset0 = off0,
                    enableOff0 = enableOff0,
                    versionIndex0 = versionIdx0,
                    offset1 = off1,
                    enableOff1 = enableOff1,
                    versionIndex1 = versionIdx1,
                    offset2 = off2,
                    enableOff2 = enableOff2,
                    versionIndex2 = versionIdx2,
                    offset3 = off3,
                    enableOff3 = enableOff3,
                    versionIndex3 = versionIdx3,
                    offset4 = off4,
                    enableOff4 = enableOff4,
                    versionIndex4 = versionIdx4,
                    hiddenEnableCount = hiddenCount,
                };

                for (var h = 0; h < hiddenCount; h++)
                {
                    ctx.hiddenEnableOffsets[h] = hiddenOffsets[h];
                }
                batches.Add(ctx);
            }
        }

        var runner = new JobEntityBatch<TJob, T0, T1, T2, T3, T4>
        {
            userJob = jobData,
            batches = batches,
            mask = _mask,
            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;
        runner.componentIDs[1] = ComponentTypeID<T1>.Value;
        runner.componentIDs[2] = ComponentTypeID<T2>.Value;
        runner.componentIDs[3] = ComponentTypeID<T3>.Value;
        runner.componentIDs[4] = ComponentTypeID<T4>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var idx = 0; idx < 5; idx++)
            {
                if (id == runner.componentIDs[idx])
                {
                    runner.componentRW[idx] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, batches.Count, batchSize, dependency);

        var disposeJob = new DisposeJobEntity5
        {
            batches = batches,
        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity6 : IJob
    {
        public UnsafeList<JobBatchContext6> batches;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            batches.Dispose();
        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0, T1, T2, T3, T4, T5>(TJob jobData, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobEntity<T0, T1, T2, T3, T4, T5>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
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

        var batches = new UnsafeList<JobBatchContext6>(128, TempJobAllocator.AllocationHandle);
        var hiddenOffsets = stackalloc int[16];

        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            int off0;
            int enableOff0;
            int versionIdx0;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = -1;
                versionIdx0 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = layout.enableBitsOffset;
                versionIdx0 = layout.versionIndex;
            }
            int off1;
            int enableOff1;
            int versionIdx1;
            if (ComponentTypeID<T1>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
                off1 = layout.offset;
                enableOff1 = -1;
                versionIdx1 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
                off1 = layout.offset;
                enableOff1 = layout.enableBitsOffset;
                versionIdx1 = layout.versionIndex;
            }
            int off2;
            int enableOff2;
            int versionIdx2;
            if (ComponentTypeID<T2>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
                off2 = layout.offset;
                enableOff2 = -1;
                versionIdx2 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
                off2 = layout.offset;
                enableOff2 = layout.enableBitsOffset;
                versionIdx2 = layout.versionIndex;
            }
            int off3;
            int enableOff3;
            int versionIdx3;
            if (ComponentTypeID<T3>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T3>.Value).GetValueOrThrow();
                off3 = layout.offset;
                enableOff3 = -1;
                versionIdx3 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T3>.Value).GetValueOrThrow();
                off3 = layout.offset;
                enableOff3 = layout.enableBitsOffset;
                versionIdx3 = layout.versionIndex;
            }
            int off4;
            int enableOff4;
            int versionIdx4;
            if (ComponentTypeID<T4>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T4>.Value).GetValueOrThrow();
                off4 = layout.offset;
                enableOff4 = -1;
                versionIdx4 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T4>.Value).GetValueOrThrow();
                off4 = layout.offset;
                enableOff4 = layout.enableBitsOffset;
                versionIdx4 = layout.versionIndex;
            }
            int off5;
            int enableOff5;
            int versionIdx5;
            if (ComponentTypeID<T5>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T5>.Value).GetValueOrThrow();
                off5 = layout.offset;
                enableOff5 = -1;
                versionIdx5 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T5>.Value).GetValueOrThrow();
                off5 = layout.offset;
                enableOff5 = layout.enableBitsOffset;
                versionIdx5 = layout.versionIndex;
            }

            var hiddenCount = 0;
            var itE = _mask.requireEnabled.GetIterator();
            while (itE.Next(out var id) && hiddenCount < 16)
            {
                var found = false;
                if (id == ComponentTypeID<T0>.Value) found = true;
                if (id == ComponentTypeID<T1>.Value) found = true;
                if (id == ComponentTypeID<T2>.Value) found = true;
                if (id == ComponentTypeID<T3>.Value) found = true;
                if (id == ComponentTypeID<T4>.Value) found = true;
                if (id == ComponentTypeID<T5>.Value) found = true;
                if (!found)
                {
                    var layout = arch.GetLayout(id);
                    if (layout.Error == Error.None && layout.Value.enableBitsOffset != -1)
                    {
                        hiddenOffsets[hiddenCount++] = layout.Value.enableBitsOffset;
                    }
                }
            }

            for (var chunkIdx = 0; chunkIdx < arch.ChunkCount; chunkIdx++)
            {
                ref var chunkRef = ref arch.GetChunkReference(chunkIdx);

                byte* pSharedBlob = null;
                if (arch._chunkGroups.Count > 0 && chunkRef._groupIndex >= 0 && chunkRef._groupIndex < arch._chunkGroups.Count)
                {
                    var sharedSpan = arch._chunkGroups[chunkRef._groupIndex].sharedData;
                    if (sharedSpan.IsCreated)
                    {
                        pSharedBlob = (byte*)sharedSpan.GetUnsafePtr();
                    }
                }

                var ctx = new JobBatchContext6
                {
                    chunk = chunkRef.GetUnsafePtr(),
                    chunkVersions = chunkRef.GetVersionUnsafePtr(),
                    chunkCount = chunkRef._count,
                    entityOffset = arch.EntityIDsOffset,
                    sharedDataBlob = pSharedBlob,

                    offset0 = off0,
                    enableOff0 = enableOff0,
                    versionIndex0 = versionIdx0,
                    offset1 = off1,
                    enableOff1 = enableOff1,
                    versionIndex1 = versionIdx1,
                    offset2 = off2,
                    enableOff2 = enableOff2,
                    versionIndex2 = versionIdx2,
                    offset3 = off3,
                    enableOff3 = enableOff3,
                    versionIndex3 = versionIdx3,
                    offset4 = off4,
                    enableOff4 = enableOff4,
                    versionIndex4 = versionIdx4,
                    offset5 = off5,
                    enableOff5 = enableOff5,
                    versionIndex5 = versionIdx5,
                    hiddenEnableCount = hiddenCount,
                };

                for (var h = 0; h < hiddenCount; h++)
                {
                    ctx.hiddenEnableOffsets[h] = hiddenOffsets[h];
                }
                batches.Add(ctx);
            }
        }

        var runner = new JobEntityBatch<TJob, T0, T1, T2, T3, T4, T5>
        {
            userJob = jobData,
            batches = batches,
            mask = _mask,
            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;
        runner.componentIDs[1] = ComponentTypeID<T1>.Value;
        runner.componentIDs[2] = ComponentTypeID<T2>.Value;
        runner.componentIDs[3] = ComponentTypeID<T3>.Value;
        runner.componentIDs[4] = ComponentTypeID<T4>.Value;
        runner.componentIDs[5] = ComponentTypeID<T5>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var idx = 0; idx < 6; idx++)
            {
                if (id == runner.componentIDs[idx])
                {
                    runner.componentRW[idx] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, batches.Count, batchSize, dependency);

        var disposeJob = new DisposeJobEntity6
        {
            batches = batches,
        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity7 : IJob
    {
        public UnsafeList<JobBatchContext7> batches;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            batches.Dispose();
        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0, T1, T2, T3, T4, T5, T6>(TJob jobData, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobEntity<T0, T1, T2, T3, T4, T5, T6>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
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

        var batches = new UnsafeList<JobBatchContext7>(128, TempJobAllocator.AllocationHandle);
        var hiddenOffsets = stackalloc int[16];

        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            int off0;
            int enableOff0;
            int versionIdx0;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = -1;
                versionIdx0 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = layout.enableBitsOffset;
                versionIdx0 = layout.versionIndex;
            }
            int off1;
            int enableOff1;
            int versionIdx1;
            if (ComponentTypeID<T1>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
                off1 = layout.offset;
                enableOff1 = -1;
                versionIdx1 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
                off1 = layout.offset;
                enableOff1 = layout.enableBitsOffset;
                versionIdx1 = layout.versionIndex;
            }
            int off2;
            int enableOff2;
            int versionIdx2;
            if (ComponentTypeID<T2>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
                off2 = layout.offset;
                enableOff2 = -1;
                versionIdx2 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
                off2 = layout.offset;
                enableOff2 = layout.enableBitsOffset;
                versionIdx2 = layout.versionIndex;
            }
            int off3;
            int enableOff3;
            int versionIdx3;
            if (ComponentTypeID<T3>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T3>.Value).GetValueOrThrow();
                off3 = layout.offset;
                enableOff3 = -1;
                versionIdx3 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T3>.Value).GetValueOrThrow();
                off3 = layout.offset;
                enableOff3 = layout.enableBitsOffset;
                versionIdx3 = layout.versionIndex;
            }
            int off4;
            int enableOff4;
            int versionIdx4;
            if (ComponentTypeID<T4>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T4>.Value).GetValueOrThrow();
                off4 = layout.offset;
                enableOff4 = -1;
                versionIdx4 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T4>.Value).GetValueOrThrow();
                off4 = layout.offset;
                enableOff4 = layout.enableBitsOffset;
                versionIdx4 = layout.versionIndex;
            }
            int off5;
            int enableOff5;
            int versionIdx5;
            if (ComponentTypeID<T5>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T5>.Value).GetValueOrThrow();
                off5 = layout.offset;
                enableOff5 = -1;
                versionIdx5 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T5>.Value).GetValueOrThrow();
                off5 = layout.offset;
                enableOff5 = layout.enableBitsOffset;
                versionIdx5 = layout.versionIndex;
            }
            int off6;
            int enableOff6;
            int versionIdx6;
            if (ComponentTypeID<T6>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T6>.Value).GetValueOrThrow();
                off6 = layout.offset;
                enableOff6 = -1;
                versionIdx6 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T6>.Value).GetValueOrThrow();
                off6 = layout.offset;
                enableOff6 = layout.enableBitsOffset;
                versionIdx6 = layout.versionIndex;
            }

            var hiddenCount = 0;
            var itE = _mask.requireEnabled.GetIterator();
            while (itE.Next(out var id) && hiddenCount < 16)
            {
                var found = false;
                if (id == ComponentTypeID<T0>.Value) found = true;
                if (id == ComponentTypeID<T1>.Value) found = true;
                if (id == ComponentTypeID<T2>.Value) found = true;
                if (id == ComponentTypeID<T3>.Value) found = true;
                if (id == ComponentTypeID<T4>.Value) found = true;
                if (id == ComponentTypeID<T5>.Value) found = true;
                if (id == ComponentTypeID<T6>.Value) found = true;
                if (!found)
                {
                    var layout = arch.GetLayout(id);
                    if (layout.Error == Error.None && layout.Value.enableBitsOffset != -1)
                    {
                        hiddenOffsets[hiddenCount++] = layout.Value.enableBitsOffset;
                    }
                }
            }

            for (var chunkIdx = 0; chunkIdx < arch.ChunkCount; chunkIdx++)
            {
                ref var chunkRef = ref arch.GetChunkReference(chunkIdx);

                byte* pSharedBlob = null;
                if (arch._chunkGroups.Count > 0 && chunkRef._groupIndex >= 0 && chunkRef._groupIndex < arch._chunkGroups.Count)
                {
                    var sharedSpan = arch._chunkGroups[chunkRef._groupIndex].sharedData;
                    if (sharedSpan.IsCreated)
                    {
                        pSharedBlob = (byte*)sharedSpan.GetUnsafePtr();
                    }
                }

                var ctx = new JobBatchContext7
                {
                    chunk = chunkRef.GetUnsafePtr(),
                    chunkVersions = chunkRef.GetVersionUnsafePtr(),
                    chunkCount = chunkRef._count,
                    entityOffset = arch.EntityIDsOffset,
                    sharedDataBlob = pSharedBlob,

                    offset0 = off0,
                    enableOff0 = enableOff0,
                    versionIndex0 = versionIdx0,
                    offset1 = off1,
                    enableOff1 = enableOff1,
                    versionIndex1 = versionIdx1,
                    offset2 = off2,
                    enableOff2 = enableOff2,
                    versionIndex2 = versionIdx2,
                    offset3 = off3,
                    enableOff3 = enableOff3,
                    versionIndex3 = versionIdx3,
                    offset4 = off4,
                    enableOff4 = enableOff4,
                    versionIndex4 = versionIdx4,
                    offset5 = off5,
                    enableOff5 = enableOff5,
                    versionIndex5 = versionIdx5,
                    offset6 = off6,
                    enableOff6 = enableOff6,
                    versionIndex6 = versionIdx6,
                    hiddenEnableCount = hiddenCount,
                };

                for (var h = 0; h < hiddenCount; h++)
                {
                    ctx.hiddenEnableOffsets[h] = hiddenOffsets[h];
                }
                batches.Add(ctx);
            }
        }

        var runner = new JobEntityBatch<TJob, T0, T1, T2, T3, T4, T5, T6>
        {
            userJob = jobData,
            batches = batches,
            mask = _mask,
            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;
        runner.componentIDs[1] = ComponentTypeID<T1>.Value;
        runner.componentIDs[2] = ComponentTypeID<T2>.Value;
        runner.componentIDs[3] = ComponentTypeID<T3>.Value;
        runner.componentIDs[4] = ComponentTypeID<T4>.Value;
        runner.componentIDs[5] = ComponentTypeID<T5>.Value;
        runner.componentIDs[6] = ComponentTypeID<T6>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var idx = 0; idx < 7; idx++)
            {
                if (id == runner.componentIDs[idx])
                {
                    runner.componentRW[idx] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, batches.Count, batchSize, dependency);

        var disposeJob = new DisposeJobEntity7
        {
            batches = batches,
        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity8 : IJob
    {
        public UnsafeList<JobBatchContext8> batches;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            batches.Dispose();
        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0, T1, T2, T3, T4, T5, T6, T7>(TJob jobData, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobEntity<T0, T1, T2, T3, T4, T5, T6, T7>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
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

        var batches = new UnsafeList<JobBatchContext8>(128, TempJobAllocator.AllocationHandle);
        var hiddenOffsets = stackalloc int[16];

        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            int off0;
            int enableOff0;
            int versionIdx0;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = -1;
                versionIdx0 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
                off0 = layout.offset;
                enableOff0 = layout.enableBitsOffset;
                versionIdx0 = layout.versionIndex;
            }
            int off1;
            int enableOff1;
            int versionIdx1;
            if (ComponentTypeID<T1>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
                off1 = layout.offset;
                enableOff1 = -1;
                versionIdx1 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
                off1 = layout.offset;
                enableOff1 = layout.enableBitsOffset;
                versionIdx1 = layout.versionIndex;
            }
            int off2;
            int enableOff2;
            int versionIdx2;
            if (ComponentTypeID<T2>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
                off2 = layout.offset;
                enableOff2 = -1;
                versionIdx2 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
                off2 = layout.offset;
                enableOff2 = layout.enableBitsOffset;
                versionIdx2 = layout.versionIndex;
            }
            int off3;
            int enableOff3;
            int versionIdx3;
            if (ComponentTypeID<T3>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T3>.Value).GetValueOrThrow();
                off3 = layout.offset;
                enableOff3 = -1;
                versionIdx3 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T3>.Value).GetValueOrThrow();
                off3 = layout.offset;
                enableOff3 = layout.enableBitsOffset;
                versionIdx3 = layout.versionIndex;
            }
            int off4;
            int enableOff4;
            int versionIdx4;
            if (ComponentTypeID<T4>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T4>.Value).GetValueOrThrow();
                off4 = layout.offset;
                enableOff4 = -1;
                versionIdx4 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T4>.Value).GetValueOrThrow();
                off4 = layout.offset;
                enableOff4 = layout.enableBitsOffset;
                versionIdx4 = layout.versionIndex;
            }
            int off5;
            int enableOff5;
            int versionIdx5;
            if (ComponentTypeID<T5>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T5>.Value).GetValueOrThrow();
                off5 = layout.offset;
                enableOff5 = -1;
                versionIdx5 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T5>.Value).GetValueOrThrow();
                off5 = layout.offset;
                enableOff5 = layout.enableBitsOffset;
                versionIdx5 = layout.versionIndex;
            }
            int off6;
            int enableOff6;
            int versionIdx6;
            if (ComponentTypeID<T6>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T6>.Value).GetValueOrThrow();
                off6 = layout.offset;
                enableOff6 = -1;
                versionIdx6 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T6>.Value).GetValueOrThrow();
                off6 = layout.offset;
                enableOff6 = layout.enableBitsOffset;
                versionIdx6 = layout.versionIndex;
            }
            int off7;
            int enableOff7;
            int versionIdx7;
            if (ComponentTypeID<T7>.IsShared)
            {
                var layout = arch.GetSharedLayout(ComponentTypeID<T7>.Value).GetValueOrThrow();
                off7 = layout.offset;
                enableOff7 = -1;
                versionIdx7 = -1;
            }
            else
            {
                var layout = arch.GetLayout(ComponentTypeID<T7>.Value).GetValueOrThrow();
                off7 = layout.offset;
                enableOff7 = layout.enableBitsOffset;
                versionIdx7 = layout.versionIndex;
            }

            var hiddenCount = 0;
            var itE = _mask.requireEnabled.GetIterator();
            while (itE.Next(out var id) && hiddenCount < 16)
            {
                var found = false;
                if (id == ComponentTypeID<T0>.Value) found = true;
                if (id == ComponentTypeID<T1>.Value) found = true;
                if (id == ComponentTypeID<T2>.Value) found = true;
                if (id == ComponentTypeID<T3>.Value) found = true;
                if (id == ComponentTypeID<T4>.Value) found = true;
                if (id == ComponentTypeID<T5>.Value) found = true;
                if (id == ComponentTypeID<T6>.Value) found = true;
                if (id == ComponentTypeID<T7>.Value) found = true;
                if (!found)
                {
                    var layout = arch.GetLayout(id);
                    if (layout.Error == Error.None && layout.Value.enableBitsOffset != -1)
                    {
                        hiddenOffsets[hiddenCount++] = layout.Value.enableBitsOffset;
                    }
                }
            }

            for (var chunkIdx = 0; chunkIdx < arch.ChunkCount; chunkIdx++)
            {
                ref var chunkRef = ref arch.GetChunkReference(chunkIdx);

                byte* pSharedBlob = null;
                if (arch._chunkGroups.Count > 0 && chunkRef._groupIndex >= 0 && chunkRef._groupIndex < arch._chunkGroups.Count)
                {
                    var sharedSpan = arch._chunkGroups[chunkRef._groupIndex].sharedData;
                    if (sharedSpan.IsCreated)
                    {
                        pSharedBlob = (byte*)sharedSpan.GetUnsafePtr();
                    }
                }

                var ctx = new JobBatchContext8
                {
                    chunk = chunkRef.GetUnsafePtr(),
                    chunkVersions = chunkRef.GetVersionUnsafePtr(),
                    chunkCount = chunkRef._count,
                    entityOffset = arch.EntityIDsOffset,
                    sharedDataBlob = pSharedBlob,

                    offset0 = off0,
                    enableOff0 = enableOff0,
                    versionIndex0 = versionIdx0,
                    offset1 = off1,
                    enableOff1 = enableOff1,
                    versionIndex1 = versionIdx1,
                    offset2 = off2,
                    enableOff2 = enableOff2,
                    versionIndex2 = versionIdx2,
                    offset3 = off3,
                    enableOff3 = enableOff3,
                    versionIndex3 = versionIdx3,
                    offset4 = off4,
                    enableOff4 = enableOff4,
                    versionIndex4 = versionIdx4,
                    offset5 = off5,
                    enableOff5 = enableOff5,
                    versionIndex5 = versionIdx5,
                    offset6 = off6,
                    enableOff6 = enableOff6,
                    versionIndex6 = versionIdx6,
                    offset7 = off7,
                    enableOff7 = enableOff7,
                    versionIndex7 = versionIdx7,
                    hiddenEnableCount = hiddenCount,
                };

                for (var h = 0; h < hiddenCount; h++)
                {
                    ctx.hiddenEnableOffsets[h] = hiddenOffsets[h];
                }
                batches.Add(ctx);
            }
        }

        var runner = new JobEntityBatch<TJob, T0, T1, T2, T3, T4, T5, T6, T7>
        {
            userJob = jobData,
            batches = batches,
            mask = _mask,
            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;
        runner.componentIDs[1] = ComponentTypeID<T1>.Value;
        runner.componentIDs[2] = ComponentTypeID<T2>.Value;
        runner.componentIDs[3] = ComponentTypeID<T3>.Value;
        runner.componentIDs[4] = ComponentTypeID<T4>.Value;
        runner.componentIDs[5] = ComponentTypeID<T5>.Value;
        runner.componentIDs[6] = ComponentTypeID<T6>.Value;
        runner.componentIDs[7] = ComponentTypeID<T7>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var idx = 0; idx < 8; idx++)
            {
                if (id == runner.componentIDs[idx])
                {
                    runner.componentRW[idx] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, batches.Count, batchSize, dependency);

        var disposeJob = new DisposeJobEntity8
        {
            batches = batches,
        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

}
