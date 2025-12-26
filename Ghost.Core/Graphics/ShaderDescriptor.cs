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
    public List<string>? keywords;
}

public interface IPassDescriptor
{
    public string Identifier
    {
        get;
    }

    public string Name
    {
        get;
    }
}

public struct PropertyDescriptor
{
    public ShaderPropertyType type;
    public string name;
    public object? defaultValue;
}

public class FullPassDescriptor : IPassDescriptor
{
    public string uniqueIdentifier = string.Empty;
    public string name = string.Empty;

    public ShaderEntryPoint taskShader;
    public ShaderEntryPoint meshShader;
    public ShaderEntryPoint pixelShader;
    public List<string>? defines;
    public List<KeywordsGroup>? keywords;
    public PipelineState localPipeline;

    public string Identifier => uniqueIdentifier;
    public string Name => name;
}

public class FallbackPassDescriptor : IPassDescriptor
{
    public string fallbackPassIdentifier = string.Empty;
    public string name = string.Empty;

    public string Identifier => fallbackPassIdentifier;
    public string Name => name;
}

public class ShaderDescriptor
{
    public string name = string.Empty;
    public string? generatedCodePath;
    public uint cbufferSize;
    public List<PropertyDescriptor> globalProperties = new();
    public List<PropertyDescriptor> properties = new();
    public List<IPassDescriptor> passes = new();
}

public static class ShaderDescriptorExtensions
{
    public static uint GetSize(this ShaderPropertyType type)
    {
        return type switch
        {
            ShaderPropertyType.Float => 4,
            ShaderPropertyType.Float2 => 8,
            ShaderPropertyType.Float3 => 12,
            ShaderPropertyType.Float4 => 16,
            ShaderPropertyType.Float4x4 => 64,
            ShaderPropertyType.Int => 4,
            ShaderPropertyType.Int2 => 8,
            ShaderPropertyType.Int3 => 12,
            ShaderPropertyType.Int4 => 16,
            ShaderPropertyType.UInt => 4,
            ShaderPropertyType.UInt2 => 8,
            ShaderPropertyType.UInt3 => 12,
            ShaderPropertyType.UInt4 => 16,
            ShaderPropertyType.Bool => 4,
            ShaderPropertyType.Bool2 => 8,
            ShaderPropertyType.Bool3 => 12,
            ShaderPropertyType.Bool4 => 16,
            ShaderPropertyType.Texture2D => 4, // Bindless resource use uint32
            ShaderPropertyType.Texture3D => 4,
            ShaderPropertyType.TextureCube => 4,
            ShaderPropertyType.Texture2DArray => 4,
            ShaderPropertyType.TextureCubeArray => 4,
            ShaderPropertyType.Sampler => 4,
            _ => 0,
        };
    }
}
