using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using Misaki.HighPerformance.LowLevel.Utilities;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this Stream stream)
        where T : unmanaged
    {
        var value = default(T);
        stream.ReadExactly(MemoryMarshal.AsBytes(new Span<T>(ref value)));
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryBlock ReadMemory(this Stream stream, long length, AllocationHandle allocationHandle)
    {
        var alignedLength = SpanUtility.AlignUp((nuint)length, 16);
        var memory = new MemoryBlock(alignedLength, 16, allocationHandle);

        // C# built-in collections use int for indexing, so we need to ensure that the buffer size does not exceed int.MaxValue
        var maxChunkSize = (int)Math.Min(0x7fffffffL, length);
        var offset = 0L;

        while (offset < length)
        {
            var segmentSize = (int)Math.Min(maxChunkSize, stream.Length - stream.Position);
            using var mem = NativeMemoryManager<byte>.FromMemoryBlock(memory, (nuint)offset, segmentSize);
            stream.ReadExactly(mem.Memory.Span);
            offset += (uint)mem.Memory.Length;
        }

        return memory;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryBlock ReadMemory(this Stream stream, AllocationHandle allocationHandle)
    {
        return stream.ReadMemory(stream.Length - stream.Position, allocationHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this BinaryReader reader)
        where T : unmanaged
    {
        var value = default(T);
        reader.ReadExactly(MemoryMarshal.AsBytes(new Span<T>(ref value)));
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryBlock ReadMemory(this BinaryReader reader, long length, AllocationHandle allocationHandle)
    {
        return reader.BaseStream.ReadMemory(length, allocationHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryBlock ReadMemory(this BinaryReader reader, AllocationHandle allocationHandle)
    {
        return reader.BaseStream.ReadMemory(reader.BaseStream.Length - reader.BaseStream.Position, allocationHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadSpan<T>(this BinaryReader reader, Span<T> data)
    where T : struct
    {
        reader.ReadExactly(MemoryMarshal.AsBytes(data));
    }
}
