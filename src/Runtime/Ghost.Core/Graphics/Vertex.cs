using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Core.Graphics;

[StructLayout(LayoutKind.Sequential)]
public record struct Vertex
{
    public static class Semantic
    {
        public const int COUNT = 5;

        public static readonly FixedText32 Position = new("POSITION"u8);
        public static readonly FixedText32 Normal = new("NORMAL"u8);
        public static readonly FixedText32 Tangent = new("TANGENT"u8);
        public static readonly FixedText32 Uv = new("TEXCOORD"u8);
        public static readonly FixedText32 Color = new("COLOR"u8);
    }

    public Color128 color;
    public float4 tangent;
    public float3 position;
    public float3 normal;
    public float2 uv;
}
