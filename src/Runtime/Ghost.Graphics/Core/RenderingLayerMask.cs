using System.Diagnostics;

namespace Ghost.Graphics.Core;

public readonly struct RenderingLayerMask : IEquatable<RenderingLayerMask>
{
    private static readonly Dictionary<string, uint> s_layerNameToBit = new(32);
    private static readonly Dictionary<uint, string> s_bitToLayerName = new(32);

    internal static void SetLayerName(int layerIndex, string name)
    {
        Debug.Assert(layerIndex >= 0 && layerIndex < 32, "Layer index must be between 0 and 31.");

        var bit = 1u << layerIndex;
        s_layerNameToBit[name] = bit;
        s_bitToLayerName[bit] = name;
    }

    public static uint GetLayerBit(string name)
    {
        if (s_layerNameToBit.TryGetValue(name, out var bit))
        {
            return bit;
        }

        return 0u;
    }

    private readonly uint _value;

    public static readonly RenderingLayerMask Empty = new(0);
    public static readonly RenderingLayerMask All = new(uint.MaxValue);

    public RenderingLayerMask(uint value)
    {
        _value = value;
    }

    public readonly bool Equals(RenderingLayerMask other)
    {
        return _value == other._value;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is RenderingLayerMask mask && Equals(mask);
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(RenderingLayerMask left, RenderingLayerMask right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RenderingLayerMask left, RenderingLayerMask right)
    {
        return !(left == right);
    }

    public static RenderingLayerMask operator |(RenderingLayerMask left, RenderingLayerMask right)
    {
        return new RenderingLayerMask(left._value | right._value);
    }

    public static RenderingLayerMask operator &(RenderingLayerMask left, RenderingLayerMask right)
    {
        return new RenderingLayerMask(left._value & right._value);
    }

    public static RenderingLayerMask operator ~(RenderingLayerMask mask)
    {
        return new RenderingLayerMask(~mask._value);
    }

    public static RenderingLayerMask operator ^(RenderingLayerMask left, RenderingLayerMask right)
    {
        return new RenderingLayerMask(left._value ^ right._value);
    }

    public static RenderingLayerMask operator <<(RenderingLayerMask mask, int shift)
    {
        return new RenderingLayerMask(mask._value << shift);
    }

    public static RenderingLayerMask operator >>(RenderingLayerMask mask, int shift)
    {
        return new RenderingLayerMask(mask._value >> shift);
    }

    public static implicit operator uint(RenderingLayerMask mask)
    {
        return mask._value;
    }

    public static implicit operator RenderingLayerMask(uint value)
    {
        return new RenderingLayerMask(value);
    }
}
