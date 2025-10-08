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
    Texture2D, Texture3D, TextureCube,
    Texture2DArray, TextureCubeArray,
}

public struct ShaderEntryPoint
{
    public string entry;
    public string shader;
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
}

public class FullPassDescriptor : IPassDescriptor
{
    public string uniqueIdentifier = string.Empty;
    public ShaderEntryPoint vertexShader;
    public ShaderEntryPoint pixelShader;
    public List<string>? defines;
    public List<string>? includes;
    public List<KeywordsGroup>? keywords;
    public List<PropertyDescriptor>? properties;
    public PipelineDescriptor localPipeline;

    public string Identifier => uniqueIdentifier;
}

public class FallbackPassDescriptor : IPassDescriptor
{
    public string fallbackPassIdentifier = string.Empty;

    public string Identifier => fallbackPassIdentifier;
}

public struct PropertyDescriptor
{
    public ShaderPropertyType type;
    public string name;
    public object? defaultValue;
}

public class ShaderDescriptor
{
    public string name = string.Empty;
    public List<PropertyDescriptor> globalProperties = new();
    public List<IPassDescriptor> passes = new();
}