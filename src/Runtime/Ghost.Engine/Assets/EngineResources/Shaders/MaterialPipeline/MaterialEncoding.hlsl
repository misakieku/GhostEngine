#ifndef GHOST_MATERIAL_ENCODING_HLSL
#define GHOST_MATERIAL_ENCODING_HLSL

#define MATERIAL_BLOCK_ALIGNMENT_SHIFT 4u
#define MATERIAL_OFFSET_MASK 0x001FFFFFu
#define MATERIAL_CBUFFER_MASK MATERIAL_OFFSET_MASK
#define MATERIAL_SURFACE_TYPE_SHIFT 21u
#define MATERIAL_SURFACE_TYPE_MASK 0x3u
#define MATERIAL_CHROMATIC_SHIFT 23u
#define MATERIAL_CHROMATIC_MASK 0x1u
#define MATERIAL_VARIANT_SHIFT 24u
#define MATERIAL_VARIANT_MASK 0xFFu

#define MATERIAL_SURFACE_OPAQUE 0u
#define MATERIAL_SURFACE_ALPHA_CLIP 1u
#define MATERIAL_SURFACE_TRANSPARENT 2u

uint PackMaterial(uint materialByteOffset, uint variantIndex, uint surfaceType, uint chromaticTransmittance)
{
    uint blockIndex = materialByteOffset >> MATERIAL_BLOCK_ALIGNMENT_SHIFT;
    return (blockIndex & MATERIAL_OFFSET_MASK)
         | ((surfaceType & MATERIAL_SURFACE_TYPE_MASK) << MATERIAL_SURFACE_TYPE_SHIFT)
         | ((chromaticTransmittance & MATERIAL_CHROMATIC_MASK) << MATERIAL_CHROMATIC_SHIFT)
         | ((variantIndex & MATERIAL_VARIANT_MASK) << MATERIAL_VARIANT_SHIFT);
}

uint UnpackMaterialByteOffset(uint packedMaterial)
{
    return (packedMaterial & MATERIAL_OFFSET_MASK) << MATERIAL_BLOCK_ALIGNMENT_SHIFT;
}

uint UnpackMaterialMaterialBufferIndex(uint packedMaterial)
{
    return UnpackMaterialByteOffset(packedMaterial);
}

uint UnpackMaterialSurfaceType(uint packedMaterial)
{
    return (packedMaterial >> MATERIAL_SURFACE_TYPE_SHIFT) & MATERIAL_SURFACE_TYPE_MASK;
}

uint UnpackMaterialHasAlpha(uint packedMaterial)
{
    return UnpackMaterialSurfaceType(packedMaterial) == MATERIAL_SURFACE_ALPHA_CLIP ? 1u : 0u;
}

uint UnpackMaterialIsTransparent(uint packedMaterial)
{
    return UnpackMaterialSurfaceType(packedMaterial) == MATERIAL_SURFACE_TRANSPARENT ? 1u : 0u;
}

uint UnpackMaterialChromaticTransmittance(uint packedMaterial)
{
    return (packedMaterial >> MATERIAL_CHROMATIC_SHIFT) & MATERIAL_CHROMATIC_MASK;
}

uint UnpackMaterialVariantIndex(uint packedMaterial)
{
    return (packedMaterial >> MATERIAL_VARIANT_SHIFT) & MATERIAL_VARIANT_MASK;
}

#endif // GHOST_MATERIAL_ENCODING_HLSL
