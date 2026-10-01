#ifndef GHOST_MATERIAL_ENCODING_HLSL
#define GHOST_MATERIAL_ENCODING_HLSL

#define MATERIAL_OFFSET_MASK 0x007FFFFFu
#define MATERIAL_CBUFFER_MASK MATERIAL_OFFSET_MASK
#define MATERIAL_ALPHA_CLIP_SHIFT 23u
#define MATERIAL_ALPHA_CLIP_MASK 0x1u
#define MATERIAL_VARIANT_SHIFT 24u
#define MATERIAL_VARIANT_MASK 0xFFu

uint PackMaterial(uint materialByteOffset, uint variantIndex, uint hasAlphaClip)
{
    return (materialByteOffset & MATERIAL_OFFSET_MASK)
         | ((hasAlphaClip & MATERIAL_ALPHA_CLIP_MASK) << MATERIAL_ALPHA_CLIP_SHIFT)
         | ((variantIndex & MATERIAL_VARIANT_MASK) << MATERIAL_VARIANT_SHIFT);
}

uint UnpackMaterialByteOffset(uint packedMaterial)
{
    return packedMaterial & MATERIAL_OFFSET_MASK;
}

uint UnpackMaterialMaterialBufferIndex(uint packedMaterial)
{
    return packedMaterial & MATERIAL_OFFSET_MASK;
}

uint UnpackMaterialHasAlpha(uint packedMaterial)
{
    return (packedMaterial >> MATERIAL_ALPHA_CLIP_SHIFT) & MATERIAL_ALPHA_CLIP_MASK;
}

uint UnpackMaterialVariantIndex(uint packedMaterial)
{
    return (packedMaterial >> MATERIAL_VARIANT_SHIFT) & MATERIAL_VARIANT_MASK;
}

#endif // GHOST_MATERIAL_ENCODING_HLSL
