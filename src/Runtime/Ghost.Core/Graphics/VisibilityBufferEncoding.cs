using System.Runtime.CompilerServices;

namespace Ghost.Core.Graphics;

/// <summary>
/// Bit-packing helpers for the 64-bit atomic visibility buffer.
/// Upper 32 bits: IEEE 754 float depth as uint.
/// Lower 32 bits: InstanceIndex (24 bits) | PrimitiveID (8 bits).
/// </summary>
public static class VisibilityBufferEncoding
{
    public const int DEPTH_SHIFT = 32;
    public const int PRIMITIVE_ID_SHIFT = 24;
    public const uint INSTANCE_INDEX_MASK = 0x00FFFFFFu;
    public const uint PRIMITIVE_ID_MASK = 0xFFu;

    /// <summary>
    /// Packs instance index and primitive ID into a 32-bit payload.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint PackPayload(uint instanceIndex, uint primitiveId)
    {
        return (instanceIndex & INSTANCE_INDEX_MASK) | ((primitiveId & PRIMITIVE_ID_MASK) << PRIMITIVE_ID_SHIFT);
    }

    /// <summary>
    /// Packs float depth and 32-bit payload into a 64-bit unsigned integer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Pack64(float depth, uint instanceIndex, uint primitiveId)
    {
        var depthInt = (ulong)BitConverter.SingleToUInt32Bits(depth);
        var payload = (ulong)PackPayload(instanceIndex, primitiveId);
        return (depthInt << DEPTH_SHIFT) | payload;
    }

    /// <summary>
    /// Unpacks float depth and payload components from a 64-bit visibility buffer entry.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Unpack64(ulong packed64, out float depth, out uint instanceIndex, out uint primitiveId)
    {
        depth = BitConverter.UInt32BitsToSingle((uint)(packed64 >> DEPTH_SHIFT));
        var payload = (uint)(packed64 & 0xFFFFFFFFu);
        instanceIndex = payload & INSTANCE_INDEX_MASK;
        primitiveId = (payload >> PRIMITIVE_ID_SHIFT) & PRIMITIVE_ID_MASK;
    }
}
