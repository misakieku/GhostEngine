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
        new TemplatePropertyDef("bool", "alphaClip", "false"),
        new TemplatePropertyDef("float", "alphaClipThreshold", "0.5"),
    };

    public IReadOnlyList<TemplatePropertyDef> BaseProperties => s_baseProperties;

    private static readonly List<TemplatePassDef> s_passes = new()
    {
        new TemplatePassDef
        {
            name = "Visibility",
            semantic = PassSemantic.Visibility,
            pipeline = new PipelineSemantic
            {
                zTest = ZTest.Disabled,
                zWrite = ZWrite.Off,
                cull = Cull.Back,
                blend = Blend.Opaque,
                colorMask = ColorWriteMask.None
            },
            stages = new List<TemplateStage>
            {
                new() { templateFile = "Common/Visibility.template.hlsl", entryPoint = "MSMain", stage = ShaderStage.MeshShader },
                new() { templateFile = "Common/Visibility.template.hlsl", entryPoint = "PSMain", stage = ShaderStage.PixelShader },
            }
        },
        new TemplatePassDef
        {
            name = "DeferredTexturing",
            semantic = PassSemantic.DeferredTexturing,
            pipeline = new PipelineSemantic
            {
                zTest = ZTest.Disabled,
                zWrite = ZWrite.Off,
                cull = Cull.Back,
                blend = Blend.Opaque,
                colorMask = ColorWriteMask.All
            },
            stages = new List<TemplateStage>
            {
                new() { templateFile = "Lit/Lit_DeferredTexturing.template.hlsl", entryPoint = "CSMain", stage = ShaderStage.ComputeShader }
            }
        }
    };

    public IReadOnlyList<TemplatePassDef> Passes => s_passes;

    private static readonly string[] s_defines = Array.Empty<string>();
    public IReadOnlyList<string> Defines => s_defines;

    private static readonly TemplateOverridePoint[] s_overridePoints = new[]
    {
        new TemplateOverridePoint("GetAlphaCoverage", "GHOST_OVERRIDE_GET_ALPHA_COVERAGE", IsAlphaClip: true),
        new TemplateOverridePoint("GetSurfaceData", "GHOST_OVERRIDE_GET_SURFACE_DATA"),
        new TemplateOverridePoint("EvaluateBSDF", "GHOST_OVERRIDE_EVALUATE_BSDF"),
    };

    public IReadOnlyList<TemplateOverridePoint> OverridePoints => s_overridePoints;
}
