using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Helpers;

namespace Ghost.Graphics.Shading;

public enum ShaderPropertyType
{
    Float,
    Float2,
    Float3,
    Float4,
    Color,
    Matrix,
    Texture2D,
    Texture3D
}

public struct ShaderProperty : IDisposable
{
    private UnsafeArray<byte> _value;
    private FixedString128 _name;
    private readonly uint _valueOffset;

    internal readonly uint Offset => _valueOffset;

    public readonly string Name => _name.Value;
    public readonly ReadOnlySpan<byte> Value => _value.AsSpan();

    public ShaderPropertyType PropertyType
    {
        get;
    }

    public ShaderProperty(Span<byte> value, uint offset, string name, ShaderPropertyType type)
    {
        _value = new(value.Length, Allocator.Persistent);
        _valueOffset = offset;
        _name = new(name);
        PropertyType = type;
    }

    public void Dispose()
    {
        _value.Dispose();
        _name.Dispose();
    }
}