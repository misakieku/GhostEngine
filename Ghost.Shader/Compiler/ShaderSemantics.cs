using Ghost.Core.Graphics;

namespace Ghost.Shader.Compiler;

public enum PropertyScope
{
    Global,
    Local,
}

internal class PropertySemantic
{
    public PropertyScope scope;
    public ShaderPropertyType type;
    public string name = string.Empty;
    public object? defaultValue;
}

internal class PipelineSemantic
{
    public ZTestOptions? zTest;
    public ZWriteOptions? zWrite;
    public CullOptions? cull;
    public BlendOptions? blend;
    public uint? colorMask;
}

internal class PassSemantic
{
    public string name = string.Empty;
    public ShaderEntryPoint taskShader;
    public ShaderEntryPoint meshShader;
    public ShaderEntryPoint pixelShader;
    public List<string>? defines;
    public List<string>? includes;
    public List<KeywordsGroup>? keywords;
    public List<PropertySemantic>? localProperties;
    public PipelineSemantic? localPipeline;
}

internal class ShaderSemantics
{
    public string name = string.Empty;
    public string fallback = string.Empty;
    public List<PropertySemantic>? properties;
    public PipelineSemantic? pipeline;
    public List<PassSemantic>? passes;
}