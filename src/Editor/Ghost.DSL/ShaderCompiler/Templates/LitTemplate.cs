using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Generator.Templates;

namespace Ghost.DSL.ShaderCompiler.Templates;

internal sealed class LitTemplate : IShaderTemplate
{
    public const string TEMPLATE_NAME = "Lit";

    public string Name => TEMPLATE_NAME;

    public string CommonTemplateFile => "Lit/Lit_Common.template.hlsl";

    public IReadOnlyList<TemplatePropertyDef> BaseProperties => BuiltInTemplateProperties.Lit;

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
