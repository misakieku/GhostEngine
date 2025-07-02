using System.Drawing;

namespace Ghost.Graphics.Data;

/// <summary>
/// Represents a color with 32-bit components."/>
/// </summary>
public struct Color32 : IEquatable<Color32>
{
    public byte r;
    public byte g;
    public byte b;
    public byte a;

    public Color32(byte r, byte g, byte b, byte a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public Color32(Color color)
        : this(color.R, color.G, color.B, color.A)
    {
    }

    public Color32(Color128 color128)
        : this((byte)(color128.r * 255.0f), (byte)(color128.g * 255.0f), (byte)(color128.b * 255.0f), (byte)(color128.a * 255.0f))
    {
    }

    public readonly bool Equals(Color32 other)
    {
        return r == other.r && g == other.g && b == other.b && a == other.a;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Color32 color && Equals(color);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(r, g, b, a);
    }

    public static bool operator ==(Color32 left, Color32 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Color32 left, Color32 right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Represents a color with 128-bit components.
/// </summary>
public struct Color128 : IEquatable<Color128>
{
    public float r;
    public float g;
    public float b;
    public float a;

    public Color128(float r, float g, float b, float a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public Color128(Color color)
        : this(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f)
    {
    }

    public Color128(Color32 color32)
        : this(color32.r / 255.0f, color32.g / 255.0f, color32.b / 255.0f, color32.a / 255.0f)
    {
    }

    public readonly bool Equals(Color128 other)
    {
        return r.Equals(other.r) && g.Equals(other.g) && b.Equals(other.b) && a.Equals(other.a);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Color128 color && Equals(color);
    }

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(r, g, b, a);
    }

    public static bool operator ==(Color128 left, Color128 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Color128 left, Color128 right)
    {
        return !(left == right);
    }
}