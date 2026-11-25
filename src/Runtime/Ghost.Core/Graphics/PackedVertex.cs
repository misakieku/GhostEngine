using Misaki.HighPerformance.Mathematics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Core.Graphics;

/// <summary>
/// Compact per-vertex attribute payload (8 bytes: octahedral normal + half2 UV) stored separately from quantized positions.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
public struct PackedAttribute
{
    public uint packedNormal;   // 4 bytes (Octahedral snorm16x2)
    public half2 uv;            // 4 bytes (half2)
}

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
        return (ushort)sx | ((uint)(ushort)sy << 16);
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

    public static bool operator ==(PackedVertex left, PackedVertex right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PackedVertex left, PackedVertex right)
    {
        return !(left == right);
    }

    public const int MAX_POSITION_QUANT_BITS = 21;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ComputePositionQuantStep(float3 boundsMin, float3 boundsMax, int positionBits = MAX_POSITION_QUANT_BITS)
    {
        var clampedBits = Math.Clamp(positionBits, 1, MAX_POSITION_QUANT_BITS);
        var extent = boundsMax - boundsMin;
        var maxExtent = MathF.Max(extent.x, MathF.Max(extent.y, extent.z));
        var maxGrid = (1u << clampedBits) - 1u;
        return maxExtent > 1e-6f ? maxExtent / maxGrid : 1e-6f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void QuantizePosition(float3 position, float3 boundsMin, float quantStep, uint maxGrid, out uint qx, out uint qy, out uint qz)
    {
        var invStep = 1.0f / quantStep;
        var fx = MathF.Round((position.x - boundsMin.x) * invStep);
        var fy = MathF.Round((position.y - boundsMin.y) * invStep);
        var fz = MathF.Round((position.z - boundsMin.z) * invStep);

        qx = (uint)Math.Clamp((long)fx, 0L, maxGrid);
        qy = (uint)Math.Clamp((long)fy, 0L, maxGrid);
        qz = (uint)Math.Clamp((long)fz, 0L, maxGrid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 DequantizePosition(uint qx, uint qy, uint qz, float3 boundsMin, float quantStep)
    {
        return new float3(boundsMin.x + qx * quantStep, boundsMin.y + qy * quantStep, boundsMin.z + qz * quantStep);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BitWidth(uint maxDelta)
    {
        return maxDelta == 0 ? 0 : 32 - BitOperations.LeadingZeroCount(maxDelta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PackQuantOrigin(uint minX, uint minY, uint minZ, out uint packed0, out uint packed1)
    {
        packed0 = (minX & 0x1FFFFFu) | ((minY & 0x7FFu) << 21);
        packed1 = ((minY >> 11) & 0x3FFu) | ((minZ & 0x1FFFFFu) << 10);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint PackMeshletFormat(int bitsX, int bitsY, int bitsZ, bool hasVertexColor, uint attrByteOffset)
    {
        return ((uint)bitsX & 0x1Fu)
            | (((uint)bitsY & 0x1Fu) << 5)
            | (((uint)bitsZ & 0x1Fu) << 10)
            | (hasVertexColor ? (1u << 15) : 0u)
            | (((attrByteOffset >> 2) & 0x7FFFu) << 16);
    }
}
