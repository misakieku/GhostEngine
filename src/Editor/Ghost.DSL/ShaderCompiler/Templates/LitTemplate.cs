using Ghost.Core;
using Ghost.Core.Graphics;

namespace Ghost.DSL.ShaderCompiler.Templates;

/// <summary>
/// TODO: This Lit template is a placeholder for testing and framework validation only.
/// As the GPU-driven rendering pipeline (V-Buffer, compute deferred texturing, G-Buffer layout,
/// clustered lighting) continues to evolve, this template will be fully expanded.
/// </summary>
public sealed class LitTemplate : IShaderTemplate
{
    public const string TemplateName = "Lit";

    public string Name => TemplateName;

    public string CommonTemplateFile => "Lit/Lit_Common.template.hlsl";

    private static readonly TemplatePropertyDef[] s_baseProperties = new[]
    {
        new TemplatePropertyDef("float4", "baseColor", "float4(1, 1, 1, 1)"),
        new TemplatePropertyDef("uint", "baseMap", "0"),
        new TemplatePropertyDef("uint", "sampler_baseMap", "0"),
        new TemplatePropertyDef("uint", "normalMap", "0"),
        new TemplatePropertyDef("uint", "sampler_normalMap", "0"),
        new TemplatePropertyDef("float", "metallic", "0.0"),
        new TemplatePropertyDef("float", "roughness", "0.5"),
        new TemplatePropertyDef("float", "occlusion", "1.0"),
    };

    public IReadOnlyList<TemplatePropertyDef> BaseProperties => s_baseProperties;

    private static readonly List<TemplatePassDef> s_passes = new()
    {
        new TemplatePassDef
        {
            name = "Forward",
            pipeline = new PipelineSemantic
            {
                zTest = ZTest.LessEqual,
                zWrite = ZWrite.On,
                cull = Cull.Back,
                blend = Blend.Opaque,
                colorMask = ColorWriteMask.All
            },
            stages = new List<TemplateStage>
            {
                new() { templateFile = "Lit/Lit_Forward.template.hlsl", entryPoint = "ASMain", stage = ShaderStage.AmplificationShader },
                new() { templateFile = "Lit/Lit_Forward.template.hlsl", entryPoint = "MSMain", stage = ShaderStage.MeshShader },
                new() { templateFile = "Lit/Lit_Forward.template.hlsl", entryPoint = "PSMain", stage = ShaderStage.PixelShader },
            }
        },
        new TemplatePassDef
        {
            name = "Visibility",
            pipeline = new PipelineSemantic
            {
                zTest = ZTest.LessEqual,
                zWrite = ZWrite.On,
                cull = Cull.Back,
                blend = Blend.Opaque,
                colorMask = ColorWriteMask.None
            },
            stages = new List<TemplateStage>
            {
                new() { templateFile = "Lit/Lit_Visibility.template.hlsl", entryPoint = "ASMain", stage = ShaderStage.AmplificationShader },
                new() { templateFile = "Lit/Lit_Visibility.template.hlsl", entryPoint = "MSMain", stage = ShaderStage.MeshShader },
                new() { templateFile = "Lit/Lit_Visibility.template.hlsl", entryPoint = "PSMain", stage = ShaderStage.PixelShader },
            }
        },
        new TemplatePassDef
        {
            name = "Shadow",
            pipeline = new PipelineSemantic
            {
                zTest = ZTest.LessEqual,
                zWrite = ZWrite.On,
                cull = Cull.Back,
                blend = Blend.Opaque,
                colorMask = ColorWriteMask.None
            },
            stages = new List<TemplateStage>
            {
                new() { templateFile = "Lit/Lit_Shadow.template.hlsl", entryPoint = "ASMain", stage = ShaderStage.AmplificationShader },
                new() { templateFile = "Lit/Lit_Shadow.template.hlsl", entryPoint = "MSMain", stage = ShaderStage.MeshShader },
                new() { templateFile = "Lit/Lit_Shadow.template.hlsl", entryPoint = "PSMain", stage = ShaderStage.PixelShader },
            }
        },
        new TemplatePassDef
        {
            name = "DeferredTexturing",
            pipeline = new PipelineSemantic(),
            stages = new List<TemplateStage>
            {
                new() { templateFile = "Lit/Lit_DeferredTexturing.template.hlsl", entryPoint = "CSMain", stage = ShaderStage.ComputeShader },
            }
        },
    };

    public IReadOnlyList<TemplatePassDef> Passes => s_passes;

    public IReadOnlyList<string> Defines => Array.Empty<string>();
}
