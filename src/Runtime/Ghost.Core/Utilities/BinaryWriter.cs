using System.Runtime.CompilerServices;

namespace Ghost.Core.Utilities;

public ref struct BinaryWriter
{
    private readonly Span<byte> _buffer;
    private int _position;

    public int Position
    {
        readonly get => _position;
        set => _position = value;
    }

    public BinaryWriter(Span<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    public unsafe void Write<T>(scoped ref readonly T value)
        where T : unmanaged
    {
        Unsafe.WriteUnaligned(ref _buffer[_position], value);
        _position += sizeof(T);
    }

    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        data.CopyTo(_buffer.Slice(_position, data.Length));
        _position += data.Length;
    }

    public Span<byte> GetSpan(int length)
    {
        var span = _buffer.Slice(_position, length);
        _position += length;
        return span;
    }
}
