using Ghost.Core;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Entities;

public interface IJobEntity<T0>
    where T0 : unmanaged, IComponent
{
    void Execute(Entity entity, ref T0 component0, ref readonly JobExecutionContext ctx);
}

internal unsafe struct JobEntityBatch<TJob, T0> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0>
    where T0 : unmanaged, IComponent
{
    public fixed int componentIDs[1];
    public fixed bool componentRW[1];

    public TJob userJob;

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<IntPtr> chunkVersions;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;
    public UnsafeList<int> versionIndices0;

    public int version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var pVersions = (int*)chunkVersions[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];
        var versionIndex0 = versionIndices0[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);

        // 2. Update versions for RW components
        if (componentRW[0])
        {
            pVersions[versionIndex0] = version;
        }

        // 3. Iterate all entities in this chunk
        for (var i = 0; i < count; i++)
        {
            if (enableOff0 != -1 && !EntityQuery.CheckBit(pChunk + enableOff0, i))
            {
                continue;
            }

            userJob.Execute(pEntity[i], ref ptr0[i], in ctx);
        }
    }
}

public interface IJobEntity<T0, T1>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
{
    void Execute(Entity entity, ref T0 component0, ref T1 component1, ref readonly JobExecutionContext ctx);
}

internal unsafe struct JobEntityBatch<TJob, T0, T1> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0, T1>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
{
    public fixed int componentIDs[2];
    public fixed bool componentRW[2];

    public TJob userJob;

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<IntPtr> chunkVersions;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;
    public UnsafeList<int> versionIndices0;

    public UnsafeList<int> offsets1;
    public UnsafeList<int> bitsOffsets1;
    public UnsafeList<int> versionIndices1;

    public int version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var pVersions = (int*)chunkVersions[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];
        var versionIndex0 = versionIndices0[loopIndex];

        var off1 = offsets1[loopIndex];
        var enableOff1 = bitsOffsets1[loopIndex];
        var versionIndex1 = versionIndices1[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);
        var ptr1 = (T1*)(pChunk + off1);

        // 2. Update versions for RW components
        if (componentRW[0])
        {
            pVersions[versionIndex0] = version;
        }

        if (componentRW[1])
        {
            pVersions[versionIndex1] = version;
        }

        // 3. Iterate all entities in this chunk
        for (var i = 0; i < count; i++)
        {
            if (enableOff0 != -1 && !EntityQuery.CheckBit(pChunk + enableOff0, i))
            {
                continue;
            }

            if (enableOff1 != -1 && !EntityQuery.CheckBit(pChunk + enableOff1, i))
            {
                continue;
            }

            userJob.Execute(pEntity[i], ref ptr0[i], ref ptr1[i], in ctx);
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

internal unsafe struct JobEntityBatch<TJob, T0, T1, T2> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0, T1, T2>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
{
    public fixed int componentIDs[3];
    public fixed bool componentRW[3];

    public TJob userJob;

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<IntPtr> chunkVersions;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;
    public UnsafeList<int> versionIndices0;

    public UnsafeList<int> offsets1;
    public UnsafeList<int> bitsOffsets1;
    public UnsafeList<int> versionIndices1;

    public UnsafeList<int> offsets2;
    public UnsafeList<int> bitsOffsets2;
    public UnsafeList<int> versionIndices2;

    public int version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var pVersions = (int*)chunkVersions[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];
        var versionIndex0 = versionIndices0[loopIndex];

        var off1 = offsets1[loopIndex];
        var enableOff1 = bitsOffsets1[loopIndex];
        var versionIndex1 = versionIndices1[loopIndex];

        var off2 = offsets2[loopIndex];
        var enableOff2 = bitsOffsets2[loopIndex];
        var versionIndex2 = versionIndices2[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);
        var ptr1 = (T1*)(pChunk + off1);
        var ptr2 = (T2*)(pChunk + off2);

        // 2. Update versions for RW components
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

        // 3. Iterate all entities in this chunk
        for (var i = 0; i < count; i++)
        {
            if (enableOff0 != -1 && !EntityQuery.CheckBit(pChunk + enableOff0, i))
            {
                continue;
            }

            if (enableOff1 != -1 && !EntityQuery.CheckBit(pChunk + enableOff1, i))
            {
                continue;
            }

            if (enableOff2 != -1 && !EntityQuery.CheckBit(pChunk + enableOff2, i))
            {
                continue;
            }

            userJob.Execute(pEntity[i], ref ptr0[i], ref ptr1[i], ref ptr2[i], in ctx);
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

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<IntPtr> chunkVersions;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;
    public UnsafeList<int> versionIndices0;

    public UnsafeList<int> offsets1;
    public UnsafeList<int> bitsOffsets1;
    public UnsafeList<int> versionIndices1;

    public UnsafeList<int> offsets2;
    public UnsafeList<int> bitsOffsets2;
    public UnsafeList<int> versionIndices2;

    public UnsafeList<int> offsets3;
    public UnsafeList<int> bitsOffsets3;
    public UnsafeList<int> versionIndices3;

    public int version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var pVersions = (int*)chunkVersions[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];
        var versionIndex0 = versionIndices0[loopIndex];

        var off1 = offsets1[loopIndex];
        var enableOff1 = bitsOffsets1[loopIndex];
        var versionIndex1 = versionIndices1[loopIndex];

        var off2 = offsets2[loopIndex];
        var enableOff2 = bitsOffsets2[loopIndex];
        var versionIndex2 = versionIndices2[loopIndex];

        var off3 = offsets3[loopIndex];
        var enableOff3 = bitsOffsets3[loopIndex];
        var versionIndex3 = versionIndices3[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);
        var ptr1 = (T1*)(pChunk + off1);
        var ptr2 = (T2*)(pChunk + off2);
        var ptr3 = (T3*)(pChunk + off3);

        // 2. Update versions for RW components
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

        // 3. Iterate all entities in this chunk
        for (var i = 0; i < count; i++)
        {
            if (enableOff0 != -1 && !EntityQuery.CheckBit(pChunk + enableOff0, i))
            {
                continue;
            }

            if (enableOff1 != -1 && !EntityQuery.CheckBit(pChunk + enableOff1, i))
            {
                continue;
            }

            if (enableOff2 != -1 && !EntityQuery.CheckBit(pChunk + enableOff2, i))
            {
                continue;
            }

            if (enableOff3 != -1 && !EntityQuery.CheckBit(pChunk + enableOff3, i))
            {
                continue;
            }

            userJob.Execute(pEntity[i], ref ptr0[i], ref ptr1[i], ref ptr2[i], ref ptr3[i], in ctx);
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

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<IntPtr> chunkVersions;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;
    public UnsafeList<int> versionIndices0;

    public UnsafeList<int> offsets1;
    public UnsafeList<int> bitsOffsets1;
    public UnsafeList<int> versionIndices1;

    public UnsafeList<int> offsets2;
    public UnsafeList<int> bitsOffsets2;
    public UnsafeList<int> versionIndices2;

    public UnsafeList<int> offsets3;
    public UnsafeList<int> bitsOffsets3;
    public UnsafeList<int> versionIndices3;

    public UnsafeList<int> offsets4;
    public UnsafeList<int> bitsOffsets4;
    public UnsafeList<int> versionIndices4;

    public int version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var pVersions = (int*)chunkVersions[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];
        var versionIndex0 = versionIndices0[loopIndex];

        var off1 = offsets1[loopIndex];
        var enableOff1 = bitsOffsets1[loopIndex];
        var versionIndex1 = versionIndices1[loopIndex];

        var off2 = offsets2[loopIndex];
        var enableOff2 = bitsOffsets2[loopIndex];
        var versionIndex2 = versionIndices2[loopIndex];

        var off3 = offsets3[loopIndex];
        var enableOff3 = bitsOffsets3[loopIndex];
        var versionIndex3 = versionIndices3[loopIndex];

        var off4 = offsets4[loopIndex];
        var enableOff4 = bitsOffsets4[loopIndex];
        var versionIndex4 = versionIndices4[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);
        var ptr1 = (T1*)(pChunk + off1);
        var ptr2 = (T2*)(pChunk + off2);
        var ptr3 = (T3*)(pChunk + off3);
        var ptr4 = (T4*)(pChunk + off4);

        // 2. Update versions for RW components
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

        // 3. Iterate all entities in this chunk
        for (var i = 0; i < count; i++)
        {
            if (enableOff0 != -1 && !EntityQuery.CheckBit(pChunk + enableOff0, i))
            {
                continue;
            }

            if (enableOff1 != -1 && !EntityQuery.CheckBit(pChunk + enableOff1, i))
            {
                continue;
            }

            if (enableOff2 != -1 && !EntityQuery.CheckBit(pChunk + enableOff2, i))
            {
                continue;
            }

            if (enableOff3 != -1 && !EntityQuery.CheckBit(pChunk + enableOff3, i))
            {
                continue;
            }

            if (enableOff4 != -1 && !EntityQuery.CheckBit(pChunk + enableOff4, i))
            {
                continue;
            }

            userJob.Execute(pEntity[i], ref ptr0[i], ref ptr1[i], ref ptr2[i], ref ptr3[i], ref ptr4[i], in ctx);
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

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<IntPtr> chunkVersions;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;
    public UnsafeList<int> versionIndices0;

    public UnsafeList<int> offsets1;
    public UnsafeList<int> bitsOffsets1;
    public UnsafeList<int> versionIndices1;

    public UnsafeList<int> offsets2;
    public UnsafeList<int> bitsOffsets2;
    public UnsafeList<int> versionIndices2;

    public UnsafeList<int> offsets3;
    public UnsafeList<int> bitsOffsets3;
    public UnsafeList<int> versionIndices3;

    public UnsafeList<int> offsets4;
    public UnsafeList<int> bitsOffsets4;
    public UnsafeList<int> versionIndices4;

    public UnsafeList<int> offsets5;
    public UnsafeList<int> bitsOffsets5;
    public UnsafeList<int> versionIndices5;

    public int version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var pVersions = (int*)chunkVersions[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];
        var versionIndex0 = versionIndices0[loopIndex];

        var off1 = offsets1[loopIndex];
        var enableOff1 = bitsOffsets1[loopIndex];
        var versionIndex1 = versionIndices1[loopIndex];

        var off2 = offsets2[loopIndex];
        var enableOff2 = bitsOffsets2[loopIndex];
        var versionIndex2 = versionIndices2[loopIndex];

        var off3 = offsets3[loopIndex];
        var enableOff3 = bitsOffsets3[loopIndex];
        var versionIndex3 = versionIndices3[loopIndex];

        var off4 = offsets4[loopIndex];
        var enableOff4 = bitsOffsets4[loopIndex];
        var versionIndex4 = versionIndices4[loopIndex];

        var off5 = offsets5[loopIndex];
        var enableOff5 = bitsOffsets5[loopIndex];
        var versionIndex5 = versionIndices5[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);
        var ptr1 = (T1*)(pChunk + off1);
        var ptr2 = (T2*)(pChunk + off2);
        var ptr3 = (T3*)(pChunk + off3);
        var ptr4 = (T4*)(pChunk + off4);
        var ptr5 = (T5*)(pChunk + off5);

        // 2. Update versions for RW components
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

        // 3. Iterate all entities in this chunk
        for (var i = 0; i < count; i++)
        {
            if (enableOff0 != -1 && !EntityQuery.CheckBit(pChunk + enableOff0, i))
            {
                continue;
            }

            if (enableOff1 != -1 && !EntityQuery.CheckBit(pChunk + enableOff1, i))
            {
                continue;
            }

            if (enableOff2 != -1 && !EntityQuery.CheckBit(pChunk + enableOff2, i))
            {
                continue;
            }

            if (enableOff3 != -1 && !EntityQuery.CheckBit(pChunk + enableOff3, i))
            {
                continue;
            }

            if (enableOff4 != -1 && !EntityQuery.CheckBit(pChunk + enableOff4, i))
            {
                continue;
            }

            if (enableOff5 != -1 && !EntityQuery.CheckBit(pChunk + enableOff5, i))
            {
                continue;
            }

            userJob.Execute(pEntity[i], ref ptr0[i], ref ptr1[i], ref ptr2[i], ref ptr3[i], ref ptr4[i], ref ptr5[i], in ctx);
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

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<IntPtr> chunkVersions;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;
    public UnsafeList<int> versionIndices0;

    public UnsafeList<int> offsets1;
    public UnsafeList<int> bitsOffsets1;
    public UnsafeList<int> versionIndices1;

    public UnsafeList<int> offsets2;
    public UnsafeList<int> bitsOffsets2;
    public UnsafeList<int> versionIndices2;

    public UnsafeList<int> offsets3;
    public UnsafeList<int> bitsOffsets3;
    public UnsafeList<int> versionIndices3;

    public UnsafeList<int> offsets4;
    public UnsafeList<int> bitsOffsets4;
    public UnsafeList<int> versionIndices4;

    public UnsafeList<int> offsets5;
    public UnsafeList<int> bitsOffsets5;
    public UnsafeList<int> versionIndices5;

    public UnsafeList<int> offsets6;
    public UnsafeList<int> bitsOffsets6;
    public UnsafeList<int> versionIndices6;

    public int version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var pVersions = (int*)chunkVersions[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];
        var versionIndex0 = versionIndices0[loopIndex];

        var off1 = offsets1[loopIndex];
        var enableOff1 = bitsOffsets1[loopIndex];
        var versionIndex1 = versionIndices1[loopIndex];

        var off2 = offsets2[loopIndex];
        var enableOff2 = bitsOffsets2[loopIndex];
        var versionIndex2 = versionIndices2[loopIndex];

        var off3 = offsets3[loopIndex];
        var enableOff3 = bitsOffsets3[loopIndex];
        var versionIndex3 = versionIndices3[loopIndex];

        var off4 = offsets4[loopIndex];
        var enableOff4 = bitsOffsets4[loopIndex];
        var versionIndex4 = versionIndices4[loopIndex];

        var off5 = offsets5[loopIndex];
        var enableOff5 = bitsOffsets5[loopIndex];
        var versionIndex5 = versionIndices5[loopIndex];

        var off6 = offsets6[loopIndex];
        var enableOff6 = bitsOffsets6[loopIndex];
        var versionIndex6 = versionIndices6[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);
        var ptr1 = (T1*)(pChunk + off1);
        var ptr2 = (T2*)(pChunk + off2);
        var ptr3 = (T3*)(pChunk + off3);
        var ptr4 = (T4*)(pChunk + off4);
        var ptr5 = (T5*)(pChunk + off5);
        var ptr6 = (T6*)(pChunk + off6);

        // 2. Update versions for RW components
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

        // 3. Iterate all entities in this chunk
        for (var i = 0; i < count; i++)
        {
            if (enableOff0 != -1 && !EntityQuery.CheckBit(pChunk + enableOff0, i))
            {
                continue;
            }

            if (enableOff1 != -1 && !EntityQuery.CheckBit(pChunk + enableOff1, i))
            {
                continue;
            }

            if (enableOff2 != -1 && !EntityQuery.CheckBit(pChunk + enableOff2, i))
            {
                continue;
            }

            if (enableOff3 != -1 && !EntityQuery.CheckBit(pChunk + enableOff3, i))
            {
                continue;
            }

            if (enableOff4 != -1 && !EntityQuery.CheckBit(pChunk + enableOff4, i))
            {
                continue;
            }

            if (enableOff5 != -1 && !EntityQuery.CheckBit(pChunk + enableOff5, i))
            {
                continue;
            }

            if (enableOff6 != -1 && !EntityQuery.CheckBit(pChunk + enableOff6, i))
            {
                continue;
            }

            userJob.Execute(pEntity[i], ref ptr0[i], ref ptr1[i], ref ptr2[i], ref ptr3[i], ref ptr4[i], ref ptr5[i], ref ptr6[i], in ctx);
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

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<IntPtr> chunkVersions;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;
    public UnsafeList<int> versionIndices0;

    public UnsafeList<int> offsets1;
    public UnsafeList<int> bitsOffsets1;
    public UnsafeList<int> versionIndices1;

    public UnsafeList<int> offsets2;
    public UnsafeList<int> bitsOffsets2;
    public UnsafeList<int> versionIndices2;

    public UnsafeList<int> offsets3;
    public UnsafeList<int> bitsOffsets3;
    public UnsafeList<int> versionIndices3;

    public UnsafeList<int> offsets4;
    public UnsafeList<int> bitsOffsets4;
    public UnsafeList<int> versionIndices4;

    public UnsafeList<int> offsets5;
    public UnsafeList<int> bitsOffsets5;
    public UnsafeList<int> versionIndices5;

    public UnsafeList<int> offsets6;
    public UnsafeList<int> bitsOffsets6;
    public UnsafeList<int> versionIndices6;

    public UnsafeList<int> offsets7;
    public UnsafeList<int> bitsOffsets7;
    public UnsafeList<int> versionIndices7;

    public int version;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var pVersions = (int*)chunkVersions[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];
        var versionIndex0 = versionIndices0[loopIndex];

        var off1 = offsets1[loopIndex];
        var enableOff1 = bitsOffsets1[loopIndex];
        var versionIndex1 = versionIndices1[loopIndex];

        var off2 = offsets2[loopIndex];
        var enableOff2 = bitsOffsets2[loopIndex];
        var versionIndex2 = versionIndices2[loopIndex];

        var off3 = offsets3[loopIndex];
        var enableOff3 = bitsOffsets3[loopIndex];
        var versionIndex3 = versionIndices3[loopIndex];

        var off4 = offsets4[loopIndex];
        var enableOff4 = bitsOffsets4[loopIndex];
        var versionIndex4 = versionIndices4[loopIndex];

        var off5 = offsets5[loopIndex];
        var enableOff5 = bitsOffsets5[loopIndex];
        var versionIndex5 = versionIndices5[loopIndex];

        var off6 = offsets6[loopIndex];
        var enableOff6 = bitsOffsets6[loopIndex];
        var versionIndex6 = versionIndices6[loopIndex];

        var off7 = offsets7[loopIndex];
        var enableOff7 = bitsOffsets7[loopIndex];
        var versionIndex7 = versionIndices7[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);
        var ptr1 = (T1*)(pChunk + off1);
        var ptr2 = (T2*)(pChunk + off2);
        var ptr3 = (T3*)(pChunk + off3);
        var ptr4 = (T4*)(pChunk + off4);
        var ptr5 = (T5*)(pChunk + off5);
        var ptr6 = (T6*)(pChunk + off6);
        var ptr7 = (T7*)(pChunk + off7);

        // 2. Update versions for RW components
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

        // 3. Iterate all entities in this chunk
        for (var i = 0; i < count; i++)
        {
            if (enableOff0 != -1 && !EntityQuery.CheckBit(pChunk + enableOff0, i))
            {
                continue;
            }

            if (enableOff1 != -1 && !EntityQuery.CheckBit(pChunk + enableOff1, i))
            {
                continue;
            }

            if (enableOff2 != -1 && !EntityQuery.CheckBit(pChunk + enableOff2, i))
            {
                continue;
            }

            if (enableOff3 != -1 && !EntityQuery.CheckBit(pChunk + enableOff3, i))
            {
                continue;
            }

            if (enableOff4 != -1 && !EntityQuery.CheckBit(pChunk + enableOff4, i))
            {
                continue;
            }

            if (enableOff5 != -1 && !EntityQuery.CheckBit(pChunk + enableOff5, i))
            {
                continue;
            }

            if (enableOff6 != -1 && !EntityQuery.CheckBit(pChunk + enableOff6, i))
            {
                continue;
            }

            if (enableOff7 != -1 && !EntityQuery.CheckBit(pChunk + enableOff7, i))
            {
                continue;
            }

            userJob.Execute(pEntity[i], ref ptr0[i], ref ptr1[i], ref ptr2[i], ref ptr3[i], ref ptr4[i], ref ptr5[i], ref ptr6[i], ref ptr7[i], in ctx);
        }
    }
}

public unsafe partial struct EntityQuery
{
    private struct DisposeJobEntity1 : IJob
    {
        public UnsafeList<IntPtr> chunks;
        public UnsafeList<IntPtr> chunkVersions;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;
        public UnsafeList<int> versionIndices0;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            chunks.Dispose();
            chunkVersions.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();
            versionIndices0.Dispose();

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

        // 1. Flatten the World
        var chunks = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkVersions = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkEntityCounts = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var entityOffsets = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunks.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkVersions.Add((IntPtr)chunkRef.GetVersionUnsafePtr());
                chunkEntityCounts.Add(chunkRef._count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);
                versionIndices0.Add(layout0.versionIndex);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0>
        {
            userJob = jobData,
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i = 0; i < 1; i++)
            {
                if (id == runner.componentIDs[i])
                {
                    runner.componentRW[i] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, chunks.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity1
        {
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity2 : IJob
    {
        public UnsafeList<IntPtr> chunks;
        public UnsafeList<IntPtr> chunkVersions;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;
        public UnsafeList<int> versionIndices0;

        public UnsafeList<int> offsets1;
        public UnsafeList<int> bitsOffsets1;
        public UnsafeList<int> versionIndices1;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            chunks.Dispose();
            chunkVersions.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();
            versionIndices0.Dispose();

            offsets1.Dispose();
            bitsOffsets1.Dispose();
            versionIndices1.Dispose();

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

        // 1. Flatten the World
        var chunks = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkVersions = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkEntityCounts = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var entityOffsets = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
            var layout1 = arch.GetLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunks.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkVersions.Add((IntPtr)chunkRef.GetVersionUnsafePtr());
                chunkEntityCounts.Add(chunkRef._count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);
                versionIndices0.Add(layout0.versionIndex);

                offsets1.Add(layout1.offset);
                bitsOffsets1.Add(layout1.enableBitsOffset);
                versionIndices1.Add(layout1.versionIndex);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0, T1>
        {
            userJob = jobData,
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,
            versionIndices1 = versionIndices1,

            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i = 0; i < 1; i++)
            {
                if (id == runner.componentIDs[i])
                {
                    runner.componentRW[i] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, chunks.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity2
        {
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,
            versionIndices1 = versionIndices1,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity3 : IJob
    {
        public UnsafeList<IntPtr> chunks;
        public UnsafeList<IntPtr> chunkVersions;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;
        public UnsafeList<int> versionIndices0;

        public UnsafeList<int> offsets1;
        public UnsafeList<int> bitsOffsets1;
        public UnsafeList<int> versionIndices1;

        public UnsafeList<int> offsets2;
        public UnsafeList<int> bitsOffsets2;
        public UnsafeList<int> versionIndices2;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            chunks.Dispose();
            chunkVersions.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();
            versionIndices0.Dispose();

            offsets1.Dispose();
            bitsOffsets1.Dispose();
            versionIndices1.Dispose();

            offsets2.Dispose();
            bitsOffsets2.Dispose();
            versionIndices2.Dispose();

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

        // 1. Flatten the World
        var chunks = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkVersions = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkEntityCounts = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var entityOffsets = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
            var layout1 = arch.GetLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
            var layout2 = arch.GetLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunks.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkVersions.Add((IntPtr)chunkRef.GetVersionUnsafePtr());
                chunkEntityCounts.Add(chunkRef._count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);
                versionIndices0.Add(layout0.versionIndex);

                offsets1.Add(layout1.offset);
                bitsOffsets1.Add(layout1.enableBitsOffset);
                versionIndices1.Add(layout1.versionIndex);

                offsets2.Add(layout2.offset);
                bitsOffsets2.Add(layout2.enableBitsOffset);
                versionIndices2.Add(layout2.versionIndex);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0, T1, T2>
        {
            userJob = jobData,
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,
            versionIndices1 = versionIndices1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,
            versionIndices2 = versionIndices2,

            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i = 0; i < 1; i++)
            {
                if (id == runner.componentIDs[i])
                {
                    runner.componentRW[i] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, chunks.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity3
        {
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,
            versionIndices1 = versionIndices1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,
            versionIndices2 = versionIndices2,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity4 : IJob
    {
        public UnsafeList<IntPtr> chunks;
        public UnsafeList<IntPtr> chunkVersions;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;
        public UnsafeList<int> versionIndices0;

        public UnsafeList<int> offsets1;
        public UnsafeList<int> bitsOffsets1;
        public UnsafeList<int> versionIndices1;

        public UnsafeList<int> offsets2;
        public UnsafeList<int> bitsOffsets2;
        public UnsafeList<int> versionIndices2;

        public UnsafeList<int> offsets3;
        public UnsafeList<int> bitsOffsets3;
        public UnsafeList<int> versionIndices3;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            chunks.Dispose();
            chunkVersions.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();
            versionIndices0.Dispose();

            offsets1.Dispose();
            bitsOffsets1.Dispose();
            versionIndices1.Dispose();

            offsets2.Dispose();
            bitsOffsets2.Dispose();
            versionIndices2.Dispose();

            offsets3.Dispose();
            bitsOffsets3.Dispose();
            versionIndices3.Dispose();

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

        // 1. Flatten the World
        var chunks = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkVersions = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkEntityCounts = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var entityOffsets = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets3 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets3 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices3 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
            var layout1 = arch.GetLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
            var layout2 = arch.GetLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
            var layout3 = arch.GetLayout(ComponentTypeID<T3>.Value).GetValueOrThrow();

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunks.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkVersions.Add((IntPtr)chunkRef.GetVersionUnsafePtr());
                chunkEntityCounts.Add(chunkRef._count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);
                versionIndices0.Add(layout0.versionIndex);

                offsets1.Add(layout1.offset);
                bitsOffsets1.Add(layout1.enableBitsOffset);
                versionIndices1.Add(layout1.versionIndex);

                offsets2.Add(layout2.offset);
                bitsOffsets2.Add(layout2.enableBitsOffset);
                versionIndices2.Add(layout2.versionIndex);

                offsets3.Add(layout3.offset);
                bitsOffsets3.Add(layout3.enableBitsOffset);
                versionIndices3.Add(layout3.versionIndex);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0, T1, T2, T3>
        {
            userJob = jobData,
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,
            versionIndices1 = versionIndices1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,
            versionIndices2 = versionIndices2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,
            versionIndices3 = versionIndices3,

            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i = 0; i < 1; i++)
            {
                if (id == runner.componentIDs[i])
                {
                    runner.componentRW[i] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, chunks.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity4
        {
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,
            versionIndices1 = versionIndices1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,
            versionIndices2 = versionIndices2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,
            versionIndices3 = versionIndices3,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity5 : IJob
    {
        public UnsafeList<IntPtr> chunks;
        public UnsafeList<IntPtr> chunkVersions;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;
        public UnsafeList<int> versionIndices0;

        public UnsafeList<int> offsets1;
        public UnsafeList<int> bitsOffsets1;
        public UnsafeList<int> versionIndices1;

        public UnsafeList<int> offsets2;
        public UnsafeList<int> bitsOffsets2;
        public UnsafeList<int> versionIndices2;

        public UnsafeList<int> offsets3;
        public UnsafeList<int> bitsOffsets3;
        public UnsafeList<int> versionIndices3;

        public UnsafeList<int> offsets4;
        public UnsafeList<int> bitsOffsets4;
        public UnsafeList<int> versionIndices4;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            chunks.Dispose();
            chunkVersions.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();
            versionIndices0.Dispose();

            offsets1.Dispose();
            bitsOffsets1.Dispose();
            versionIndices1.Dispose();

            offsets2.Dispose();
            bitsOffsets2.Dispose();
            versionIndices2.Dispose();

            offsets3.Dispose();
            bitsOffsets3.Dispose();
            versionIndices3.Dispose();

            offsets4.Dispose();
            bitsOffsets4.Dispose();
            versionIndices4.Dispose();

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

        // 1. Flatten the World
        var chunks = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkVersions = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkEntityCounts = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var entityOffsets = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets3 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets3 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices3 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets4 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets4 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices4 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
            var layout1 = arch.GetLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
            var layout2 = arch.GetLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
            var layout3 = arch.GetLayout(ComponentTypeID<T3>.Value).GetValueOrThrow();
            var layout4 = arch.GetLayout(ComponentTypeID<T4>.Value).GetValueOrThrow();

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunks.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkVersions.Add((IntPtr)chunkRef.GetVersionUnsafePtr());
                chunkEntityCounts.Add(chunkRef._count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);
                versionIndices0.Add(layout0.versionIndex);

                offsets1.Add(layout1.offset);
                bitsOffsets1.Add(layout1.enableBitsOffset);
                versionIndices1.Add(layout1.versionIndex);

                offsets2.Add(layout2.offset);
                bitsOffsets2.Add(layout2.enableBitsOffset);
                versionIndices2.Add(layout2.versionIndex);

                offsets3.Add(layout3.offset);
                bitsOffsets3.Add(layout3.enableBitsOffset);
                versionIndices3.Add(layout3.versionIndex);

                offsets4.Add(layout4.offset);
                bitsOffsets4.Add(layout4.enableBitsOffset);
                versionIndices4.Add(layout4.versionIndex);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0, T1, T2, T3, T4>
        {
            userJob = jobData,
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,
            versionIndices1 = versionIndices1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,
            versionIndices2 = versionIndices2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,
            versionIndices3 = versionIndices3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,
            versionIndices4 = versionIndices4,

            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i = 0; i < 1; i++)
            {
                if (id == runner.componentIDs[i])
                {
                    runner.componentRW[i] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, chunks.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity5
        {
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,
            versionIndices1 = versionIndices1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,
            versionIndices2 = versionIndices2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,
            versionIndices3 = versionIndices3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,
            versionIndices4 = versionIndices4,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity6 : IJob
    {
        public UnsafeList<IntPtr> chunks;
        public UnsafeList<IntPtr> chunkVersions;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;
        public UnsafeList<int> versionIndices0;

        public UnsafeList<int> offsets1;
        public UnsafeList<int> bitsOffsets1;
        public UnsafeList<int> versionIndices1;

        public UnsafeList<int> offsets2;
        public UnsafeList<int> bitsOffsets2;
        public UnsafeList<int> versionIndices2;

        public UnsafeList<int> offsets3;
        public UnsafeList<int> bitsOffsets3;
        public UnsafeList<int> versionIndices3;

        public UnsafeList<int> offsets4;
        public UnsafeList<int> bitsOffsets4;
        public UnsafeList<int> versionIndices4;

        public UnsafeList<int> offsets5;
        public UnsafeList<int> bitsOffsets5;
        public UnsafeList<int> versionIndices5;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            chunks.Dispose();
            chunkVersions.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();
            versionIndices0.Dispose();

            offsets1.Dispose();
            bitsOffsets1.Dispose();
            versionIndices1.Dispose();

            offsets2.Dispose();
            bitsOffsets2.Dispose();
            versionIndices2.Dispose();

            offsets3.Dispose();
            bitsOffsets3.Dispose();
            versionIndices3.Dispose();

            offsets4.Dispose();
            bitsOffsets4.Dispose();
            versionIndices4.Dispose();

            offsets5.Dispose();
            bitsOffsets5.Dispose();
            versionIndices5.Dispose();

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

        // 1. Flatten the World
        var chunks = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkVersions = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkEntityCounts = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var entityOffsets = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets3 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets3 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices3 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets4 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets4 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices4 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets5 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets5 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices5 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
            var layout1 = arch.GetLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
            var layout2 = arch.GetLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
            var layout3 = arch.GetLayout(ComponentTypeID<T3>.Value).GetValueOrThrow();
            var layout4 = arch.GetLayout(ComponentTypeID<T4>.Value).GetValueOrThrow();
            var layout5 = arch.GetLayout(ComponentTypeID<T5>.Value).GetValueOrThrow();

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunks.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkVersions.Add((IntPtr)chunkRef.GetVersionUnsafePtr());
                chunkEntityCounts.Add(chunkRef._count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);
                versionIndices0.Add(layout0.versionIndex);

                offsets1.Add(layout1.offset);
                bitsOffsets1.Add(layout1.enableBitsOffset);
                versionIndices1.Add(layout1.versionIndex);

                offsets2.Add(layout2.offset);
                bitsOffsets2.Add(layout2.enableBitsOffset);
                versionIndices2.Add(layout2.versionIndex);

                offsets3.Add(layout3.offset);
                bitsOffsets3.Add(layout3.enableBitsOffset);
                versionIndices3.Add(layout3.versionIndex);

                offsets4.Add(layout4.offset);
                bitsOffsets4.Add(layout4.enableBitsOffset);
                versionIndices4.Add(layout4.versionIndex);

                offsets5.Add(layout5.offset);
                bitsOffsets5.Add(layout5.enableBitsOffset);
                versionIndices5.Add(layout5.versionIndex);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0, T1, T2, T3, T4, T5>
        {
            userJob = jobData,
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,
            versionIndices1 = versionIndices1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,
            versionIndices2 = versionIndices2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,
            versionIndices3 = versionIndices3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,
            versionIndices4 = versionIndices4,

            offsets5 = offsets5,
            bitsOffsets5 = bitsOffsets5,
            versionIndices5 = versionIndices5,

            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i = 0; i < 1; i++)
            {
                if (id == runner.componentIDs[i])
                {
                    runner.componentRW[i] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, chunks.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity6
        {
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,
            versionIndices1 = versionIndices1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,
            versionIndices2 = versionIndices2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,
            versionIndices3 = versionIndices3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,
            versionIndices4 = versionIndices4,

            offsets5 = offsets5,
            bitsOffsets5 = bitsOffsets5,
            versionIndices5 = versionIndices5,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity7 : IJob
    {
        public UnsafeList<IntPtr> chunks;
        public UnsafeList<IntPtr> chunkVersions;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;
        public UnsafeList<int> versionIndices0;

        public UnsafeList<int> offsets1;
        public UnsafeList<int> bitsOffsets1;
        public UnsafeList<int> versionIndices1;

        public UnsafeList<int> offsets2;
        public UnsafeList<int> bitsOffsets2;
        public UnsafeList<int> versionIndices2;

        public UnsafeList<int> offsets3;
        public UnsafeList<int> bitsOffsets3;
        public UnsafeList<int> versionIndices3;

        public UnsafeList<int> offsets4;
        public UnsafeList<int> bitsOffsets4;
        public UnsafeList<int> versionIndices4;

        public UnsafeList<int> offsets5;
        public UnsafeList<int> bitsOffsets5;
        public UnsafeList<int> versionIndices5;

        public UnsafeList<int> offsets6;
        public UnsafeList<int> bitsOffsets6;
        public UnsafeList<int> versionIndices6;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            chunks.Dispose();
            chunkVersions.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();
            versionIndices0.Dispose();

            offsets1.Dispose();
            bitsOffsets1.Dispose();
            versionIndices1.Dispose();

            offsets2.Dispose();
            bitsOffsets2.Dispose();
            versionIndices2.Dispose();

            offsets3.Dispose();
            bitsOffsets3.Dispose();
            versionIndices3.Dispose();

            offsets4.Dispose();
            bitsOffsets4.Dispose();
            versionIndices4.Dispose();

            offsets5.Dispose();
            bitsOffsets5.Dispose();
            versionIndices5.Dispose();

            offsets6.Dispose();
            bitsOffsets6.Dispose();
            versionIndices6.Dispose();

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

        // 1. Flatten the World
        var chunks = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkVersions = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkEntityCounts = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var entityOffsets = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets3 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets3 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices3 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets4 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets4 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices4 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets5 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets5 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices5 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets6 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets6 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices6 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
            var layout1 = arch.GetLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
            var layout2 = arch.GetLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
            var layout3 = arch.GetLayout(ComponentTypeID<T3>.Value).GetValueOrThrow();
            var layout4 = arch.GetLayout(ComponentTypeID<T4>.Value).GetValueOrThrow();
            var layout5 = arch.GetLayout(ComponentTypeID<T5>.Value).GetValueOrThrow();
            var layout6 = arch.GetLayout(ComponentTypeID<T6>.Value).GetValueOrThrow();

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunks.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkVersions.Add((IntPtr)chunkRef.GetVersionUnsafePtr());
                chunkEntityCounts.Add(chunkRef._count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);
                versionIndices0.Add(layout0.versionIndex);

                offsets1.Add(layout1.offset);
                bitsOffsets1.Add(layout1.enableBitsOffset);
                versionIndices1.Add(layout1.versionIndex);

                offsets2.Add(layout2.offset);
                bitsOffsets2.Add(layout2.enableBitsOffset);
                versionIndices2.Add(layout2.versionIndex);

                offsets3.Add(layout3.offset);
                bitsOffsets3.Add(layout3.enableBitsOffset);
                versionIndices3.Add(layout3.versionIndex);

                offsets4.Add(layout4.offset);
                bitsOffsets4.Add(layout4.enableBitsOffset);
                versionIndices4.Add(layout4.versionIndex);

                offsets5.Add(layout5.offset);
                bitsOffsets5.Add(layout5.enableBitsOffset);
                versionIndices5.Add(layout5.versionIndex);

                offsets6.Add(layout6.offset);
                bitsOffsets6.Add(layout6.enableBitsOffset);
                versionIndices6.Add(layout6.versionIndex);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0, T1, T2, T3, T4, T5, T6>
        {
            userJob = jobData,
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,
            versionIndices1 = versionIndices1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,
            versionIndices2 = versionIndices2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,
            versionIndices3 = versionIndices3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,
            versionIndices4 = versionIndices4,

            offsets5 = offsets5,
            bitsOffsets5 = bitsOffsets5,
            versionIndices5 = versionIndices5,

            offsets6 = offsets6,
            bitsOffsets6 = bitsOffsets6,
            versionIndices6 = versionIndices6,

            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i = 0; i < 1; i++)
            {
                if (id == runner.componentIDs[i])
                {
                    runner.componentRW[i] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, chunks.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity7
        {
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,
            versionIndices1 = versionIndices1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,
            versionIndices2 = versionIndices2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,
            versionIndices3 = versionIndices3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,
            versionIndices4 = versionIndices4,

            offsets5 = offsets5,
            bitsOffsets5 = bitsOffsets5,
            versionIndices5 = versionIndices5,

            offsets6 = offsets6,
            bitsOffsets6 = bitsOffsets6,
            versionIndices6 = versionIndices6,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity8 : IJob
    {
        public UnsafeList<IntPtr> chunks;
        public UnsafeList<IntPtr> chunkVersions;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;
        public UnsafeList<int> versionIndices0;

        public UnsafeList<int> offsets1;
        public UnsafeList<int> bitsOffsets1;
        public UnsafeList<int> versionIndices1;

        public UnsafeList<int> offsets2;
        public UnsafeList<int> bitsOffsets2;
        public UnsafeList<int> versionIndices2;

        public UnsafeList<int> offsets3;
        public UnsafeList<int> bitsOffsets3;
        public UnsafeList<int> versionIndices3;

        public UnsafeList<int> offsets4;
        public UnsafeList<int> bitsOffsets4;
        public UnsafeList<int> versionIndices4;

        public UnsafeList<int> offsets5;
        public UnsafeList<int> bitsOffsets5;
        public UnsafeList<int> versionIndices5;

        public UnsafeList<int> offsets6;
        public UnsafeList<int> bitsOffsets6;
        public UnsafeList<int> versionIndices6;

        public UnsafeList<int> offsets7;
        public UnsafeList<int> bitsOffsets7;
        public UnsafeList<int> versionIndices7;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            chunks.Dispose();
            chunkVersions.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();
            versionIndices0.Dispose();

            offsets1.Dispose();
            bitsOffsets1.Dispose();
            versionIndices1.Dispose();

            offsets2.Dispose();
            bitsOffsets2.Dispose();
            versionIndices2.Dispose();

            offsets3.Dispose();
            bitsOffsets3.Dispose();
            versionIndices3.Dispose();

            offsets4.Dispose();
            bitsOffsets4.Dispose();
            versionIndices4.Dispose();

            offsets5.Dispose();
            bitsOffsets5.Dispose();
            versionIndices5.Dispose();

            offsets6.Dispose();
            bitsOffsets6.Dispose();
            versionIndices6.Dispose();

            offsets7.Dispose();
            bitsOffsets7.Dispose();
            versionIndices7.Dispose();

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

        // 1. Flatten the World
        var chunks = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkVersions = new UnsafeList<IntPtr>(128, TempJobAllocator.AllocationHandle);
        var chunkEntityCounts = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var entityOffsets = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices0 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices1 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices2 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets3 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets3 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices3 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets4 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets4 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices4 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets5 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets5 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices5 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets6 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets6 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices6 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        var offsets7 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var bitsOffsets7 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);
        var versionIndices7 = new UnsafeList<int>(128, TempJobAllocator.AllocationHandle);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.ComponentManager.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.Value).GetValueOrThrow();
            var layout1 = arch.GetLayout(ComponentTypeID<T1>.Value).GetValueOrThrow();
            var layout2 = arch.GetLayout(ComponentTypeID<T2>.Value).GetValueOrThrow();
            var layout3 = arch.GetLayout(ComponentTypeID<T3>.Value).GetValueOrThrow();
            var layout4 = arch.GetLayout(ComponentTypeID<T4>.Value).GetValueOrThrow();
            var layout5 = arch.GetLayout(ComponentTypeID<T5>.Value).GetValueOrThrow();
            var layout6 = arch.GetLayout(ComponentTypeID<T6>.Value).GetValueOrThrow();
            var layout7 = arch.GetLayout(ComponentTypeID<T7>.Value).GetValueOrThrow();

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunks.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkVersions.Add((IntPtr)chunkRef.GetVersionUnsafePtr());
                chunkEntityCounts.Add(chunkRef._count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);
                versionIndices0.Add(layout0.versionIndex);

                offsets1.Add(layout1.offset);
                bitsOffsets1.Add(layout1.enableBitsOffset);
                versionIndices1.Add(layout1.versionIndex);

                offsets2.Add(layout2.offset);
                bitsOffsets2.Add(layout2.enableBitsOffset);
                versionIndices2.Add(layout2.versionIndex);

                offsets3.Add(layout3.offset);
                bitsOffsets3.Add(layout3.enableBitsOffset);
                versionIndices3.Add(layout3.versionIndex);

                offsets4.Add(layout4.offset);
                bitsOffsets4.Add(layout4.enableBitsOffset);
                versionIndices4.Add(layout4.versionIndex);

                offsets5.Add(layout5.offset);
                bitsOffsets5.Add(layout5.enableBitsOffset);
                versionIndices5.Add(layout5.versionIndex);

                offsets6.Add(layout6.offset);
                bitsOffsets6.Add(layout6.enableBitsOffset);
                versionIndices6.Add(layout6.versionIndex);

                offsets7.Add(layout7.offset);
                bitsOffsets7.Add(layout7.enableBitsOffset);
                versionIndices7.Add(layout7.versionIndex);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0, T1, T2, T3, T4, T5, T6, T7>
        {
            userJob = jobData,
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,
            versionIndices1 = versionIndices1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,
            versionIndices2 = versionIndices2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,
            versionIndices3 = versionIndices3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,
            versionIndices4 = versionIndices4,

            offsets5 = offsets5,
            bitsOffsets5 = bitsOffsets5,
            versionIndices5 = versionIndices5,

            offsets6 = offsets6,
            bitsOffsets6 = bitsOffsets6,
            versionIndices6 = versionIndices6,

            offsets7 = offsets7,
            bitsOffsets7 = bitsOffsets7,
            versionIndices7 = versionIndices7,

            version = world.Version,
        };

        runner.componentIDs[0] = ComponentTypeID<T0>.Value;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i = 0; i < 1; i++)
            {
                if (id == runner.componentIDs[i])
                {
                    runner.componentRW[i] = true;
                    break;
                }
            }
        }

        var jobHandle = world.JobScheduler.ScheduleParallelFor(ref runner, chunks.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity8
        {
            chunks = chunks,
            chunkVersions = chunkVersions,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,
            versionIndices0 = versionIndices0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,
            versionIndices1 = versionIndices1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,
            versionIndices2 = versionIndices2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,
            versionIndices3 = versionIndices3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,
            versionIndices4 = versionIndices4,

            offsets5 = offsets5,
            bitsOffsets5 = bitsOffsets5,
            versionIndices5 = versionIndices5,

            offsets6 = offsets6,
            bitsOffsets6 = bitsOffsets6,
            versionIndices6 = versionIndices6,

            offsets7 = offsets7,
            bitsOffsets7 = bitsOffsets7,
            versionIndices7 = versionIndices7,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

}
