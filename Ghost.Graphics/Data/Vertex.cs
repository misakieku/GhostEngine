using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.Data;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public unsafe struct Semantic
    {
        public const Format ALIGNED_FORMAT = Format.R32G32B32A32Float;

        private static readonly byte[] s_positionBytes = Encoding.UTF8.GetBytes("POSITION");
        private static readonly byte[] s_normalBytes = Encoding.UTF8.GetBytes("NORMAL");
        private static readonly byte[] s_tangentBytes = Encoding.UTF8.GetBytes("TANGENT");
        private static readonly byte[] s_colorBytes = Encoding.UTF8.GetBytes("COLOR");
        private static readonly byte[] s_uvBytes = Encoding.UTF8.GetBytes("TEXCOORD");

        public static byte* pPositionName => (byte*)Unsafe.AsPointer(ref s_positionBytes[0]);
        public static byte* pNormalName => (byte*)Unsafe.AsPointer(ref s_normalBytes[0]);
        public static byte* pTangentName => (byte*)Unsafe.AsPointer(ref s_tangentBytes[0]);
        public static byte* pColorName => (byte*)Unsafe.AsPointer(ref s_colorBytes[0]);
        public static byte* pUVName => (byte*)Unsafe.AsPointer(ref s_uvBytes[0]);
    }

    public float4 Position
    {
        get;
        set;
    }

    public float4 Normal
    {
        get;
        set;
    }

    public float4 Tangent
    {
        get;
        set;
    }

    public Color16 Color
    {
        get;
        set;
    }

    public float4 UV
    {
        get;
        set;
    }

    public Vertex(float4 position, float4 normal, float4 tangent, Color16 color, float4 uv)
    {
        Position = position;
        Normal = normal;
        Tangent = tangent;
        Color = color;
        UV = uv;
    }
}