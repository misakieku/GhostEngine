using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DSL.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace Ghost.DSL.ShaderCompiler.Templates;

/// <summary>
/// Stitches a template-based shader (semantics + user code) into a full
/// <see cref="GraphicsShaderDescriptor"/> containing every pass and stage
/// the template defines. Template HLSL files are embedded into the
/// Ghost.DSL assembly, so stitching works from any output directory.
/// </summary>
public static class TemplateStitcher
{
    private const string RESOURCE_PREFIX = "Ghost.DSL.Templates.";
    private static readonly char[] s_separator = new[] { '/', '\\', '.', ' ', '-' };

    /// <summary>
    /// Loads an embedded template file by its template-relative path (e.g. "Unlit/Unlit_Forward.template.hlsl").
    /// </summary>
    public static Result<string> LoadTemplateSource(string templateFile)
    {
        var resourceName = RESOURCE_PREFIX + templateFile.Replace('/', '.').Replace('\\', '.');

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
    /// Stitches one template file into a complete translation unit for a stage.
    /// </summary>
    private static readonly Regex s_shadingModelIdRegex = new(
        @"\bShadingModelID\s*=\s*(\d+)u?\b",
        RegexOptions.Compiled);

    private static uint ExtractShadingModelId(string shadingModelFile, IReadOnlyDictionary<string, string> virtualShaders)
    {
        var relativePath = shadingModelFile.TrimStart('/', '\\');
        string? content = null;

        if (virtualShaders.TryGetValue("/" + relativePath, out var code) ||
            virtualShaders.TryGetValue(relativePath, out code) ||
            virtualShaders.TryGetValue(shadingModelFile, out code))
        {
            content = code;
        }
        else if (File.Exists(shadingModelFile))
        {
            content = File.ReadAllText(shadingModelFile);
        }
        else
        {
            var candidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), relativePath),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "Runtime", "Ghost.Engine", "Assets", relativePath),
                Path.Combine(AppContext.BaseDirectory, relativePath),
                Path.Combine(AppContext.BaseDirectory, "Assets", relativePath),
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    content = File.ReadAllText(candidate);
                    break;
                }
            }

            if (content == null)
            {
                var fileName = Path.GetFileName(shadingModelFile);
                var matches = Directory.GetFiles(Directory.GetCurrentDirectory(), fileName, SearchOption.AllDirectories);
                if (matches.Length > 0)
                {
                    content = File.ReadAllText(matches[0]);
                }
            }
        }

        if (!string.IsNullOrEmpty(content))
        {
            var match = s_shadingModelIdRegex.Match(content);
            if (match.Success && uint.TryParse(match.Groups[1].Value, out var parsed))
            {
                return parsed;
            }
        }

        return 1u;
    }

    /// <summary>
    /// Stitches one template file into a complete translation unit for a stage.
    /// </summary>
    private static Result<string> StitchStage(IShaderTemplate template, GraphicsShaderSemantics semantics, ShaderReflectionData reflectionData, IReadOnlyDictionary<string, string> virtualShaders, string templateFile, PassSemantic passSemantic)
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
        var hasCustomVBuffer = semantics.hlsl != null && (semantics.hlsl.Contains("VBUFFER_STRATEGY") || semantics.hlsl.Contains("GHOST_PASS_VISIBILITY"));
        var needsProperties = passSemantic != PassSemantic.DeferredLighting &&
            (passSemantic != PassSemantic.Visibility || hasCustomVBuffer);
        var propertiesStruct = needsProperties
            ? BuildPropertiesStruct(template, semantics)
            : string.Empty;

        var userHlsl = semantics.hlsl ?? string.Empty;
        if (passSemantic == PassSemantic.Visibility && !hasCustomVBuffer)
        {
            userHlsl = string.Empty;
        }
        else if (passSemantic == PassSemantic.DeferredLighting && (semantics.hlsl == null || (!semantics.hlsl.Contains("DEFERREDLIGHTING_STRATEGY") && !semantics.hlsl.Contains("GHOST_PASS_DEFERREDLIGHTING"))))
        {
            userHlsl = string.Empty;
        }

        var stitchedCommon = commonResult.Value
            .Replace("$GHOST_PROPERTIES_STRUCT$", propertiesStruct)
            .Replace("$GHOST_USER_HLSL$", userHlsl);

        var final = templateResult.Value
            .Replace($"#include \"{template.CommonTemplateFile}\"", stitchedCommon)
            .Replace($"#include \"{commonFileName}\"", stitchedCommon);

        var sb = new StringBuilder();

        // Template-level defines (e.g. GHOST_TEMPLATE_LIT / GHOST_TEMPLATE_UNLIT).
        foreach (var define in template.Defines)
        {
            sb.AppendLine($"#define {define} 1");
        }

        sb.AppendLine($"#define SHADING_MODEL_ID {semantics.shadingModelId}u");

        // Automatically include the shading model file if declared via shading_model(...) only for DeferredLighting
        if (passSemantic == PassSemantic.DeferredLighting && !string.IsNullOrEmpty(semantics.shadingModelFile))
        {
            var modelRel = semantics.shadingModelFile.TrimStart('/', '\\');
            if (virtualShaders.TryGetValue("/" + modelRel, out var modelCode) ||
                virtualShaders.TryGetValue(modelRel, out modelCode) ||
                virtualShaders.TryGetValue(semantics.shadingModelFile, out modelCode))
            {
                sb.AppendLine(modelCode);
            }
            else
            {
                sb.AppendLine($"#include \"{modelRel}\"");
            }
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

        if (needsProperties && !string.IsNullOrEmpty(reflectionData.Code))
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
    public static Result<GraphicsShaderDescriptor> ResolveShader(IShaderTemplate template, GraphicsShaderSemantics semantics, ShaderReflectionData reflectionData, IReadOnlyDictionary<string, string> virtualShaders)
    {
        uint shadingModelId = 0u;
        if (!string.IsNullOrEmpty(semantics.shadingModelFile))
        {
            shadingModelId = ExtractShadingModelId(semantics.shadingModelFile, virtualShaders);
        }
        else if (!string.IsNullOrEmpty(semantics.hlsl))
        {
            var match = s_shadingModelIdRegex.Match(semantics.hlsl);
            if (match.Success && uint.TryParse(match.Groups[1].Value, out var parsed))
            {
                shadingModelId = parsed;
            }
        }

        semantics.shadingModelId = shadingModelId;

        var passes = new PassDescriptor[template.Passes.Count];

        for (var i = 0; i < passes.Length; i++)
        {
            var passDef = template.Passes[i];
            var defines = new List<string>(template.Defines);

            defines.Add($"GHOST_TEMPLATE_{template.Name.ToUpperInvariant()}");
            defines.Add($"GHOST_PASS_{passDef.name.ToUpperInvariant()}");
            defines.Add($"SHADING_MODEL_ID={shadingModelId}u");

            var pass = new PassDescriptor
            {
                name = passDef.name,
                semantic = passDef.semantic,
                localPipeline = DSLShaderCompiler.MergePipeline(semantics.pipeline, passDef.pipeline.ToPipelineState()),
                defines = defines.ToArray(),
                shadingModelId = shadingModelId,
            };

            foreach (var stageDef in passDef.stages)
            {
                var result = StitchStage(template, semantics, reflectionData, virtualShaders, stageDef.templateFile, passDef.semantic);
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

        var seenProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allProperties = new List<PropertySemantic>();
        foreach (var prop in template.BaseProperties)
        {
            if (seenProperties.Add(prop.name))
            {
                allProperties.Add(new PropertySemantic
                {
                    type = prop.type,
                    name = prop.name,
                    defaultValue = prop.defaultValue
                });
            }
        }
        foreach (var prop in semantics.properties)
        {
            if (seenProperties.Add(prop.name))
            {
                allProperties.Add(prop);
            }
        }
        var propertyBufferSize = DSLShaderCompiler.CalculatePropertyBufferSize(allProperties);
        if (propertyBufferSize == 0 && reflectionData != null && reflectionData.Size > 0)
        {
            propertyBufferSize = reflectionData.Size;
        }

        var descriptor = new GraphicsShaderDescriptor
        {
            Name = semantics.name,
            PropertyBufferSize = propertyBufferSize,
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
        var parts = shaderName.Split(s_separator, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length > 0)
            {
                sb.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                {
                    sb.Append(part.AsSpan(1));
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
