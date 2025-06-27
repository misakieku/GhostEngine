using System.Drawing;

namespace Ghost.Graphics.Data;

/// <summary>
/// Represents a color with 32-bit components, the unmanaged version of <see cref="Color"/>."/>
/// </summary>
public struct Color32
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
}