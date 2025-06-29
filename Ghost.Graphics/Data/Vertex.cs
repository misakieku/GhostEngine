using System.Numerics;

namespace Ghost.Graphics.Data;

public struct Vertex(Vector4 position, Vector4 normal, Vector4 tangent, Color32 color, Vector4 uv)
{
    public Vector4 Position
    {
        get;
        set;
    } = position;

    public Vector4 Normal
    {
        get;
        set;
    } = normal;

    public Vector4 Tangent
    {
        get;
        set;
    } = tangent;

    public Color32 Color
    {
        get;
        set;
    } = color;

    public Vector4 UV
    {
        get;
        set;
    } = uv;
}