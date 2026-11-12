using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Streaming;

public unsafe struct AssetPayload : IDisposable
{
    private byte* _pointer;
    private long _size;
    private MemoryBlock _allocatedMemory;

    public readonly byte* Pointer => _pointer;
    public readonly long Size => _size;
    public readonly bool IsValid => _pointer != null;

    public readonly ReadOnlySpan<byte> Span => new(_pointer, (int)Math.Min(0x7fffffffL, _size));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<byte> Slice(long offset, int length)
    {
        Logger.DebugAssert(_pointer != null, "AssetPayload has been disposed.");
        Logger.DebugAssert(offset >= 0 && length >= 0 && offset + length <= _size, "Slice range is out of bounds.");
        return new ReadOnlySpan<byte>(_pointer + offset, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Read(long offset, Span<byte> destination)
    {
        Logger.DebugAssert(_pointer != null, "AssetPayload has been disposed.");
        Logger.DebugAssert(offset >= 0 && offset + destination.Length <= _size, "Read range is out of bounds.");
        new ReadOnlySpan<byte>(_pointer + offset, destination.Length).CopyTo(destination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T Read<T>(long offset)
        where T : unmanaged
    {
        Logger.DebugAssert(_pointer != null, "AssetPayload has been disposed.");
        Logger.DebugAssert(offset >= 0 && offset + sizeof(T) <= _size, "Read<T> range is out of bounds.");
        return *(T*)(_pointer + offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AssetPayload FromMappedMemory(byte* pointer, long size)
    {
        return new AssetPayload
        {
            _pointer = pointer,
            _size = size,
            _allocatedMemory = default
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AssetPayload FromAllocatedMemory(MemoryBlock block, long uncompressedSize)
    {
        return new AssetPayload
        {
            _pointer = (byte*)block.GetUnsafePtr(),
            _size = uncompressedSize,
            _allocatedMemory = block
        };
    }

    public void Dispose()
    {
        if (_allocatedMemory.IsCreated)
        {
            _allocatedMemory.Dispose();
            _allocatedMemory = default;
        }

        _pointer = null;
        _size = 0;
    }
}
