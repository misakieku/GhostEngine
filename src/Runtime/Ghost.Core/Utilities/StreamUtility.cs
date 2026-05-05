using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Core.Utilities;

public static class StreamUtility
{
    public static void Write<T>(this Stream stream, in T value)
        where T : struct
    {
        stream.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value)));
    }

    public static void Write<T>(this Stream stream, ReadOnlySpan<T> values)
        where T : struct
    {
        if (values.IsEmpty)
        {
            return;
        }

        stream.Write(MemoryMarshal.AsBytes(values));
    }

    public static async ValueTask WriteAsync<T, C>(this Stream stream, C collection, CancellationToken cancellationToken = default)
        where T : unmanaged
        where C : IUnsafeCollection<T>
    {
        if (!collection.IsCreated || collection.Count == 0)
        {
            return;
        }

        using var manager = NativeMemoryManager<byte>.FromUnsafeCollectionInterpolated<C, T>(in collection);
        await stream.WriteAsync(manager.Memory, cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask WriteAsync<T>(this Stream stream, T value, CancellationToken cancellationToken = default)
        where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            Unsafe.WriteUnaligned(ref buffer[0], value);
             await stream.WriteAsync(buffer.AsMemory(0, size), cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
