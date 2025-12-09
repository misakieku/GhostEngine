using Ghost.Core;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Entities;

public interface IJobEntity<T0>
    where T0 : unmanaged, IComponent
{
    void Execute(Entity entity, ref T0 component0);
}

internal unsafe struct JobEntityBatch<TJob, T0> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0>
    where T0 : unmanaged, IComponent
{
    public TJob userJob;

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;

    public void Execute(int loopIndex, int threadIndex)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);

        for (var i = 0; i < count; i++)
        {
            if (enableOff0 != -1 && !EntityQuery.CheckBit(pChunk + enableOff0, i))
            {
                continue;
            }

            userJob.Execute(pEntity[i], ref ptr0[i]);
        }
    }
}

public interface IJobEntity<T0, T1>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
{
    void Execute(Entity entity, ref T0 component0, ref T1 component1);
}

internal unsafe struct JobEntityBatch<TJob, T0, T1> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0, T1>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
{
    public TJob userJob;

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;

    public UnsafeList<int> offsets1;
    public UnsafeList<int> bitsOffsets1;

    public void Execute(int loopIndex, int threadIndex)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];

        var off1 = offsets1[loopIndex];
        var enableOff1 = bitsOffsets1[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);
        var ptr1 = (T1*)(pChunk + off1);

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

            userJob.Execute(pEntity[i], ref ptr0[i], ref ptr1[i]);
        }
    }
}

public interface IJobEntity<T0, T1, T2>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
{
    void Execute(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2);
}

internal unsafe struct JobEntityBatch<TJob, T0, T1, T2> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0, T1, T2>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
{
    public TJob userJob;

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;

    public UnsafeList<int> offsets1;
    public UnsafeList<int> bitsOffsets1;

    public UnsafeList<int> offsets2;
    public UnsafeList<int> bitsOffsets2;

    public void Execute(int loopIndex, int threadIndex)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];

        var off1 = offsets1[loopIndex];
        var enableOff1 = bitsOffsets1[loopIndex];

        var off2 = offsets2[loopIndex];
        var enableOff2 = bitsOffsets2[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);
        var ptr1 = (T1*)(pChunk + off1);
        var ptr2 = (T2*)(pChunk + off2);

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

            userJob.Execute(pEntity[i], ref ptr0[i], ref ptr1[i], ref ptr2[i]);
        }
    }
}

public interface IJobEntity<T0, T1, T2, T3>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
{
    void Execute(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3);
}

internal unsafe struct JobEntityBatch<TJob, T0, T1, T2, T3> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0, T1, T2, T3>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
{
    public TJob userJob;

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;

    public UnsafeList<int> offsets1;
    public UnsafeList<int> bitsOffsets1;

    public UnsafeList<int> offsets2;
    public UnsafeList<int> bitsOffsets2;

    public UnsafeList<int> offsets3;
    public UnsafeList<int> bitsOffsets3;

    public void Execute(int loopIndex, int threadIndex)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];

        var off1 = offsets1[loopIndex];
        var enableOff1 = bitsOffsets1[loopIndex];

        var off2 = offsets2[loopIndex];
        var enableOff2 = bitsOffsets2[loopIndex];

        var off3 = offsets3[loopIndex];
        var enableOff3 = bitsOffsets3[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);
        var ptr1 = (T1*)(pChunk + off1);
        var ptr2 = (T2*)(pChunk + off2);
        var ptr3 = (T3*)(pChunk + off3);

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

            userJob.Execute(pEntity[i], ref ptr0[i], ref ptr1[i], ref ptr2[i], ref ptr3[i]);
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
    void Execute(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4);
}

internal unsafe struct JobEntityBatch<TJob, T0, T1, T2, T3, T4> : IJobParallelFor
    where TJob : unmanaged, IJobEntity<T0, T1, T2, T3, T4>
    where T0 : unmanaged, IComponent
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
{
    public TJob userJob;

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;

    public UnsafeList<int> offsets1;
    public UnsafeList<int> bitsOffsets1;

    public UnsafeList<int> offsets2;
    public UnsafeList<int> bitsOffsets2;

    public UnsafeList<int> offsets3;
    public UnsafeList<int> bitsOffsets3;

    public UnsafeList<int> offsets4;
    public UnsafeList<int> bitsOffsets4;

    public void Execute(int loopIndex, int threadIndex)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];

        var off1 = offsets1[loopIndex];
        var enableOff1 = bitsOffsets1[loopIndex];

        var off2 = offsets2[loopIndex];
        var enableOff2 = bitsOffsets2[loopIndex];

        var off3 = offsets3[loopIndex];
        var enableOff3 = bitsOffsets3[loopIndex];

        var off4 = offsets4[loopIndex];
        var enableOff4 = bitsOffsets4[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);
        var ptr1 = (T1*)(pChunk + off1);
        var ptr2 = (T2*)(pChunk + off2);
        var ptr3 = (T3*)(pChunk + off3);
        var ptr4 = (T4*)(pChunk + off4);

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

            userJob.Execute(pEntity[i], ref ptr0[i], ref ptr1[i], ref ptr2[i], ref ptr3[i], ref ptr4[i]);
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
    void Execute(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5);
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
    public TJob userJob;

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;

    public UnsafeList<int> offsets1;
    public UnsafeList<int> bitsOffsets1;

    public UnsafeList<int> offsets2;
    public UnsafeList<int> bitsOffsets2;

    public UnsafeList<int> offsets3;
    public UnsafeList<int> bitsOffsets3;

    public UnsafeList<int> offsets4;
    public UnsafeList<int> bitsOffsets4;

    public UnsafeList<int> offsets5;
    public UnsafeList<int> bitsOffsets5;

    public void Execute(int loopIndex, int threadIndex)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];

        var off1 = offsets1[loopIndex];
        var enableOff1 = bitsOffsets1[loopIndex];

        var off2 = offsets2[loopIndex];
        var enableOff2 = bitsOffsets2[loopIndex];

        var off3 = offsets3[loopIndex];
        var enableOff3 = bitsOffsets3[loopIndex];

        var off4 = offsets4[loopIndex];
        var enableOff4 = bitsOffsets4[loopIndex];

        var off5 = offsets5[loopIndex];
        var enableOff5 = bitsOffsets5[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);
        var ptr1 = (T1*)(pChunk + off1);
        var ptr2 = (T2*)(pChunk + off2);
        var ptr3 = (T3*)(pChunk + off3);
        var ptr4 = (T4*)(pChunk + off4);
        var ptr5 = (T5*)(pChunk + off5);

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

            userJob.Execute(pEntity[i], ref ptr0[i], ref ptr1[i], ref ptr2[i], ref ptr3[i], ref ptr4[i], ref ptr5[i]);
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
    void Execute(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5, ref T6 component6);
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
    public TJob userJob;

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;

    public UnsafeList<int> offsets1;
    public UnsafeList<int> bitsOffsets1;

    public UnsafeList<int> offsets2;
    public UnsafeList<int> bitsOffsets2;

    public UnsafeList<int> offsets3;
    public UnsafeList<int> bitsOffsets3;

    public UnsafeList<int> offsets4;
    public UnsafeList<int> bitsOffsets4;

    public UnsafeList<int> offsets5;
    public UnsafeList<int> bitsOffsets5;

    public UnsafeList<int> offsets6;
    public UnsafeList<int> bitsOffsets6;

    public void Execute(int loopIndex, int threadIndex)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];

        var off1 = offsets1[loopIndex];
        var enableOff1 = bitsOffsets1[loopIndex];

        var off2 = offsets2[loopIndex];
        var enableOff2 = bitsOffsets2[loopIndex];

        var off3 = offsets3[loopIndex];
        var enableOff3 = bitsOffsets3[loopIndex];

        var off4 = offsets4[loopIndex];
        var enableOff4 = bitsOffsets4[loopIndex];

        var off5 = offsets5[loopIndex];
        var enableOff5 = bitsOffsets5[loopIndex];

        var off6 = offsets6[loopIndex];
        var enableOff6 = bitsOffsets6[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);
        var ptr1 = (T1*)(pChunk + off1);
        var ptr2 = (T2*)(pChunk + off2);
        var ptr3 = (T3*)(pChunk + off3);
        var ptr4 = (T4*)(pChunk + off4);
        var ptr5 = (T5*)(pChunk + off5);
        var ptr6 = (T6*)(pChunk + off6);

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

            userJob.Execute(pEntity[i], ref ptr0[i], ref ptr1[i], ref ptr2[i], ref ptr3[i], ref ptr4[i], ref ptr5[i], ref ptr6[i]);
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
    void Execute(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5, ref T6 component6, ref T7 component7);
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
    public TJob userJob;

    public UnsafeList<IntPtr> chunks;
    public UnsafeList<int> chunkCount;
    public UnsafeList<int> entityOffset;

    public UnsafeList<int> offsets0;
    public UnsafeList<int> bitsOffsets0;

    public UnsafeList<int> offsets1;
    public UnsafeList<int> bitsOffsets1;

    public UnsafeList<int> offsets2;
    public UnsafeList<int> bitsOffsets2;

    public UnsafeList<int> offsets3;
    public UnsafeList<int> bitsOffsets3;

    public UnsafeList<int> offsets4;
    public UnsafeList<int> bitsOffsets4;

    public UnsafeList<int> offsets5;
    public UnsafeList<int> bitsOffsets5;

    public UnsafeList<int> offsets6;
    public UnsafeList<int> bitsOffsets6;

    public UnsafeList<int> offsets7;
    public UnsafeList<int> bitsOffsets7;

    public void Execute(int loopIndex, int threadIndex)
    {
        // 1. Get the specific pChunk for this thread
        var pChunk = (byte*)chunks[loopIndex];
        var count = chunkCount[loopIndex];

        var off0 = offsets0[loopIndex];
        var enableOff0 = bitsOffsets0[loopIndex];

        var off1 = offsets1[loopIndex];
        var enableOff1 = bitsOffsets1[loopIndex];

        var off2 = offsets2[loopIndex];
        var enableOff2 = bitsOffsets2[loopIndex];

        var off3 = offsets3[loopIndex];
        var enableOff3 = bitsOffsets3[loopIndex];

        var off4 = offsets4[loopIndex];
        var enableOff4 = bitsOffsets4[loopIndex];

        var off5 = offsets5[loopIndex];
        var enableOff5 = bitsOffsets5[loopIndex];

        var off6 = offsets6[loopIndex];
        var enableOff6 = bitsOffsets6[loopIndex];

        var off7 = offsets7[loopIndex];
        var enableOff7 = bitsOffsets7[loopIndex];

        var pEntity = (Entity*)(pChunk + entityOffset[loopIndex]);
        var ptr0 = (T0*)(pChunk + off0);
        var ptr1 = (T1*)(pChunk + off1);
        var ptr2 = (T2*)(pChunk + off2);
        var ptr3 = (T3*)(pChunk + off3);
        var ptr4 = (T4*)(pChunk + off4);
        var ptr5 = (T5*)(pChunk + off5);
        var ptr6 = (T6*)(pChunk + off6);
        var ptr7 = (T7*)(pChunk + off7);

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

            userJob.Execute(pEntity[i], ref ptr0[i], ref ptr1[i], ref ptr2[i], ref ptr3[i], ref ptr4[i], ref ptr5[i], ref ptr6[i], ref ptr7[i]);
        }
    }
}

public unsafe partial struct EntityQuery
{
    private struct DisposeJobEntity1 : IJob
    {
        public UnsafeList<IntPtr> chunkList;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;

        public void Execute(int threadIndex)
        {
            chunkList.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();

        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0>(TJob jobData, Allocator allocator, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobEntity<T0>
        where T0 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        // 1. Flatten the World
        var chunkList = new UnsafeList<IntPtr>(128, allocator);
        var chunkEntityCounts = new UnsafeList<int>(128, allocator);
        var entityOffsets = new UnsafeList<int>(128, allocator);

        var offsets0 = new UnsafeList<int>(128, allocator);
        var bitsOffsets0 = new UnsafeList<int>(128, allocator);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.value)
                .GetValueOrThrow(ResultStatus.Success);

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunkList.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkEntityCounts.Add(chunkRef.Count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0>
        {
            userJob = jobData,
            chunks = chunkList,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

        };

        var jobHandle = world.JobScheduler.ScheduleParallel(ref runner, chunkList.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity1
        {
            chunkList = chunkList,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity2 : IJob
    {
        public UnsafeList<IntPtr> chunkList;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;

        public UnsafeList<int> offsets1;
        public UnsafeList<int> bitsOffsets1;

        public void Execute(int threadIndex)
        {
            chunkList.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();

            offsets1.Dispose();
            bitsOffsets1.Dispose();

        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0, T1>(TJob jobData, Allocator allocator, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobEntity<T0, T1>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        // 1. Flatten the World
        var chunkList = new UnsafeList<IntPtr>(128, allocator);
        var chunkEntityCounts = new UnsafeList<int>(128, allocator);
        var entityOffsets = new UnsafeList<int>(128, allocator);

        var offsets0 = new UnsafeList<int>(128, allocator);
        var bitsOffsets0 = new UnsafeList<int>(128, allocator);

        var offsets1 = new UnsafeList<int>(128, allocator);
        var bitsOffsets1 = new UnsafeList<int>(128, allocator);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout1 = arch.GetLayout(ComponentTypeID<T1>.value)
                .GetValueOrThrow(ResultStatus.Success);

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunkList.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkEntityCounts.Add(chunkRef.Count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);

                offsets1.Add(layout1.offset);
                bitsOffsets1.Add(layout1.enableBitsOffset);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0, T1>
        {
            userJob = jobData,
            chunks = chunkList,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,

        };

        var jobHandle = world.JobScheduler.ScheduleParallel(ref runner, chunkList.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity2
        {
            chunkList = chunkList,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity3 : IJob
    {
        public UnsafeList<IntPtr> chunkList;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;

        public UnsafeList<int> offsets1;
        public UnsafeList<int> bitsOffsets1;

        public UnsafeList<int> offsets2;
        public UnsafeList<int> bitsOffsets2;

        public void Execute(int threadIndex)
        {
            chunkList.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();

            offsets1.Dispose();
            bitsOffsets1.Dispose();

            offsets2.Dispose();
            bitsOffsets2.Dispose();

        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0, T1, T2>(TJob jobData, Allocator allocator, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobEntity<T0, T1, T2>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        // 1. Flatten the World
        var chunkList = new UnsafeList<IntPtr>(128, allocator);
        var chunkEntityCounts = new UnsafeList<int>(128, allocator);
        var entityOffsets = new UnsafeList<int>(128, allocator);

        var offsets0 = new UnsafeList<int>(128, allocator);
        var bitsOffsets0 = new UnsafeList<int>(128, allocator);

        var offsets1 = new UnsafeList<int>(128, allocator);
        var bitsOffsets1 = new UnsafeList<int>(128, allocator);

        var offsets2 = new UnsafeList<int>(128, allocator);
        var bitsOffsets2 = new UnsafeList<int>(128, allocator);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout1 = arch.GetLayout(ComponentTypeID<T1>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout2 = arch.GetLayout(ComponentTypeID<T2>.value)
                .GetValueOrThrow(ResultStatus.Success);

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunkList.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkEntityCounts.Add(chunkRef.Count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);

                offsets1.Add(layout1.offset);
                bitsOffsets1.Add(layout1.enableBitsOffset);

                offsets2.Add(layout2.offset);
                bitsOffsets2.Add(layout2.enableBitsOffset);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0, T1, T2>
        {
            userJob = jobData,
            chunks = chunkList,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,

        };

        var jobHandle = world.JobScheduler.ScheduleParallel(ref runner, chunkList.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity3
        {
            chunkList = chunkList,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity4 : IJob
    {
        public UnsafeList<IntPtr> chunkList;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;

        public UnsafeList<int> offsets1;
        public UnsafeList<int> bitsOffsets1;

        public UnsafeList<int> offsets2;
        public UnsafeList<int> bitsOffsets2;

        public UnsafeList<int> offsets3;
        public UnsafeList<int> bitsOffsets3;

        public void Execute(int threadIndex)
        {
            chunkList.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();

            offsets1.Dispose();
            bitsOffsets1.Dispose();

            offsets2.Dispose();
            bitsOffsets2.Dispose();

            offsets3.Dispose();
            bitsOffsets3.Dispose();

        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0, T1, T2, T3>(TJob jobData, Allocator allocator, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobEntity<T0, T1, T2, T3>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        // 1. Flatten the World
        var chunkList = new UnsafeList<IntPtr>(128, allocator);
        var chunkEntityCounts = new UnsafeList<int>(128, allocator);
        var entityOffsets = new UnsafeList<int>(128, allocator);

        var offsets0 = new UnsafeList<int>(128, allocator);
        var bitsOffsets0 = new UnsafeList<int>(128, allocator);

        var offsets1 = new UnsafeList<int>(128, allocator);
        var bitsOffsets1 = new UnsafeList<int>(128, allocator);

        var offsets2 = new UnsafeList<int>(128, allocator);
        var bitsOffsets2 = new UnsafeList<int>(128, allocator);

        var offsets3 = new UnsafeList<int>(128, allocator);
        var bitsOffsets3 = new UnsafeList<int>(128, allocator);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout1 = arch.GetLayout(ComponentTypeID<T1>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout2 = arch.GetLayout(ComponentTypeID<T2>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout3 = arch.GetLayout(ComponentTypeID<T3>.value)
                .GetValueOrThrow(ResultStatus.Success);

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunkList.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkEntityCounts.Add(chunkRef.Count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);

                offsets1.Add(layout1.offset);
                bitsOffsets1.Add(layout1.enableBitsOffset);

                offsets2.Add(layout2.offset);
                bitsOffsets2.Add(layout2.enableBitsOffset);

                offsets3.Add(layout3.offset);
                bitsOffsets3.Add(layout3.enableBitsOffset);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0, T1, T2, T3>
        {
            userJob = jobData,
            chunks = chunkList,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,

        };

        var jobHandle = world.JobScheduler.ScheduleParallel(ref runner, chunkList.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity4
        {
            chunkList = chunkList,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity5 : IJob
    {
        public UnsafeList<IntPtr> chunkList;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;

        public UnsafeList<int> offsets1;
        public UnsafeList<int> bitsOffsets1;

        public UnsafeList<int> offsets2;
        public UnsafeList<int> bitsOffsets2;

        public UnsafeList<int> offsets3;
        public UnsafeList<int> bitsOffsets3;

        public UnsafeList<int> offsets4;
        public UnsafeList<int> bitsOffsets4;

        public void Execute(int threadIndex)
        {
            chunkList.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();

            offsets1.Dispose();
            bitsOffsets1.Dispose();

            offsets2.Dispose();
            bitsOffsets2.Dispose();

            offsets3.Dispose();
            bitsOffsets3.Dispose();

            offsets4.Dispose();
            bitsOffsets4.Dispose();

        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0, T1, T2, T3, T4>(TJob jobData, Allocator allocator, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobEntity<T0, T1, T2, T3, T4>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        // 1. Flatten the World
        var chunkList = new UnsafeList<IntPtr>(128, allocator);
        var chunkEntityCounts = new UnsafeList<int>(128, allocator);
        var entityOffsets = new UnsafeList<int>(128, allocator);

        var offsets0 = new UnsafeList<int>(128, allocator);
        var bitsOffsets0 = new UnsafeList<int>(128, allocator);

        var offsets1 = new UnsafeList<int>(128, allocator);
        var bitsOffsets1 = new UnsafeList<int>(128, allocator);

        var offsets2 = new UnsafeList<int>(128, allocator);
        var bitsOffsets2 = new UnsafeList<int>(128, allocator);

        var offsets3 = new UnsafeList<int>(128, allocator);
        var bitsOffsets3 = new UnsafeList<int>(128, allocator);

        var offsets4 = new UnsafeList<int>(128, allocator);
        var bitsOffsets4 = new UnsafeList<int>(128, allocator);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout1 = arch.GetLayout(ComponentTypeID<T1>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout2 = arch.GetLayout(ComponentTypeID<T2>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout3 = arch.GetLayout(ComponentTypeID<T3>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout4 = arch.GetLayout(ComponentTypeID<T4>.value)
                .GetValueOrThrow(ResultStatus.Success);

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunkList.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkEntityCounts.Add(chunkRef.Count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);

                offsets1.Add(layout1.offset);
                bitsOffsets1.Add(layout1.enableBitsOffset);

                offsets2.Add(layout2.offset);
                bitsOffsets2.Add(layout2.enableBitsOffset);

                offsets3.Add(layout3.offset);
                bitsOffsets3.Add(layout3.enableBitsOffset);

                offsets4.Add(layout4.offset);
                bitsOffsets4.Add(layout4.enableBitsOffset);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0, T1, T2, T3, T4>
        {
            userJob = jobData,
            chunks = chunkList,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,

        };

        var jobHandle = world.JobScheduler.ScheduleParallel(ref runner, chunkList.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity5
        {
            chunkList = chunkList,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity6 : IJob
    {
        public UnsafeList<IntPtr> chunkList;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;

        public UnsafeList<int> offsets1;
        public UnsafeList<int> bitsOffsets1;

        public UnsafeList<int> offsets2;
        public UnsafeList<int> bitsOffsets2;

        public UnsafeList<int> offsets3;
        public UnsafeList<int> bitsOffsets3;

        public UnsafeList<int> offsets4;
        public UnsafeList<int> bitsOffsets4;

        public UnsafeList<int> offsets5;
        public UnsafeList<int> bitsOffsets5;

        public void Execute(int threadIndex)
        {
            chunkList.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();

            offsets1.Dispose();
            bitsOffsets1.Dispose();

            offsets2.Dispose();
            bitsOffsets2.Dispose();

            offsets3.Dispose();
            bitsOffsets3.Dispose();

            offsets4.Dispose();
            bitsOffsets4.Dispose();

            offsets5.Dispose();
            bitsOffsets5.Dispose();

        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0, T1, T2, T3, T4, T5>(TJob jobData, Allocator allocator, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobEntity<T0, T1, T2, T3, T4, T5>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        // 1. Flatten the World
        var chunkList = new UnsafeList<IntPtr>(128, allocator);
        var chunkEntityCounts = new UnsafeList<int>(128, allocator);
        var entityOffsets = new UnsafeList<int>(128, allocator);

        var offsets0 = new UnsafeList<int>(128, allocator);
        var bitsOffsets0 = new UnsafeList<int>(128, allocator);

        var offsets1 = new UnsafeList<int>(128, allocator);
        var bitsOffsets1 = new UnsafeList<int>(128, allocator);

        var offsets2 = new UnsafeList<int>(128, allocator);
        var bitsOffsets2 = new UnsafeList<int>(128, allocator);

        var offsets3 = new UnsafeList<int>(128, allocator);
        var bitsOffsets3 = new UnsafeList<int>(128, allocator);

        var offsets4 = new UnsafeList<int>(128, allocator);
        var bitsOffsets4 = new UnsafeList<int>(128, allocator);

        var offsets5 = new UnsafeList<int>(128, allocator);
        var bitsOffsets5 = new UnsafeList<int>(128, allocator);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout1 = arch.GetLayout(ComponentTypeID<T1>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout2 = arch.GetLayout(ComponentTypeID<T2>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout3 = arch.GetLayout(ComponentTypeID<T3>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout4 = arch.GetLayout(ComponentTypeID<T4>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout5 = arch.GetLayout(ComponentTypeID<T5>.value)
                .GetValueOrThrow(ResultStatus.Success);

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunkList.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkEntityCounts.Add(chunkRef.Count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);

                offsets1.Add(layout1.offset);
                bitsOffsets1.Add(layout1.enableBitsOffset);

                offsets2.Add(layout2.offset);
                bitsOffsets2.Add(layout2.enableBitsOffset);

                offsets3.Add(layout3.offset);
                bitsOffsets3.Add(layout3.enableBitsOffset);

                offsets4.Add(layout4.offset);
                bitsOffsets4.Add(layout4.enableBitsOffset);

                offsets5.Add(layout5.offset);
                bitsOffsets5.Add(layout5.enableBitsOffset);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0, T1, T2, T3, T4, T5>
        {
            userJob = jobData,
            chunks = chunkList,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,

            offsets5 = offsets5,
            bitsOffsets5 = bitsOffsets5,

        };

        var jobHandle = world.JobScheduler.ScheduleParallel(ref runner, chunkList.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity6
        {
            chunkList = chunkList,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,

            offsets5 = offsets5,
            bitsOffsets5 = bitsOffsets5,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity7 : IJob
    {
        public UnsafeList<IntPtr> chunkList;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;

        public UnsafeList<int> offsets1;
        public UnsafeList<int> bitsOffsets1;

        public UnsafeList<int> offsets2;
        public UnsafeList<int> bitsOffsets2;

        public UnsafeList<int> offsets3;
        public UnsafeList<int> bitsOffsets3;

        public UnsafeList<int> offsets4;
        public UnsafeList<int> bitsOffsets4;

        public UnsafeList<int> offsets5;
        public UnsafeList<int> bitsOffsets5;

        public UnsafeList<int> offsets6;
        public UnsafeList<int> bitsOffsets6;

        public void Execute(int threadIndex)
        {
            chunkList.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();

            offsets1.Dispose();
            bitsOffsets1.Dispose();

            offsets2.Dispose();
            bitsOffsets2.Dispose();

            offsets3.Dispose();
            bitsOffsets3.Dispose();

            offsets4.Dispose();
            bitsOffsets4.Dispose();

            offsets5.Dispose();
            bitsOffsets5.Dispose();

            offsets6.Dispose();
            bitsOffsets6.Dispose();

        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0, T1, T2, T3, T4, T5, T6>(TJob jobData, Allocator allocator, int batchSize, JobHandle dependency)
        where TJob : unmanaged, IJobEntity<T0, T1, T2, T3, T4, T5, T6>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        // 1. Flatten the World
        var chunkList = new UnsafeList<IntPtr>(128, allocator);
        var chunkEntityCounts = new UnsafeList<int>(128, allocator);
        var entityOffsets = new UnsafeList<int>(128, allocator);

        var offsets0 = new UnsafeList<int>(128, allocator);
        var bitsOffsets0 = new UnsafeList<int>(128, allocator);

        var offsets1 = new UnsafeList<int>(128, allocator);
        var bitsOffsets1 = new UnsafeList<int>(128, allocator);

        var offsets2 = new UnsafeList<int>(128, allocator);
        var bitsOffsets2 = new UnsafeList<int>(128, allocator);

        var offsets3 = new UnsafeList<int>(128, allocator);
        var bitsOffsets3 = new UnsafeList<int>(128, allocator);

        var offsets4 = new UnsafeList<int>(128, allocator);
        var bitsOffsets4 = new UnsafeList<int>(128, allocator);

        var offsets5 = new UnsafeList<int>(128, allocator);
        var bitsOffsets5 = new UnsafeList<int>(128, allocator);

        var offsets6 = new UnsafeList<int>(128, allocator);
        var bitsOffsets6 = new UnsafeList<int>(128, allocator);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout1 = arch.GetLayout(ComponentTypeID<T1>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout2 = arch.GetLayout(ComponentTypeID<T2>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout3 = arch.GetLayout(ComponentTypeID<T3>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout4 = arch.GetLayout(ComponentTypeID<T4>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout5 = arch.GetLayout(ComponentTypeID<T5>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout6 = arch.GetLayout(ComponentTypeID<T6>.value)
                .GetValueOrThrow(ResultStatus.Success);

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunkList.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkEntityCounts.Add(chunkRef.Count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);

                offsets1.Add(layout1.offset);
                bitsOffsets1.Add(layout1.enableBitsOffset);

                offsets2.Add(layout2.offset);
                bitsOffsets2.Add(layout2.enableBitsOffset);

                offsets3.Add(layout3.offset);
                bitsOffsets3.Add(layout3.enableBitsOffset);

                offsets4.Add(layout4.offset);
                bitsOffsets4.Add(layout4.enableBitsOffset);

                offsets5.Add(layout5.offset);
                bitsOffsets5.Add(layout5.enableBitsOffset);

                offsets6.Add(layout6.offset);
                bitsOffsets6.Add(layout6.enableBitsOffset);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0, T1, T2, T3, T4, T5, T6>
        {
            userJob = jobData,
            chunks = chunkList,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,

            offsets5 = offsets5,
            bitsOffsets5 = bitsOffsets5,

            offsets6 = offsets6,
            bitsOffsets6 = bitsOffsets6,

        };

        var jobHandle = world.JobScheduler.ScheduleParallel(ref runner, chunkList.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity7
        {
            chunkList = chunkList,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,

            offsets5 = offsets5,
            bitsOffsets5 = bitsOffsets5,

            offsets6 = offsets6,
            bitsOffsets6 = bitsOffsets6,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

    private struct DisposeJobEntity8 : IJob
    {
        public UnsafeList<IntPtr> chunkList;
        public UnsafeList<int> chunkEntityCounts;
        public UnsafeList<int> entityOffsets;

        public UnsafeList<int> offsets0;
        public UnsafeList<int> bitsOffsets0;

        public UnsafeList<int> offsets1;
        public UnsafeList<int> bitsOffsets1;

        public UnsafeList<int> offsets2;
        public UnsafeList<int> bitsOffsets2;

        public UnsafeList<int> offsets3;
        public UnsafeList<int> bitsOffsets3;

        public UnsafeList<int> offsets4;
        public UnsafeList<int> bitsOffsets4;

        public UnsafeList<int> offsets5;
        public UnsafeList<int> bitsOffsets5;

        public UnsafeList<int> offsets6;
        public UnsafeList<int> bitsOffsets6;

        public UnsafeList<int> offsets7;
        public UnsafeList<int> bitsOffsets7;

        public void Execute(int threadIndex)
        {
            chunkList.Dispose();
            chunkEntityCounts.Dispose();
            entityOffsets.Dispose();

            offsets0.Dispose();
            bitsOffsets0.Dispose();

            offsets1.Dispose();
            bitsOffsets1.Dispose();

            offsets2.Dispose();
            bitsOffsets2.Dispose();

            offsets3.Dispose();
            bitsOffsets3.Dispose();

            offsets4.Dispose();
            bitsOffsets4.Dispose();

            offsets5.Dispose();
            bitsOffsets5.Dispose();

            offsets6.Dispose();
            bitsOffsets6.Dispose();

            offsets7.Dispose();
            bitsOffsets7.Dispose();

        }
    }

    public JobHandle ScheduleEntityParallel<TJob, T0, T1, T2, T3, T4, T5, T6, T7>(TJob jobData, Allocator allocator, int batchSize, JobHandle dependency)
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
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        // 1. Flatten the World
        var chunkList = new UnsafeList<IntPtr>(128, allocator);
        var chunkEntityCounts = new UnsafeList<int>(128, allocator);
        var entityOffsets = new UnsafeList<int>(128, allocator);

        var offsets0 = new UnsafeList<int>(128, allocator);
        var bitsOffsets0 = new UnsafeList<int>(128, allocator);

        var offsets1 = new UnsafeList<int>(128, allocator);
        var bitsOffsets1 = new UnsafeList<int>(128, allocator);

        var offsets2 = new UnsafeList<int>(128, allocator);
        var bitsOffsets2 = new UnsafeList<int>(128, allocator);

        var offsets3 = new UnsafeList<int>(128, allocator);
        var bitsOffsets3 = new UnsafeList<int>(128, allocator);

        var offsets4 = new UnsafeList<int>(128, allocator);
        var bitsOffsets4 = new UnsafeList<int>(128, allocator);

        var offsets5 = new UnsafeList<int>(128, allocator);
        var bitsOffsets5 = new UnsafeList<int>(128, allocator);

        var offsets6 = new UnsafeList<int>(128, allocator);
        var bitsOffsets6 = new UnsafeList<int>(128, allocator);

        var offsets7 = new UnsafeList<int>(128, allocator);
        var bitsOffsets7 = new UnsafeList<int>(128, allocator);

        // Iterate the Query's matching archetypes
        foreach (var archID in _matchingArchetypes)
        {
            ref var arch = ref world.GetArchetypeReference(archID);

            if (arch.ChunkCount == 0)
            {
                continue;
            }

            // Get offsets ONCE per archetype
            var layout0 = arch.GetLayout(ComponentTypeID<T0>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout1 = arch.GetLayout(ComponentTypeID<T1>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout2 = arch.GetLayout(ComponentTypeID<T2>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout3 = arch.GetLayout(ComponentTypeID<T3>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout4 = arch.GetLayout(ComponentTypeID<T4>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout5 = arch.GetLayout(ComponentTypeID<T5>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout6 = arch.GetLayout(ComponentTypeID<T6>.value)
                .GetValueOrThrow(ResultStatus.Success);
            var layout7 = arch.GetLayout(ComponentTypeID<T7>.value)
                .GetValueOrThrow(ResultStatus.Success);

            // Add all chunks from this archetype
            for (var i = 0; i < arch.ChunkCount; i++)
            {
                ref var chunkRef = ref arch.GetChunkReference(i);

                chunkList.Add((IntPtr)chunkRef.GetUnsafePtr());
                chunkEntityCounts.Add(chunkRef.Count);
                entityOffsets.Add(arch.EntityIDsOffset);

                offsets0.Add(layout0.offset);
                bitsOffsets0.Add(layout0.enableBitsOffset);

                offsets1.Add(layout1.offset);
                bitsOffsets1.Add(layout1.enableBitsOffset);

                offsets2.Add(layout2.offset);
                bitsOffsets2.Add(layout2.enableBitsOffset);

                offsets3.Add(layout3.offset);
                bitsOffsets3.Add(layout3.enableBitsOffset);

                offsets4.Add(layout4.offset);
                bitsOffsets4.Add(layout4.enableBitsOffset);

                offsets5.Add(layout5.offset);
                bitsOffsets5.Add(layout5.enableBitsOffset);

                offsets6.Add(layout6.offset);
                bitsOffsets6.Add(layout6.enableBitsOffset);

                offsets7.Add(layout7.offset);
                bitsOffsets7.Add(layout7.enableBitsOffset);

            }
        }

        // 2. Create the Runner
        var runner = new JobEntityBatch<TJob, T0, T1, T2, T3, T4, T5, T6, T7>
        {
            userJob = jobData,
            chunks = chunkList,
            chunkCount = chunkEntityCounts,
            entityOffset = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,

            offsets5 = offsets5,
            bitsOffsets5 = bitsOffsets5,

            offsets6 = offsets6,
            bitsOffsets6 = bitsOffsets6,

            offsets7 = offsets7,
            bitsOffsets7 = bitsOffsets7,

        };

        var jobHandle = world.JobScheduler.ScheduleParallel(ref runner, chunkList.Count, batchSize, dependency);

        // 3. Dispose the temp lists
        var disposeJob = new DisposeJobEntity8
        {
            chunkList = chunkList,
            chunkEntityCounts = chunkEntityCounts,
            entityOffsets = entityOffsets,

            offsets0 = offsets0,
            bitsOffsets0 = bitsOffsets0,

            offsets1 = offsets1,
            bitsOffsets1 = bitsOffsets1,

            offsets2 = offsets2,
            bitsOffsets2 = bitsOffsets2,

            offsets3 = offsets3,
            bitsOffsets3 = bitsOffsets3,

            offsets4 = offsets4,
            bitsOffsets4 = bitsOffsets4,

            offsets5 = offsets5,
            bitsOffsets5 = bitsOffsets5,

            offsets6 = offsets6,
            bitsOffsets6 = bitsOffsets6,

            offsets7 = offsets7,
            bitsOffsets7 = bitsOffsets7,

        };

        world.JobScheduler.Schedule(ref disposeJob, jobHandle);

        return jobHandle;
    }

}
