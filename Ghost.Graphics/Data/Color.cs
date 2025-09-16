using System.Drawing;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Data;

/// <summary>
/// Represents a color with 4 bytes components.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct Color4 : IEquatable<Color4>
{
    public byte r;
    public byte g;
    public byte b;
    public byte a;

    public Color4(byte r, byte g, byte b, byte a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public Color4(Color color)
        : this(color.R, color.G, color.B, color.A)
    {
    }

    public Color4(Color16 color128)
        : this((byte)(color128.r * 255.0f), (byte)(color128.g * 255.0f), (byte)(color128.b * 255.0f), (byte)(color128.a * 255.0f))
    {
    }

    public readonly bool Equals(Color4 other)
    {
        return r == other.r && g == other.g && b == other.b && a == other.a;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Color4 color && Equals(color);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(r, g, b, a);
    }

    public static bool operator ==(Color4 left, Color4 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Color4 left, Color4 right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Represents a color with 16 bytes components.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct Color16 : IEquatable<Color16>
{
    public float r;
    public float g;
    public float b;
    public float a;

    public Color16(float r, float g, float b, float a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public Color16(Color color)
        : this(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f)
    {
    }

    public Color16(Color4 color32)
        : this(color32.r / 255.0f, color32.g / 255.0f, color32.b / 255.0f, color32.a / 255.0f)
    {
    }

    public readonly bool Equals(Color16 other)
    {
        return r.Equals(other.r) && g.Equals(other.g) && b.Equals(other.b) && a.Equals(other.a);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Color16 color && Equals(color);
    }

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(r, g, b, a);
    }

    public static bool operator ==(Color16 left, Color16 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Color16 left, Color16 right)
    {
        return !(left == right);
    }
}