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

public struct PassDescriptor
{
    public ShaderDescriptor shader;

    public ulong identifier;
    public string name;

    public string? hlsl;
    public ShaderEntryPoint taskShader;
    public ShaderEntryPoint meshShader;
    public ShaderEntryPoint pixelShader;
    public string[] defines;
    public string[] includes;
    public KeywordsGroup[] keywords;
    public PipelineState localPipeline;
}

public class ShaderDescriptor
{
    public string name = string.Empty;
    public string propertiesCode = string.Empty;
    public uint propertyBufferSize;
    public PassDescriptor[] passes = Array.Empty<PassDescriptor>();
}

public class ComputeShaderDescriptor
{
    public string name = string.Empty;
    public string propertiesCode = string.Empty;
    public uint propertyBufferSize;
    public ShaderEntryPoint entryPoint;
    public string[] defines = Array.Empty<string>();
    public string[] includes = Array.Empty<string>();
    public KeywordsGroup[] keywords = Array.Empty<KeywordsGroup>();
}
