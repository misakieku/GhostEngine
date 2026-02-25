using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Core.Utilities;

public struct BufferWriter : IDisposable
{
    private UnsafeList<byte> _buffer;
    private int _position;

    public int Position
    {
        readonly get => _position;
        set => _position = value;
    }

    public BufferWriter(int initialCapacity, AllocationHandle allocationHandle)
    {
        _buffer = new UnsafeList<byte>(initialCapacity, allocationHandle);
        _position = 0;
    }

    public unsafe void Write<T>(T value)
        where T : unmanaged
    {
        Unsafe.WriteUnaligned(ref _buffer[_position], value);
        _position += sizeof(T);
    }

    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        data.CopyTo(_buffer.AsSpan().Slice(_position, data.Length));
        _position += data.Length;
    }

    public Span<byte> ReserveSpan(int length)
    {
        var span = _buffer.AsSpan().Slice(_position, length);
        _position += length;
        return span;
    }

    public readonly Span<byte> AsSpan()
    {
        return _buffer.AsSpan();
    }

    public void Dispose()
    {
        _buffer.Dispose();
    }
}
