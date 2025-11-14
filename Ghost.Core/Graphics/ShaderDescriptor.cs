namespace Ghost.Core.Graphics;

public enum KeywordType
{
    Static,
    Dynamic,
}

public enum ShaderPropertyType
{
    None,
    Float, Float2, Float3, Float4,
    Int, Int2, Int3, Int4,
    UInt, UInt2, UInt3, UInt4,
    Bool, Bool2, Bool3, Bool4,
    Texture2DBindless, Texture3DBindless, TextureCubeBindless,
    Texture2DArrayBindless, TextureCubeArrayBindless,
}

public struct ShaderEntryPoint
{
    public string entry;
    public string shader;

    public readonly bool IsCreated => !string.IsNullOrEmpty(entry) && !string.IsNullOrEmpty(shader);
}

public struct KeywordsGroup
{
    public KeywordType type;
    public List<string>? keywords;
}

public struct PipelineDescriptor
{
    public ZTestOptions zTest;
    public ZWriteOptions zWrite;
    public CullOptions cull;
    public BlendOptions blend;
    public uint colorMask;

    public static PipelineDescriptor Default = new PipelineDescriptor
    {
        zTest = ZTestOptions.LessEqual,
        zWrite = ZWriteOptions.On,
        cull = CullOptions.Back,
        blend = BlendOptions.Opaque,
        colorMask = 0
    };
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
    public string? generatedCodePath;
    public List<string>? defines;
    public List<string>? includes;
    public List<KeywordsGroup>? keywords;
    public List<PropertyDescriptor>? properties;
    public PipelineDescriptor localPipeline;

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
    public List<PropertyDescriptor> globalProperties = new();
    public List<IPassDescriptor> passes = new();
}