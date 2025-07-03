using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.Data;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex(Vector4 position, Vector4 normal, Vector4 tangent, Color128 color, Vector4 uv)
{
    public unsafe struct Semantic
    {
        public const Format ALIGNED_FORMAT = Format.R32G32B32A32Float;

        private static readonly byte[] s_positionBytes = Encoding.UTF8.GetBytes("POSITION");
        private static readonly byte[] s_normalBytes = Encoding.UTF8.GetBytes("NORMAL");
        private static readonly byte[] s_tangentBytes = Encoding.UTF8.GetBytes("TANGENT");
        private static readonly byte[] s_colorBytes = Encoding.UTF8.GetBytes("COLOR");
        private static readonly byte[] s_uvBytes = Encoding.UTF8.GetBytes("UV");

        public static byte* PositionName => (byte*)Unsafe.AsPointer(ref s_positionBytes[0]);
        public static byte* NormalName => (byte*)Unsafe.AsPointer(ref s_normalBytes[0]);
        public static byte* TangentName => (byte*)Unsafe.AsPointer(ref s_tangentBytes[0]);
        public static byte* ColorName => (byte*)Unsafe.AsPointer(ref s_colorBytes[0]);
        public static byte* UVName => (byte*)Unsafe.AsPointer(ref s_uvBytes[0]);
    }

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

    public Color128 Color
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