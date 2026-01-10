using Ghost.Core.Graphics;

namespace Ghost.DSL.ShaderCompiler;

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
    public ZTest? zTest;
    public ZWrite? zWrite;
    public Cull? cull;
    public Blend? blend;
    public ColorWriteMask? colorMask;
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
    public PipelineSemantic? localPipeline;
}

internal class DSLShaderSemantics
{
    public string name = string.Empty;
    public string fallback = string.Empty;
    public List<PropertySemantic>? properties;
    public PipelineSemantic? pipeline;
    public List<PassSemantic>? passes;
}