using System.Runtime.CompilerServices;

namespace Ghost.Core.Graphics;

/// <summary>
/// Bit-packing helpers for the 32-bit material entry in the material index buffer.
/// </summary>
public static class MaterialEncoding
{
    public const int BLOCK_ALIGNMENT_SHIFT = 4;
    public const uint OFFSET_MASK = 0x001FFFFFu;
    public const int SURFACE_TYPE_SHIFT = 21;
    public const uint SURFACE_TYPE_MASK = 0x3u;
    public const int CHROMATIC_SHIFT = 23;
    public const uint CHROMATIC_MASK = 0x1u;
    public const int VARIANT_INDEX_SHIFT = 24;
    public const uint VARIANT_INDEX_MASK = 0xFFu;

    /// <summary>
    /// Legacy mask alias for backward compatibility.
    /// </summary>
    public const uint CBUFFER_INDEX_MASK = OFFSET_MASK;

    /// <summary>
    /// Legacy alpha-clip shift alias for backward compatibility.
    /// </summary>
    public const int ALPHA_CLIP_SHIFT = SURFACE_TYPE_SHIFT;

    /// <summary>
    /// Legacy alpha-clip mask alias for backward compatibility.
    /// </summary>
    public const uint ALPHA_CLIP_MASK = SURFACE_TYPE_MASK;

    /// <summary>
    /// Packs a 16-byte-aligned material buffer offset (21 bits), surface type (2 bits), chromatic transmittance flag (1 bit), and shader variant index (8 bits).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Encode(uint materialByteOffset, uint variantIndex, MaterialSurfaceType surfaceType = MaterialSurfaceType.Opaque, bool chromaticTransmittance = false)
    {
        Logger.DebugAssert((materialByteOffset & ((1u << BLOCK_ALIGNMENT_SHIFT) - 1u)) == 0u, "Material byte offset must be 16-byte aligned.");
        var blockIndex = materialByteOffset >> BLOCK_ALIGNMENT_SHIFT;
        Logger.DebugAssert(blockIndex <= OFFSET_MASK, "Material pool offset exceeds 32 MB MaterialEncoding capacity.");

        return (blockIndex & OFFSET_MASK)
             | (((uint)surfaceType & SURFACE_TYPE_MASK) << SURFACE_TYPE_SHIFT)
             | ((chromaticTransmittance ? 1u : 0u) << CHROMATIC_SHIFT)
             | ((variantIndex & VARIANT_INDEX_MASK) << VARIANT_INDEX_SHIFT);
    }

    /// <summary>
    /// Packs a material buffer byte offset, alpha clip flag, and shader variant index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Encode(uint materialByteOffset, uint variantIndex, bool hasAlphaClip)
    {
        return Encode(materialByteOffset, variantIndex, hasAlphaClip ? MaterialSurfaceType.AlphaClip : MaterialSurfaceType.Opaque, false);
    }

    /// <summary>
    /// Extracts the material buffer byte offset from the packed material entry.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetMaterialByteOffset(uint packedMaterial) => (packedMaterial & OFFSET_MASK) << BLOCK_ALIGNMENT_SHIFT;

    /// <summary>
    /// Backwards-compatible alias for GetMaterialByteOffset.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetMaterialBufferIndex(uint packedMaterial) => GetMaterialByteOffset(packedMaterial);

    /// <summary>
    /// Extracts the surface type from the packed material entry.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MaterialSurfaceType GetSurfaceType(uint packedMaterial) => (MaterialSurfaceType)((packedMaterial >> SURFACE_TYPE_SHIFT) & SURFACE_TYPE_MASK);

    /// <summary>
    /// Extracts whether the packed material uses alpha clipping.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetHasAlphaClip(uint packedMaterial) => GetSurfaceType(packedMaterial) == MaterialSurfaceType.AlphaClip;

    /// <summary>
    /// Extracts whether the packed material is transparent.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetIsTransparent(uint packedMaterial) => GetSurfaceType(packedMaterial) == MaterialSurfaceType.Transparent;

    /// <summary>
    /// Extracts whether the packed material uses chromatic (RGB) transmittance in AVBOIT.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetChromaticTransmittance(uint packedMaterial) => ((packedMaterial >> CHROMATIC_SHIFT) & CHROMATIC_MASK) != 0;

    /// <summary>
    /// Extracts the dense shader variant index from the packed material entry.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetVariantIndex(uint packedMaterial) => (packedMaterial >> VARIANT_INDEX_SHIFT) & VARIANT_INDEX_MASK;
}
