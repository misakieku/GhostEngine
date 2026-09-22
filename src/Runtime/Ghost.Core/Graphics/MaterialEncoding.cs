using System.Runtime.CompilerServices;

namespace Ghost.Core.Graphics;

/// <summary>
/// Bit-packing helpers for the 32-bit material entry in the material index buffer.
/// </summary>
public static class MaterialEncoding
{
    public const uint CBUFFER_INDEX_MASK = 0x00FFFFFFu;
    public const int VARIANT_INDEX_SHIFT = 24;
    public const uint VARIANT_INDEX_MASK = 0xFFu;

    /// <summary>
    /// Packs a bindless constant buffer index (24 bits) and a shader variant index (8 bits).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Encode(uint materialBufferIndex, uint variantIndex)
    {
        return (materialBufferIndex & CBUFFER_INDEX_MASK) | ((variantIndex & VARIANT_INDEX_MASK) << VARIANT_INDEX_SHIFT);
    }

    /// <summary>
    /// Extracts the bindless constant buffer index from the packed material entry.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetMaterialBufferIndex(uint packedMaterial) => packedMaterial & CBUFFER_INDEX_MASK;

    /// <summary>
    /// Extracts the dense shader variant index from the packed material entry.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetVariantIndex(uint packedMaterial) => (packedMaterial >> VARIANT_INDEX_SHIFT) & VARIANT_INDEX_MASK;
}
