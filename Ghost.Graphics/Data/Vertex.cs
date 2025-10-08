using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;

namespace Ghost.Graphics.Data;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public unsafe static class Semantic
    {
        public const DXGI_FORMAT ALIGNED_FORMAT = DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT;
        public const int COUNT = 5;

        public static readonly FixedString32 position = new("POSITION");
        public static readonly FixedString32 normal = new("NORMAL");
        public static readonly FixedString32 tangent = new("TANGENT");
        public static readonly FixedString32 uv = new("TEXCOORD");
        public static readonly FixedString32 color = new("COLOR");
    }

    public float4 position;
    public float4 normal;
    public float4 tangent;
    public float4 uv;
    public Color128 color;
}