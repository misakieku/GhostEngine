using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace Ghost.Core.Utilities;

public static unsafe class CollectionUtility
{
    public static UnsafeArray<T> Clone<T>(this UnsafeArray<T> src, AllocationHandle allocationHandle)
        where T : unmanaged
    {
        if (!src.IsCreated || src.Count == 0)
        {
            return default;
        }

        var dst = new UnsafeArray<T>(src.Count, allocationHandle);
        src.CopyTo(dst);

        return dst;
    }

    public static UnsafeList<T> Clone<T>(this UnsafeList<T> src, AllocationHandle allocationHandle)
        where T : unmanaged
    {
        if (!src.IsCreated || src.Count == 0)
        {
            return default;
        }

        var dst = new UnsafeList<T>(src.Count, allocationHandle);
        src.CopyTo(dst);

        return dst;
    }

    public static void Fill(void* ptr, nuint size, nuint count, void* value)
    {
        // Copy from c# built-in SpanHelper.Fill<T>
        // Early checks to see if it's even possible to vectorize - JIT will turn these checks into consts.
        // - Vectorization must be hardware-accelerated
        // - size must not exceed the vector's size
        // - size must be a whole power of 2

        if (!Vector.IsHardwareAccelerated)
        {
            goto CannotVectorize;
        }

        if (size > (nuint)Vector<byte>.Count)
        {
            goto CannotVectorize;
        }

        if (!BitOperations.IsPow2(size))
        {
            goto CannotVectorize;
        }

        if (count >= ((nuint)Vector<byte>.Count / size))
        {
            // We have enough data for at least one vectorized write.
            Vector<byte> vector;

            if (size == 1)
            {
                vector = new Vector<byte>(*(byte*)value);
            }
            else if (size == 2)
            {
                vector = (Vector<byte>)new Vector<ushort>(*(ushort*)value);
            }
            else if (size == 4)
            {
                // special-case float since it's already passed in a SIMD reg
                vector = (Vector<byte>)new Vector<uint>(*(uint*)value);
            }
            else if (size == 8)
            {
                // special-case double since it's already passed in a SIMD reg
                vector = (Vector<byte>)new Vector<ulong>(*(ulong*)value);
            }
            else if (size == (nuint)Vector<byte>.Count)
            {
                vector = *(Vector<byte>*)value;
            }
            else if (size == 16)
            {
                if (Vector<byte>.Count == 32)
                {
                    vector = Vector256.Create(*(Vector128<byte>*)value).AsVector();
                }
                else if (Vector<byte>.Count == 64)
                {
                    vector = Vector512.Create(*(Vector128<byte>*)value).AsVector();
                }
                else
                {
                    Debug.Fail("Vector<T> is unexpected size.");
                    goto CannotVectorize;
                }
            }
            else if (size == 32)
            {
                if (Vector<byte>.Count == 64)
                {
                    vector = Vector512.Create(*(Vector256<byte>*)value).AsVector();
                }
                else
                {
                    Debug.Fail("Vector<T> is unexpected size.");
                    goto CannotVectorize;
                }
            }
            else
            {
                Debug.Fail("Vector<T> is greater than 512 bits in size?");
                goto CannotVectorize;
            }

            var pBase = (byte*)ptr;
            var totalByteLength = count * size; // get this calculation ready ahead of time
            var stopLoopAtOffset = totalByteLength & (nuint)(nint)(2 * -Vector<byte>.Count); // intentional sign extension carries the negative bit
            nuint offset = 0;

            // Loop, writing 2 vectors at a time.
            // Compare 'count' rather than 'stopLoopAtOffset' because we don't want a dependency
            // on the very recently calculated 'stopLoopAtOffset' value.

            if (count >= (2 * (nuint)Vector<byte>.Count / size))
            {
                do
                {
                    *(Vector<byte>*)(pBase + offset) = vector;
                    *(Vector<byte>*)(pBase + offset + (nuint)Vector<byte>.Count) = vector;
                    offset += (uint)(2 * Vector<byte>.Count);
                } while (offset < stopLoopAtOffset);
            }

            // At this point, if any data remains to be written, it's strictly less than
            // 2 * sizeof(Vector) bytes. The loop above had us write an even number of vectors.
            // If the total byte length instead involves us writing an odd number of vectors, write
            // one additional vector now. The bit check below tells us if we're in an "odd vector
            // count" situation.

            if ((totalByteLength & (nuint)Vector<byte>.Count) != 0)
            {
                *(Vector<byte>*)(pBase + offset) = vector;
            }

            // It's possible that some small buffer remains to be populated - something that won't
            // fit an entire vector's worth of data. Instead of falling back to a loop, we'll write
            // a vector at the very end of the buffer. This may involve overwriting previously
            // populated data, which is fine since we're splatting the same value for all entries.
            // There's no need to perform a length check here because we already performed this
            // check before entering the vectorized code path.

            *(Vector<byte>*)(pBase + (totalByteLength - (nuint)Vector<byte>.Count)) = vector;

            // And we're done!

            return;
        }

    CannotVectorize:

        MemoryUtility.MemCpy(ptr, value, size);
        var filled = 1u;
        var dst = (byte*)ptr;

        while (filled * 2 <= count)
        {
            MemoryUtility.MemCpy(dst + (filled * size), dst, filled * size);
            filled *= 2;
        }

        if (filled < count)
        {
            MemoryUtility.MemCpy(dst + (filled * size), dst, (count - filled) * size);
        }
    }
}
