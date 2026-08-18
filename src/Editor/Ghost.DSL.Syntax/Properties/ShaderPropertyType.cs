namespace Ghost.DSL.Properties;

public enum ShaderPropertyType
{
    Int,
    Int2,
    Int3,
    Int4,
    Uint,
    Uint2,
    Uint3,
    Uint4,
    Float,
    Float2,
    Float3,
    Float4,
    Float2x2,
    Float3x3,
    Float4x4,
    TextureHandle,
    SamplerHandle,
    BufferHandle
}

public static class ShaderPropertyTypeHelper
{
    public static bool TryParse(string name, out ShaderPropertyType type)
    {
        switch (name)
        {
            case "Int":
            case "int":
                type = ShaderPropertyType.Int; return true;
            case "Int2":
            case "int2":
                type = ShaderPropertyType.Int2; return true;
            case "Int3":
            case "int3":
                type = ShaderPropertyType.Int3; return true;
            case "Int4":
            case "int4":
                type = ShaderPropertyType.Int4; return true;
            case "Uint":
            case "uint":
                type = ShaderPropertyType.Uint; return true;
            case "Uint2":
            case "uint2":
                type = ShaderPropertyType.Uint2; return true;
            case "Uint3":
            case "uint3":
                type = ShaderPropertyType.Uint3; return true;
            case "Uint4":
            case "uint4":
                type = ShaderPropertyType.Uint4; return true;
            case "Float":
            case "float":
                type = ShaderPropertyType.Float; return true;
            case "Float2":
            case "float2":
                type = ShaderPropertyType.Float2; return true;
            case "Float3":
            case "float3":
                type = ShaderPropertyType.Float3; return true;
            case "Float4":
            case "float4":
                type = ShaderPropertyType.Float4; return true;
            case "Float2x2":
            case "float2x2":
                type = ShaderPropertyType.Float2x2; return true;
            case "Float3x3":
            case "float3x3":
                type = ShaderPropertyType.Float3x3; return true;
            case "Float4x4":
            case "float4x4":
                type = ShaderPropertyType.Float4x4; return true;
            case "TextureHandle":
            case "Texture2DHandle":
            case "Texture":
                type = ShaderPropertyType.TextureHandle; return true;
            case "SamplerHandle":
            case "Sampler":
                type = ShaderPropertyType.SamplerHandle; return true;
            case "BufferHandle":
            case "Buffer":
                type = ShaderPropertyType.BufferHandle; return true;
            default:
                type = default; return false;
        }
    }

    public static uint GetSize(ShaderPropertyType type)
    {
        return type switch
        {
            ShaderPropertyType.Int => 4,
            ShaderPropertyType.Int2 => 8,
            ShaderPropertyType.Int3 => 12,
            ShaderPropertyType.Int4 => 16,
            ShaderPropertyType.Uint => 4,
            ShaderPropertyType.Uint2 => 8,
            ShaderPropertyType.Uint3 => 12,
            ShaderPropertyType.Uint4 => 16,
            ShaderPropertyType.Float => 4,
            ShaderPropertyType.Float2 => 8,
            ShaderPropertyType.Float3 => 12,
            ShaderPropertyType.Float4 => 16,
            ShaderPropertyType.Float2x2 => 16, // 2 rows of 8 bytes
            ShaderPropertyType.Float3x3 => 48, // 3 rows of 16 bytes (HLSL 16-byte row alignment)
            ShaderPropertyType.Float4x4 => 64, // 4 rows of 16 bytes
            ShaderPropertyType.TextureHandle => 4,
            ShaderPropertyType.SamplerHandle => 4,
            ShaderPropertyType.BufferHandle => 4,
            _ => 4
        };
    }

    public static uint GetAlignment(ShaderPropertyType type)
    {
        return type switch
        {
            ShaderPropertyType.Int => 4,
            ShaderPropertyType.Int2 => 8,
            ShaderPropertyType.Int3 => 16, // 3-component vectors align to 16 bytes in HLSL struct packing
            ShaderPropertyType.Int4 => 16,
            ShaderPropertyType.Uint => 4,
            ShaderPropertyType.Uint2 => 8,
            ShaderPropertyType.Uint3 => 16,
            ShaderPropertyType.Uint4 => 16,
            ShaderPropertyType.Float => 4,
            ShaderPropertyType.Float2 => 8,
            ShaderPropertyType.Float3 => 16,
            ShaderPropertyType.Float4 => 16,
            ShaderPropertyType.Float2x2 => 8,
            ShaderPropertyType.Float3x3 => 16,
            ShaderPropertyType.Float4x4 => 16,
            ShaderPropertyType.TextureHandle => 4,
            ShaderPropertyType.SamplerHandle => 4,
            ShaderPropertyType.BufferHandle => 4,
            _ => 4
        };
    }

    public static string ToCSharpTypeName(ShaderPropertyType type)
    {
        return type switch
        {
            ShaderPropertyType.Int => "int",
            ShaderPropertyType.Int2 => "int2",
            ShaderPropertyType.Int3 => "int3",
            ShaderPropertyType.Int4 => "int4",
            ShaderPropertyType.Uint => "uint",
            ShaderPropertyType.Uint2 => "uint2",
            ShaderPropertyType.Uint3 => "uint3",
            ShaderPropertyType.Uint4 => "uint4",
            ShaderPropertyType.Float => "float",
            ShaderPropertyType.Float2 => "float2",
            ShaderPropertyType.Float3 => "float3",
            ShaderPropertyType.Float4 => "float4",
            ShaderPropertyType.Float2x2 => "float2x2",
            ShaderPropertyType.Float3x3 => "float3x3",
            ShaderPropertyType.Float4x4 => "float4x4",
            ShaderPropertyType.TextureHandle => "uint",
            ShaderPropertyType.SamplerHandle => "uint",
            ShaderPropertyType.BufferHandle => "uint",
            _ => "uint"
        };
    }

    public static string ToHlslTypeName(ShaderPropertyType type)
    {
        return type switch
        {
            ShaderPropertyType.Int => "int",
            ShaderPropertyType.Int2 => "int2",
            ShaderPropertyType.Int3 => "int3",
            ShaderPropertyType.Int4 => "int4",
            ShaderPropertyType.Uint => "uint",
            ShaderPropertyType.Uint2 => "uint2",
            ShaderPropertyType.Uint3 => "uint3",
            ShaderPropertyType.Uint4 => "uint4",
            ShaderPropertyType.Float => "float",
            ShaderPropertyType.Float2 => "float2",
            ShaderPropertyType.Float3 => "float3",
            ShaderPropertyType.Float4 => "float4",
            ShaderPropertyType.Float2x2 => "float2x2",
            ShaderPropertyType.Float3x3 => "float3x3",
            ShaderPropertyType.Float4x4 => "float4x4",
            ShaderPropertyType.TextureHandle => "uint",
            ShaderPropertyType.SamplerHandle => "uint",
            ShaderPropertyType.BufferHandle => "uint",
            _ => "uint"
        };
    }
}
