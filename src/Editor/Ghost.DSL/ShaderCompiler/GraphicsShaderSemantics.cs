using Ghost.Core.Graphics;

namespace Ghost.DSL.ShaderCompiler;

public enum PropertyScope
{
    Global,
    Local,
}

public struct ShaderEntryPoint
{
    public string? entry;
    public string? shaderPath;

    public readonly bool IsCreated => !string.IsNullOrEmpty(entry) && !string.IsNullOrEmpty(shaderPath);
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
    public ShaderEntryPoint amplificationShader;
    public ShaderEntryPoint meshShader;
    public ShaderEntryPoint pixelShader;
    public string? hlsl;
    public List<string>? defines;
    public List<string>? includes;
    public List<KeywordsGroup>? keywords;
    public PipelineSemantic? localPipeline;
}

public class GraphicsShaderSemantics
{
    public string name = string.Empty;
    public ShaderModel shaderModel;
    public PipelineSemantic? pipeline;
    public List<PassSemantic>? passes;
}

public class ComputeShaderSemantics
{
    public string name = string.Empty;
    public string? hlsl;
    public ShaderModel shaderModel;
    public List<string>? defines;
    public List<string>? includes;
    public List<KeywordsGroup>? keywords;
    public List<ShaderEntryPoint> entryPoints = null!;
}