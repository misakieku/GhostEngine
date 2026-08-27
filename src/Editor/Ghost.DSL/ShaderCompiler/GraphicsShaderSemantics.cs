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

public class PropertySemantic
{
    public string type = string.Empty;
    public string name = string.Empty;
    public string? defaultValue;
}

public class ShaderPassSemantic
{
    public string name = string.Empty;
    public ShaderEntryPoint amplificationShader;
    public ShaderEntryPoint meshShader;
    public ShaderEntryPoint pixelShader;
    public string? hlsl;
    public List<string>? defines;
    public List<string>? includes;
    public PipelineSemantic? localPipeline;
}

public class GraphicsShaderSemantics
{
    public string name = string.Empty;
    public string? templateName;
    public string? payload;
    public string? hlsl;
    public List<PropertySemantic> properties = new List<PropertySemantic>();
    public List<string> includes = new List<string>();
    public ShaderModel shaderModel;
    public PipelineSemantic? pipeline;
    public List<ShaderPassSemantic> passes = new List<ShaderPassSemantic>();
}

public class ComputeShaderSemantics
{
    public string name = string.Empty;
    public string? hlsl;
    public ShaderModel shaderModel;
    public List<string> defines = new List<string>();
    public List<string> includes = new List<string>();
    public List<ShaderEntryPoint> entryPoints = new List<ShaderEntryPoint>();
}