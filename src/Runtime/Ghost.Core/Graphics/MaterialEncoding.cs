namespace Ghost.Core.Graphics;

/// <summary>
/// Bit-packing helpers for the 32-bit material entry in the material index buffer.
/// </summary>
public static class MaterialEncoding
{
    public const uint OFFSET_MASK = 0x007FFFFFu;
    public const int ALPHA_CLIP_SHIFT = 23;
    public const uint ALPHA_CLIP_MASK = 0x1u;
    public const int VARIANT_INDEX_SHIFT = 24;
    public const uint VARIANT_INDEX_MASK = 0xFFu;

    /// <summary>
    /// Legacy mask alias for backward compatibility.
    /// </summary>
    public const uint CBUFFER_INDEX_MASK = OFFSET_MASK;

    /// <summary>
    /// Packs a material buffer byte offset (23 bits), alpha clip flag (1 bit), and a shader variant index (8 bits).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Encode(uint materialByteOffset, uint variantIndex, bool hasAlphaClip = false)
    {
        return (materialByteOffset & OFFSET_MASK)
             | ((hasAlphaClip ? 1u : 0u) << ALPHA_CLIP_SHIFT)
             | ((variantIndex & VARIANT_INDEX_MASK) << VARIANT_INDEX_SHIFT);
    }

    /// <summary>
    /// Extracts the material buffer byte offset from the packed material entry.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetMaterialByteOffset(uint packedMaterial) => packedMaterial & OFFSET_MASK;

    /// <summary>
    /// Backwards-compatible alias for GetMaterialByteOffset.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetMaterialBufferIndex(uint packedMaterial) => packedMaterial & OFFSET_MASK;

    /// <summary>
    /// Extracts the alpha clip flag from the packed material entry.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetHasAlphaClip(uint packedMaterial) => ((packedMaterial >> ALPHA_CLIP_SHIFT) & ALPHA_CLIP_MASK) != 0;

    /// <summary>
    /// Extracts the dense shader variant index from the packed material entry.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetVariantIndex(uint packedMaterial) => (packedMaterial >> VARIANT_INDEX_SHIFT) & VARIANT_INDEX_MASK;
}
