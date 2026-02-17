namespace Ghost.Core.Graphics;

public enum KeywordSpace
{
    Local,
    Global,
}

public enum ShaderPropertyType
{
    None,
    Float, Float2, Float3, Float4,
    Float4x4,
    Int, Int2, Int3, Int4,
    UInt, UInt2, UInt3, UInt4,
    Bool, Bool2, Bool3, Bool4,
    Texture2D, Texture3D, TextureCube,
    Texture2DArray, TextureCubeArray,
    Sampler
}

public struct ShaderEntryPoint
{
    public string entry;
    public string shader;

    public readonly bool IsCreated => !string.IsNullOrEmpty(entry) && !string.IsNullOrEmpty(shader);
}

public struct KeywordsGroup
{
    public KeywordSpace space;
    public List<string> keywords;
}

public struct PropertyDescriptor
{
    public string name;
    public int offset;
    public int size;
    public ShaderPropertyType type;

    public object? defaultValue;
}

public struct PassDescriptor
{
    public string identifier;
    public string name;

    public ShaderEntryPoint taskShader;
    public ShaderEntryPoint meshShader;
    public ShaderEntryPoint pixelShader;
    public string[] defines;
    public string[] includes;
    public KeywordsGroup[] keywords;
    public PipelineState localPipeline;
    public string? hlsl;
}

public class ShaderDescriptor
{
    public string name = string.Empty;
    public int cbufferSize;
    public PropertyDescriptor[] globalProperties = null!;
    public PropertyDescriptor[] properties = null!;
    public PassDescriptor[] passes = null!;
    public string? hlsl;
}

public static class ShaderDescriptorExtensions
{
    public static int GetSize(this ShaderPropertyType type)
    {
        return type switch
        {
            ShaderPropertyType.Float
            or ShaderPropertyType.Int
            or ShaderPropertyType.UInt
            or ShaderPropertyType.Bool => 4,

            ShaderPropertyType.Float2
            or ShaderPropertyType.Int2
            or ShaderPropertyType.UInt2
            or ShaderPropertyType.Bool2 => 8,

            ShaderPropertyType.Float3
            or ShaderPropertyType.Int3
            or ShaderPropertyType.UInt3
            or ShaderPropertyType.Bool3 => 12,

            ShaderPropertyType.Float4
            or ShaderPropertyType.Int4
            or ShaderPropertyType.UInt4
            or ShaderPropertyType.Bool4 => 16,

            ShaderPropertyType.Float4x4 => 64,

            ShaderPropertyType.Texture2D
            or ShaderPropertyType.Texture3D
            or ShaderPropertyType.TextureCube
            or ShaderPropertyType.Texture2DArray
            or ShaderPropertyType.TextureCubeArray
            or ShaderPropertyType.Sampler => 4, // Bindless resource use uint32
            _ => 0,
        };
    }
}
