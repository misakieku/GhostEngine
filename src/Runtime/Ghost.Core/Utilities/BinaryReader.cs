using System.Runtime.CompilerServices;

namespace Ghost.Core.Utilities;

public ref struct BinaryReader
{
    private readonly Span<byte> _buffer;
    private int _position;

    public int Position
    {
        readonly get => _position;
        set => _position = value;
    }

    public BinaryReader(Span<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    public T Read<T>()
        where T : unmanaged
    {
        var value = Unsafe.ReadUnaligned<T>(ref _buffer[_position]);
        _position += Unsafe.SizeOf<T>();
        return value;
    }

    public ReadOnlySpan<byte> ReadBytes(int length)
    {
        var span = _buffer.Slice(_position, length);
        _position += length;
        return span;
    }
}
