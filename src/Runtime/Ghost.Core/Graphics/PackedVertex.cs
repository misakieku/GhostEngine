using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Core.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 24)]
public struct PackedVertex : IEquatable<PackedVertex>
{
    public float3 position;     // 12 bytes
    public uint packedNormal;   // 4 bytes (Octahedral snorm16x2)
    public half2 uv;            // 4 bytes (half2)
    public Color32 color;       // 4 bytes (R8G8B8A8_UNORM)

    public PackedVertex(float3 position, uint packedNormal, half2 uv, Color32 color)
    {
        this.position = position;
        this.packedNormal = packedNormal;
        this.uv = uv;
        this.color = color;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint PackNormalOctahedral(float3 n)
    {
        var l1 = MathF.Abs(n.x) + MathF.Abs(n.y) + MathF.Abs(n.z);
        var invL1 = l1 > 1e-6f ? 1.0f / l1 : 0.0f;
        var ox = n.x * invL1;
        var oy = n.y * invL1;
        if (n.z < 0.0f)
        {
            var x = (1.0f - MathF.Abs(oy)) * (ox >= 0.0f ? 1.0f : -1.0f);
            var y = (1.0f - MathF.Abs(ox)) * (oy >= 0.0f ? 1.0f : -1.0f);
            ox = x;
            oy = y;
        }
        var sx = (short)Math.Clamp(MathF.Round(ox * 32767.0f), -32767.0f, 32767.0f);
        var sy = (short)Math.Clamp(MathF.Round(oy * 32767.0f), -32767.0f, 32767.0f);
        return (uint)(ushort)sx | ((uint)(ushort)sy << 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 UnpackNormalOctahedral(uint packed)
    {
        var sx = (short)(packed & 0xFFFF);
        var sy = (short)(packed >> 16);
        var ox = sx * (1.0f / 32767.0f);
        var oy = sy * (1.0f / 32767.0f);
        var oz = 1.0f - MathF.Abs(ox) - MathF.Abs(oy);
        var n = new float3(ox, oy, oz);
        if (oz < 0.0f)
        {
            var x = (1.0f - MathF.Abs(n.y)) * (n.x >= 0.0f ? 1.0f : -1.0f);
            var y = (1.0f - MathF.Abs(n.x)) * (n.y >= 0.0f ? 1.0f : -1.0f);
            n.x = x;
            n.y = y;
        }
        return math.normalize(n);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PackedVertex Pack(in Vertex v)
    {
        return new PackedVertex
        {
            position = v.position,
            packedNormal = PackNormalOctahedral(v.normal),
            uv = new half2((Half)v.uv.x, (Half)v.uv.y),
            color = new Color32(v.color)
        };
    }

    public readonly bool Equals(PackedVertex other)
    {
        return position.Equals(other.position) &&
               packedNormal == other.packedNormal &&
               uv.Equals(other.uv) &&
               color.Equals(other.color);
    }

    public override readonly bool Equals(object? obj) => obj is PackedVertex other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(position, packedNormal, uv, color);
}
