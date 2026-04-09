namespace Ghost.Core.Graphics;

public enum ShaderModel
{
    Invalid,
    SM_6_6,
    SM_6_7,
    SM_6_8
}

public enum KeywordSpace
{
    Local,
    Global,
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
    public GraphicsShaderDescriptor shader;

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

public class GraphicsShaderDescriptor
{
    public string name = string.Empty;
    public string propertiesCode = string.Empty;
    public uint propertyBufferSize;
    public ShaderModel shaderModel;
    public PassDescriptor[] passes = Array.Empty<PassDescriptor>();
}

public class ComputeShaderDescriptor
{
    public ulong identifier;
    public string name = string.Empty;
    public string propertiesCode = string.Empty;
    public uint propertyBufferSize;
    public string? hlsl;
    public ShaderModel shaderModel;
    public string[] defines = Array.Empty<string>();
    public string[] includes = Array.Empty<string>();
    public KeywordsGroup[] keywords = Array.Empty<KeywordsGroup>();
    public ShaderEntryPoint[] entryPoints = Array.Empty<ShaderEntryPoint>();
}
