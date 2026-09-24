using Ghost.Core;
using Ghost.Core.Graphics;

namespace Ghost.DSL.ShaderCompiler.Templates;

internal sealed class UnlitTemplate : IShaderTemplate
{
    public const string TEMPLATE_NAME = "Unlit";

    public string Name => TEMPLATE_NAME;

    public string CommonTemplateFile => "Unlit/Unlit_Common.template.hlsl";

    private static readonly TemplatePropertyDef[] s_baseProperties = new[]
    {
        new TemplatePropertyDef("bool", "alphaClip", "false"),
        new TemplatePropertyDef("float", "alphaClipThreshold", "0.5"),
        // mirror: 1.0f, 1.0f, -1.0f, 1.0f, flip: -1.0f, -1.0f, -1.0f, 1.0f. w is used to determine if the material is double-sided or not. If w is 0, the material is double-sided.
        new TemplatePropertyDef("float4", "doubleSidedConstants", "float4(1.0, 1.0, 1.0, 0.0)"),
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
                new() { templateFile = "Unlit/Unlit_DeferTexturing.template.hlsl", entryPoint = "CSMain", stage = ShaderStage.ComputeShader },
            }
        }
    };

    public IReadOnlyList<TemplatePassDef> Passes => s_passes;

    private static readonly string[] s_defines = Array.Empty<string>();
    public IReadOnlyList<string> Defines => s_defines;

    private static readonly TemplateOverridePoint[] s_overridePoints = new[]
    {
        new TemplateOverridePoint("GetAlphaCoverage", "GHOST_OVERRIDE_GET_ALPHA_COVERAGE", IsAlphaClip: true),
        new TemplateOverridePoint("GetColor", "GHOST_OVERRIDE_GET_COLOR"),
    };

    public IReadOnlyList<TemplateOverridePoint> OverridePoints => s_overridePoints;
}

/// <summary>
/// Registry of built-in templates. User-defined templates are not allowed:
/// all templates ship with the engine so it knows exactly how to render each shader.
/// </summary>
public static class TemplateRegistry
{
    private static readonly Dictionary<string, IShaderTemplate> s_templates = new(StringComparer.Ordinal)
    {
        [UnlitTemplate.TEMPLATE_NAME] = new UnlitTemplate(),
        [LitTemplate.TEMPLATE_NAME] = new LitTemplate(),
    };

    public static Result<IShaderTemplate> GetTemplate(string name)
    {
        if (s_templates.TryGetValue(name, out var template))
        {
            return Result.Success(template);
        }

        return Result.Failure<IShaderTemplate>($"Unknown shader template '{name}'.");
    }

    public static bool HasTemplate(string name)
    {
        return s_templates.ContainsKey(name);
    }
}
