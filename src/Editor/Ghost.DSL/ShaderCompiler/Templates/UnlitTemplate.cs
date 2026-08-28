using Ghost.Core;
using Ghost.Core.Graphics;

namespace Ghost.DSL.ShaderCompiler.Templates;

/// <summary>
/// The built-in Unlit template. Emits unlit (emissive-only) surface programs:
/// forward pass for translucent/emissive geometry, plus visibility and
/// shadow depth passes for GPU-driven occlusion.
///
/// Injection points the user may override in their hlsl block:
///   float  GetAlphaCoverage(uint materialBindlessIndex, float2 uv, inout Payload payload)
///   float4 GetColor(inout Payload payload)
/// </summary>
public sealed class UnlitTemplate : IShaderTemplate
{
    public const string TemplateName = "Unlit";

    public string Name => TemplateName;

    public string CommonTemplateFile => "Unlit/Unlit_Common.template.hlsl";

    private static readonly TemplatePropertyDef[] s_baseProperties = new[]
    {
        new TemplatePropertyDef("float4", "baseColor", "float4(1, 1, 1, 1)"),
        new TemplatePropertyDef("uint", "baseMap", "0"),
        new TemplatePropertyDef("uint", "sampler_baseMap", "0"),
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
                new() { templateFile = "Unlit/Unlit_Forward.template.hlsl", entryPoint = "ASMain", stage = ShaderStage.AmplificationShader },
                new() { templateFile = "Unlit/Unlit_Forward.template.hlsl", entryPoint = "MSMain", stage = ShaderStage.MeshShader },
                new() { templateFile = "Unlit/Unlit_Forward.template.hlsl", entryPoint = "PSMain", stage = ShaderStage.PixelShader },
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
                new() { templateFile = "Unlit/Unlit_Visibility.template.hlsl", entryPoint = "ASMain", stage = ShaderStage.AmplificationShader },
                new() { templateFile = "Unlit/Unlit_Visibility.template.hlsl", entryPoint = "MSMain", stage = ShaderStage.MeshShader },
                new() { templateFile = "Unlit/Unlit_Visibility.template.hlsl", entryPoint = "PSMain", stage = ShaderStage.PixelShader },
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
                new() { templateFile = "Unlit/Unlit_Shadow.template.hlsl", entryPoint = "ASMain", stage = ShaderStage.AmplificationShader },
                new() { templateFile = "Unlit/Unlit_Shadow.template.hlsl", entryPoint = "MSMain", stage = ShaderStage.MeshShader },
                new() { templateFile = "Unlit/Unlit_Shadow.template.hlsl", entryPoint = "PSMain", stage = ShaderStage.PixelShader },
            }
        },
    };

    public IReadOnlyList<TemplatePassDef> Passes => s_passes;

    public IReadOnlyList<string> Defines => Array.Empty<string>();
}

/// <summary>
/// Registry of built-in templates. User-defined templates are not allowed:
/// all templates ship with the engine so it knows exactly how to render each shader.
/// </summary>
public static class TemplateRegistry
{
    private static readonly Dictionary<string, IShaderTemplate> s_templates = new(StringComparer.Ordinal)
    {
        [UnlitTemplate.TemplateName] = new UnlitTemplate(),
        [LitTemplate.TemplateName] = new LitTemplate(),
    };
    public static IReadOnlyCollection<IShaderTemplate> Templates => s_templates.Values;

    public static Result<IShaderTemplate> GetTemplate(string name)
    {
        if (s_templates.TryGetValue(name, out var template))
        {
            return Result<IShaderTemplate>.Success(template);
        }

        return Result<IShaderTemplate>.Failure($"Unknown shader template '{name}'. Built-in templates: {string.Join(", ", s_templates.Keys)}");
    }

    public static bool HasTemplate(string name)
    {
        return s_templates.ContainsKey(name);
    }
}
