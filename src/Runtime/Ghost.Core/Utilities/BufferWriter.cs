using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Core.Utilities;

public unsafe struct BufferWriter : IDisposable
{
    private UnsafeArray<byte> _buffer;
    private int _position;

    public int Position
    {
        readonly get => _position;
        set => _position = value;
    }

    public BufferWriter(int initialCapacity, AllocationHandle allocationHandle)
    {
        _buffer = new UnsafeArray<byte>(initialCapacity, allocationHandle);
        _position = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureCapacity(int bytesNeeded)
    {
        if (_position + bytesNeeded > _buffer.Count)
        {
            _buffer.Resize(Math.Max(_buffer.Count * 2, _position + bytesNeeded));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write<T>(scoped in T value)
        where T : unmanaged
    {
        EnsureCapacity(sizeof(T));
        Unsafe.WriteUnaligned(ref _buffer[_position], value);
        _position += sizeof(T);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSpan<T>(ReadOnlySpan<T> data)
        where T : unmanaged
    {
        var size = sizeof(T) * data.Length;
        var byteSpan = MemoryMarshal.AsBytes(data);

        EnsureCapacity(size);
        byteSpan.CopyTo(_buffer.AsSpan().Slice(_position, size));
        _position += size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> ReserveSpan(int length)
    {
        EnsureCapacity(length);
        var span = _buffer.AsSpan().Slice(_position, length);
        _position += length;
        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteMemory(void* data, int size)
    {
        EnsureCapacity(size);
        Unsafe.CopyBlockUnaligned((byte*)_buffer.GetUnsafePtr() + _position, data, (uint)size);
        _position += size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> AsSpan()
    {
        return _buffer.AsSpan(0, Position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BufferReader AsReader()
    {
        return new BufferReader((byte*)_buffer.GetUnsafePtr(), (nuint)_position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _position = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        _buffer.Dispose();
    }
}

public unsafe ref struct SpanWriter
{
    private Span<byte> _buffer;
    private int _position;

    public int Position
    {
        readonly get => _position;
        set => _position = value;
    }

    public readonly int RemainingBytes => _buffer.Length - _position;

    public SpanWriter(Span<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write<T>(scoped in T value)
        where T : unmanaged
    {
        Unsafe.WriteUnaligned(ref _buffer[_position], value);
        _position += sizeof(T);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSpan<T>(ReadOnlySpan<T> data)
        where T : unmanaged
    {
        var size = sizeof(T) * data.Length;
        var byteSpan = MemoryMarshal.AsBytes(data);

        byteSpan.CopyTo(_buffer.Slice(_position, size));
        _position += size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> AsSpan()
    {
        return _buffer;
    }
}

public interface IBufferReader
{
    nuint Position
    {
        get; set;
    }

    nuint RemainingBytes
    {
        get;
    }

    T Read<T>()
        where T : unmanaged;

    void ReadExactly<T>(Span<T> dst)
        where T : unmanaged;

    ReadOnlySpan<T> ReadSpan<T>(int length)
        where T : unmanaged;

    ReadOnlySpan<T> ReadToEnd<T>()
        where T : unmanaged;
}

public unsafe struct BufferReader : IBufferReader
{
    private readonly byte* _buffer;
    private readonly nuint _size;

    private byte* _address;

    public readonly byte* CurrentAddress => _address;

    public nuint Position
    {
        readonly get => (nuint)(_buffer + (_address - _buffer));
        set => _address = _buffer + value;
    }

    public readonly nuint RemainingBytes => (nuint)(_buffer + _size - _address);

    public BufferReader(byte* buffer, nuint size)
    {
        _buffer = buffer;
        _size = size;
        _address = _buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void CheckRange(byte* addr)
    {
        if (addr > _buffer + _size)
        {
            throw new EndOfStreamException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Read<T>()
        where T : unmanaged
    {
        var newAddr = _address + sizeof(T);
        CheckRange(newAddr);

        var value = *(T*)_address;
        _address = newAddr;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void ReadExactly<T>(Span<T> dst)
        where T : unmanaged
    {
        var newAddr = _address + sizeof(T) * dst.Length;
        CheckRange(newAddr);

        var src = new ReadOnlySpan<T>(_address, dst.Length);
        src.CopyTo(dst);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> ReadSpan<T>(int length)
        where T : unmanaged
    {
        var newAddr = _address + sizeof(T) * length;
        CheckRange(newAddr);

        var span = new ReadOnlySpan<T>(_address, length);
        _address = newAddr;
        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> ReadToEnd<T>()
        where T : unmanaged
    {
        var span = new ReadOnlySpan<T>(_address, (int)(_buffer + _size - _address));

        _address += (nuint)(span.Length * sizeof(T));
        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void* ReadBuffer(nuint size)
    {
        var newAddr = _address + size;
        CheckRange(newAddr);

        var p = _address;
        _address = newAddr;
        return p;
    }
}

public readonly struct StreamBufferReader : IBufferReader
{
    private readonly Stream _stream;

    public readonly nuint Position
    {
        get => (nuint)_stream.Position;
        set => _stream.Position = (long)value;
    }

    public readonly nuint RemainingBytes => (nuint)(_stream.Length - _stream.Position);

    public StreamBufferReader(Stream stream)
    {
        _stream = stream;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T Read<T>()
        where T : unmanaged
    {
        return _stream.Read<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void ReadExactly<T>(Span<T> dst)
        where T : unmanaged
    {
        _stream.ReadExactly(MemoryMarshal.AsBytes(dst));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<T> ReadSpan<T>(int length) where T : unmanaged
    {
        var arr = new T[length];
        _stream.ReadExactly(MemoryMarshal.AsBytes(arr.AsSpan()));
        return arr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe ReadOnlySpan<T> ReadToEnd<T>() where T : unmanaged
    {
        var size = (_stream.Length - _stream.Position) / sizeof(T);
        var arr = new T[size];
        _stream.ReadExactly(MemoryMarshal.AsBytes(arr.AsSpan()));
        return arr;
    }
}

public unsafe ref struct SpanReader
{
    private readonly ReadOnlySpan<byte> _buffer;
    private int _position;

    public int Position
    {
        readonly get => _position;
        set => _position = value;
    }

    public readonly int RemainingBytes => _buffer.Length - _position;

    public SpanReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Read<T>()
        where T : unmanaged
    {
        var value = Unsafe.ReadUnaligned<T>(in _buffer[_position]);
        _position += Unsafe.SizeOf<T>();
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> ReadSpan<T>(int length)
        where T : unmanaged
    {
        var size = sizeof(T) * length;
        var span = MemoryMarshal.Cast<byte, T>(_buffer.Slice(_position, size));

        _position += size;
        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> ReadToEnd<T>()
        where T : unmanaged
    {
        var span = MemoryMarshal.Cast<byte, T>(_buffer.Slice(_position));
        _position += span.Length;
        return span;
    }
}
