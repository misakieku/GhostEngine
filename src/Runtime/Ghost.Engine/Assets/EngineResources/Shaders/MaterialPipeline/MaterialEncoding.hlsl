#ifndef GHOST_MATERIAL_ENCODING_HLSL
#define GHOST_MATERIAL_ENCODING_HLSL

#define MATERIAL_CBUFFER_MASK 0x00FFFFFFu
#define MATERIAL_VARIANT_SHIFT 24u
#define MATERIAL_VARIANT_MASK 0xFFu

static inline uint PackMaterial(uint materialBufferIndex, uint variantIndex)
{
    return (materialBufferIndex & MATERIAL_CBUFFER_MASK) | ((variantIndex & MATERIAL_VARIANT_MASK) << MATERIAL_VARIANT_SHIFT);
}

static inline uint UnpackMaterialMaterialBufferIndex(uint packedMaterial)
{
    return packedMaterial & MATERIAL_CBUFFER_MASK;
}

static inline uint UnpackMaterialVariantIndex(uint packedMaterial)
{
    return (packedMaterial >> MATERIAL_VARIANT_SHIFT) & MATERIAL_VARIANT_MASK;
}

#endif // GHOST_MATERIAL_ENCODING_HLSL
