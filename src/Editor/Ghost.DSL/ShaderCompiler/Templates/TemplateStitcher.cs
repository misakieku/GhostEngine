using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DSL.Models;
using System.Text;

namespace Ghost.DSL.ShaderCompiler.Templates;

/// <summary>
/// Stitches a template-based shader (semantics + user code) into a full
/// <see cref="GraphicsShaderDescriptor"/> containing every pass and stage
/// the template defines. Template HLSL files are embedded into the
/// Ghost.DSL assembly, so stitching works from any output directory.
/// </summary>
public static class TemplateStitcher
{
    private const string ResourcePrefix = "Ghost.DSL.Templates.";

    /// <summary>
    /// Loads an embedded template file by its template-relative path (e.g. "Unlit/Unlit_Forward.template.hlsl").
    /// </summary>
    internal static Result<string> LoadTemplateSource(string templateFile)
    {
        var resourceName = ResourcePrefix + templateFile.Replace('/', '.').Replace('\\', '.');

        var assembly = typeof(TemplateStitcher).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return Result.Failure($"Embedded template resource not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Builds the flat HLSL properties struct: template base properties
    /// followed by user properties.
    /// </summary>
    internal static string BuildPropertiesStruct(IShaderTemplate template, GraphicsShaderSemantics semantics)
    {
        var structName = SanitizeToIdentifier(semantics.name);
        var sb = new StringBuilder();
        sb.AppendLine($"struct {structName}");
        sb.AppendLine("{");

        // Merge base + custom, deduplicating by name.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in template.BaseProperties)
        {
            if (seen.Add(prop.name))
            {
                AppendProperty(sb, prop.type, prop.name);
            }
        }

        foreach (var prop in semantics.properties)
        {
            if (seen.Add(prop.name))
            {
                AppendProperty(sb, prop.type, prop.name);
            }
        }

        sb.AppendLine("};");
        sb.AppendLine();
        sb.AppendLine($"typedef {structName} MaterialProperties;");
        sb.AppendLine($"typedef {structName} {template.Name}ShaderProperties;");

        static void AppendProperty(StringBuilder builder, string type, string name)
        {
            var hlslType = type.Trim().ToLowerInvariant() switch
            {
                "texture2d" or "texture3d" or "texturecube" or "texture2darray" or "texturecubearray"
                    or "samplerstate" or "sampler" or "byte_address_buffer" or "structured_buffer"
                    => "uint",
                _ => type
            };

            builder.AppendLine($"    {hlslType} {name};");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds the payload struct block. Empty payload when the user did not declare one.
    /// </summary>
    internal static string BuildPayloadStruct(GraphicsShaderSemantics semantics)
    {
        var sb = new StringBuilder();

        sb.AppendLine("struct Payload");
        sb.AppendLine("{");

        if (!string.IsNullOrWhiteSpace(semantics.payload))
        {
            sb.AppendLine(semantics.payload!.Trim());
        }
        else
        {
            sb.AppendLine("    uint _unused;");
        }

        sb.AppendLine("};");
        return sb.ToString();
    }

    /// <summary>
    /// Detects whether the user HLSL block overrides an injection point function,
    /// emitting the suppression define for its fallback.
    /// </summary>
    internal static List<string> CollectOverrideDefines(string? userHlsl)
    {
        var defines = new List<string>();

        if (string.IsNullOrEmpty(userHlsl))
        {
            return defines;
        }

        if (userHlsl.Contains("GetAlphaCoverage", StringComparison.Ordinal))
        {
            defines.Add("GHOST_OVERRIDE_GET_ALPHA_COVERAGE");
        }

        if (userHlsl.Contains("GetColor", StringComparison.Ordinal))
        {
            defines.Add("GHOST_OVERRIDE_GET_COLOR");
        }

        if (userHlsl.Contains("GetSurfaceData", StringComparison.Ordinal))
        {
            defines.Add("GHOST_OVERRIDE_GET_SURFACE_DATA");
        }

        if (userHlsl.Contains("EvaluateBSDF", StringComparison.Ordinal))
        {
            defines.Add("GHOST_OVERRIDE_EVALUATE_BSDF");
        }

        return defines;
    }

    /// <summary>
    /// Stitches one template file into a complete translation unit for a stage.
    /// </summary>
    private static Result<string> StitchStage(
        IShaderTemplate template,
        GraphicsShaderSemantics semantics,
        ShaderReflectionData reflectionData,
        IReadOnlyDictionary<string, string> virtualShaders,
        string templateFile)
    {
        var templateResult = LoadTemplateSource(templateFile);
        if (templateResult.IsFailure)
        {
            return Result.Failure(templateResult.Message);
        }

        var commonResult = LoadTemplateSource(template.CommonTemplateFile);
        if (commonResult.IsFailure)
        {
            return Result.Failure(commonResult.Message);
        }

        var commonFileName = Path.GetFileName(template.CommonTemplateFile);
        var stitchedCommon = commonResult.Value
            .Replace("$GHOST_PROPERTIES_STRUCT$", BuildPropertiesStruct(template, semantics))
            .Replace("$GHOST_PAYLOAD_STRUCT$", BuildPayloadStruct(semantics))
            .Replace("$GHOST_USER_HLSL$", semantics.hlsl ?? string.Empty);

        var final = templateResult.Value
            .Replace($"#include \"{template.CommonTemplateFile}\"", stitchedCommon)
            .Replace($"#include \"{commonFileName}\"", stitchedCommon);

        var sb = new StringBuilder();

        // Injection-point override suppressors must precede all code.
        foreach (var define in CollectOverrideDefines(semantics.hlsl))
        {
            sb.AppendLine($"#define {define} 1");
        }

        foreach (var includePath in semantics.includes ?? new List<string>())
        {
            var relativePath = includePath.TrimStart('/', '\\');
            if (virtualShaders.TryGetValue("/" + relativePath, out var code) ||
                virtualShaders.TryGetValue(relativePath, out code) ||
                virtualShaders.TryGetValue(includePath, out code))
            {
                sb.AppendLine(code);
            }
            else
            {
                sb.AppendLine($"#include \"{relativePath}\"");
            }
        }

        if (!string.IsNullOrEmpty(reflectionData.Code))
        {
            sb.AppendLine("#line 0 \"properties\"");
            sb.AppendLine(reflectionData.Code);
        }

        sb.AppendLine(final);

        return sb.ToString();
    }

    /// <summary>
    /// Resolves a template-based shader into a complete multi-pass descriptor.
    /// </summary>
    public static Result<GraphicsShaderDescriptor> ResolveShader(
        IShaderTemplate template,
        GraphicsShaderSemantics semantics,
        ShaderReflectionData reflectionData,
        IReadOnlyDictionary<string, string> virtualShaders)
    {
        var overrideDefines = CollectOverrideDefines(semantics.hlsl);
        var hasAlphaClip = overrideDefines.Any(d => d.Contains("GHOST_OVERRIDE_GET_ALPHA_COVERAGE", StringComparison.Ordinal));

        var passes = new PassDescriptor[template.Passes.Count];

        for (var i = 0; i < passes.Length; i++)
        {
            var passDef = template.Passes[i];
            var defines = new List<string>(template.Defines);
            defines.AddRange(overrideDefines);
            if (hasAlphaClip)
            {
                defines.Add("GHOST_HAS_ALPHA_CLIP");
            }

            var pass = new PassDescriptor
            {
                name = passDef.name,
                semantic = passDef.semantic,
                localPipeline = DSLShaderCompiler.MergePipeline(semantics.pipeline, passDef.pipeline.ToPipelineState()),
                defines = defines.ToArray(),
            };

            foreach (var stageDef in passDef.stages)
            {
                var result = StitchStage(template, semantics, reflectionData, virtualShaders, stageDef.templateFile);
                if (result.IsFailure)
                {
                    return Result.Failure($"Failed to stitch stage '{stageDef.entryPoint}' of pass '{passDef.name}': {result.Message}");
                }

                var shaderCode = new ShaderCode { code = result.Value, entryPoint = stageDef.entryPoint };
                pass.stageMask |= stageDef.stage switch
                {
                    ShaderStage.AmplificationShader => ShaderStageMask.Amplification,
                    ShaderStage.MeshShader => ShaderStageMask.Mesh,
                    ShaderStage.PixelShader => ShaderStageMask.Pixel,
                    ShaderStage.ComputeShader => ShaderStageMask.Compute,
                    _ => ShaderStageMask.None,
                };

                switch (stageDef.stage)
                {
                    case ShaderStage.AmplificationShader:
                        pass.amplificationShaderCode = shaderCode;
                        break;
                    case ShaderStage.MeshShader:
                        pass.meshShaderCode = shaderCode;
                        break;
                    case ShaderStage.PixelShader:
                        pass.pixelShaderCode = shaderCode;
                        break;
                    case ShaderStage.ComputeShader:
                        pass.computeShaderCode = shaderCode;
                        break;
                    default:
                        return Result.Failure($"Unsupported template stage '{stageDef.stage}' in pass '{passDef.name}'.");
                }
            }

            if (!pass.computeShaderCode.IsCreated && (!pass.meshShaderCode.IsCreated || !pass.pixelShaderCode.IsCreated))
            {
                return Result.Failure($"Template pass '{passDef.name}' is missing required shader stage code.");
            }

            passes[i] = pass;
        }

        var descriptor = new GraphicsShaderDescriptor
        {
            Name = semantics.name,
            PropertyBufferSize = reflectionData.Size,
            ShaderModel = semantics.shaderModel,
            Passes = passes
        };

        for (var i = 0; i < descriptor.Passes.Length; i++)
        {
            descriptor.Passes[i].shader = descriptor;
        }

        return descriptor;
    }

    public static string SanitizeToIdentifier(string shaderName)
    {
        var parts = shaderName.Split(new[] { '/', '\\', '.', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length > 0)
            {
                sb.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                {
                    sb.Append(part.Substring(1));
                }
            }
        }

        var result = sb.ToString();
        if (!result.EndsWith("ShaderProperties", StringComparison.OrdinalIgnoreCase) &&
            !result.EndsWith("Properties", StringComparison.OrdinalIgnoreCase))
        {
            result += "ShaderProperties";
        }

        return result;
    }
}

file static class PipelineSemanticExtensions
{
    public static PipelineState ToPipelineState(this PipelineSemantic semantic)
    {
        return new PipelineState
        {
            ZTest = semantic.zTest ?? ZTest.LessEqual,
            ZWrite = semantic.zWrite ?? ZWrite.On,
            Cull = semantic.cull ?? Cull.Back,
            Blend = semantic.blend ?? Blend.Opaque,
            ColorMask = semantic.colorMask ?? ColorWriteMask.All
        };
    }
}
