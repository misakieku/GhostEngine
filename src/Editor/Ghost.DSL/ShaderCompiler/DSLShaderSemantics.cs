using Ghost.Core.Graphics;

namespace Ghost.DSL.ShaderCompiler;

public enum PropertyScope
{
    Global,
    Local,
}

public class PipelineSemantic
{
    public ZTest? zTest;
    public ZWrite? zWrite;
    public Cull? cull;
    public Blend? blend;
    public ColorWriteMask? colorMask;
}

public class PassSemantic
{
    public string name = string.Empty;
    public ShaderEntryPoint taskShader;
    public ShaderEntryPoint meshShader;
    public ShaderEntryPoint pixelShader;
    public string? hlsl;
    public List<string>? defines;
    public List<string>? includes;
    public List<KeywordsGroup>? keywords;
    public PipelineSemantic? localPipeline;
}

public class DSLShaderSemantics
{
    public string name = string.Empty;
    public ShaderModel shaderModel;
    public PipelineSemantic? pipeline;
    public List<PassSemantic>? passes;
}

public class DSLComputeShaderSemantics
{
    public string name = string.Empty;
    public string? hlsl;
    public ShaderModel shaderModel;
    public List<string>? defines;
    public List<string>? includes;
    public List<KeywordsGroup>? keywords;
    public List<ShaderEntryPoint>? entryPoints;
}