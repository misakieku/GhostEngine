using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using Ghost.Graphics.Core;

namespace Ghost.Graphics.Core;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public static class Semantic
    {
        public const DXGI_FORMAT ALIGNED_FORMAT = DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT;
        public const int COUNT = 5;

        public static readonly FixedText32 position = new("POSITION");
        public static readonly FixedText32 normal = new("NORMAL");
        public static readonly FixedText32 tangent = new("TANGENT");
        public static readonly FixedText32 uv = new("TEXCOORD");
        public static readonly FixedText32 color = new("COLOR");
    }

    public float4 position;
    public float4 normal;
    public float4 tangent;
    public float4 uv;
    public Color128 color;
}
