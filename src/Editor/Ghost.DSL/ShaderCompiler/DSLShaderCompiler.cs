using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DSL.Models;
using Ghost.DSL.ShaderCompiler.Templates;
using Ghost.DSL.ShaderParser;
using Ghost.DSL.ShaderParser.Syntax;
using Misaki.HighPerformance.Utilities;
using System.Text;

namespace Ghost.DSL.ShaderCompiler;

public struct DSLShaderError
{
    public string message;
    public int line;
    public int column;

    public override readonly string ToString()
    {
        return $"Error at {line}:{column} - {message}";
    }
}

public static class DSLShaderCompiler
{
    internal static PipelineState MergePipeline(PipelineSemantic? semantic, PipelineState parent)
    {
        if (semantic == null)
        {
            return parent;
        }

        return new PipelineState
        {
            ZTest = semantic.zTest ?? parent.ZTest,
            ZWrite = semantic.zWrite ?? parent.ZWrite,
            Cull = semantic.cull ?? parent.Cull,
            Blend = semantic.blend ?? parent.Blend,
            ColorMask = semantic.colorMask ?? parent.ColorMask
        };
    }

    private static Result<string> BuildFinalShaderCode(string? shaderPath, ReadOnlySpan<string> includes, string? injectedCode, string? properties, IReadOnlyDictionary<string, string> virtualShaders, string? mainShaderPath)
    {
        if (string.IsNullOrEmpty(shaderPath))
        {
            return string.Empty;
        }

        string shaderCode;
        if (shaderPath == "hlsl_block")
        {
            if (string.IsNullOrEmpty(injectedCode))
            {
                return Result.Failure("Shader code is empty. Either provide a valid shader path or inject shader code directly.");
            }

            shaderCode = string.Empty;
        }
        else
        {
            if (!File.Exists(shaderPath))
            {
                return Result.Failure("Shader file not found: " + shaderPath);
            }

            shaderCode = File.ReadAllText(shaderPath);
        }

        var sb = new StringBuilder();
        foreach (var includePath in includes)
        {
            var relativePath = includePath.TrimStart('/', '\\');
            var absolutePath = "/" + relativePath;

            if (virtualShaders.TryGetValue(absolutePath, out var code) ||
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

        mainShaderPath = mainShaderPath?.Replace('\\', '/').TrimStart('/') ?? "main_shader";

        if (!string.IsNullOrEmpty(properties))
        {
            sb.AppendLine($"#line 0 \"{mainShaderPath}_properties\"");
            sb.AppendLine(properties);
        }

        if (!string.IsNullOrEmpty(injectedCode))
        {
            sb.AppendLine($"#line 0 \"{mainShaderPath}_injected_code\"");
            sb.AppendLine(injectedCode);
        }

        if (!string.IsNullOrEmpty(shaderCode))
        {
            sb.AppendLine($"#line 0 \"{shaderPath}\"");
            sb.AppendLine(shaderCode);
        }

        return sb.ToString();
    }
    public static uint GetPropertyTypeSize(string type)
    {
        return type.Trim().ToLowerInvariant() switch
        {
            "float" or "int" or "uint" or "bool" => 4u,
            "float2" or "int2" or "uint2" or "bool2" => 8u,
            "float3" or "int3" or "uint3" or "bool3" => 12u,
            "float4" or "int4" or "uint4" or "bool4" or "quaternion" => 16u,
            "float2x2" => 16u,
            "float3x3" => 36u,
            "float4x3" or "float3x4" => 48u,
            "float4x4" or "matrix4x4" => 64u,
            "int2x4" => 32u,
            "texture2d" or "texture3d" or "texturecube" or "texture2darray" or "texturecubearray"
                or "samplerstate" or "sampler" or "byte_address_buffer" or "struct_buffer" or "structured_buffer"
                or "texture2dhandle" or "texture3dhandle" or "bufferhandle" => 4u,
            _ => 4u
        };
    }

    public static uint CalculatePropertyBufferSize(IEnumerable<PropertySemantic>? properties)
    {
        if (properties == null)
        {
            return 0;
        }

        uint currentOffset = 0;
        foreach (var prop in properties)
        {
            var typeSize = GetPropertyTypeSize(prop.type);
            currentOffset += typeSize;
            if (currentOffset % 4 != 0)
            {
                currentOffset += 4 - (currentOffset % 4);
            }
        }

        return currentOffset;
    }

    internal static string BuildPropertiesStruct(string shaderName, IReadOnlyList<PropertySemantic> properties)
    {
        if (properties == null || properties.Count == 0)
        {
            return string.Empty;
        }

        var structName = TemplateStitcher.SanitizeToIdentifier(shaderName);
        var sb = new StringBuilder();
        sb.AppendLine($"struct {structName}");
        sb.AppendLine("{");
        foreach (var prop in properties)
        {
            var hlslType = prop.type.Trim().ToLowerInvariant() switch
            {
                "texture2d" or "texture3d" or "texturecube" or "texture2darray" or "texturecubearray"
                    or "samplerstate" or "sampler" or "byte_address_buffer" or "structured_buffer"
                    => "uint",
                _ => prop.type
            };
            sb.AppendLine($"    {hlslType} {prop.name};");
        }
        sb.AppendLine("};");
        sb.AppendLine();
        sb.AppendLine($"typedef {structName} MaterialProperties;");
        sb.AppendLine($"typedef {structName} ComputeProperties;");
        return sb.ToString();
    }


    public static Result<GraphicsShaderSyntax> ParseGraphicsShaderSyntax(string shaderCode)
    {
        var parseErrors = new List<DSLShaderError>();
        var syntax = AntlrShaderCompiler.ParseShaders(shaderCode, parseErrors);

        if (parseErrors.Count != 0)
        {
            var errorMessages = new StringBuilder();
            foreach (var error in parseErrors)
            {
                errorMessages.AppendLine(error.ToString());
            }
            return Result.Failure("Failed to parse shader due to errors:\n" + errorMessages.ToString());
        }

        return syntax;
    }

    // TODO: Implement shader inheritance resolution, including property and pass merging.
    // Currently, we ignore inheritance.
    public static Result<GraphicsShaderSemantics> GetShaderSemantics(GraphicsShaderSyntax syntax)
    {
        var semantics = AntlrShaderCompiler.ConvertToSemantics(syntax, out var errors);

        if (errors.Count != 0 || semantics == null)
        {
            var errorMessages = new StringBuilder();
            foreach (var error in errors)
            {
                errorMessages.AppendLine(error.ToString());
            }
            return Result.Failure("Failed to compile shader due to errors:\n" + errorMessages.ToString());
        }

        return semantics;
    }

    public static Result<GraphicsShaderDescriptor> ResolveShader(GraphicsShaderSemantics semantics, ShaderReflectionData reflectionData, IReadOnlyDictionary<string, string> virtualShaders, string? shaderPath = null)
    {
        // Template-based shaders are resolved through the template stitcher.
        if (!string.IsNullOrEmpty(semantics.templateName))
        {
            var templateResult = TemplateRegistry.GetTemplate(semantics.templateName!);
            if (templateResult.IsFailure)
            {
                return Result.Failure(templateResult.Message);
            }

            return TemplateStitcher.ResolveShader(templateResult.Value, semantics, reflectionData, virtualShaders);
        }

        var propertiesCode = BuildPropertiesStruct(semantics.name, semantics.properties);
        if (string.IsNullOrEmpty(propertiesCode) && !string.IsNullOrEmpty(reflectionData?.Code))
        {
            propertiesCode = reflectionData.Code;
        }

        var propertyBufferSize = CalculatePropertyBufferSize(semantics.properties);
        if (propertyBufferSize == 0 && reflectionData != null && reflectionData.Size > 0)
        {
            propertyBufferSize = reflectionData.Size;
        }

        var passes = semantics.passes == null ? Array.Empty<PassDescriptor>() : new PassDescriptor[semantics.passes.Count];
        for (var i = 0; i < passes.Length; i++)
        {
            var pass = semantics.passes![i];

            var localPipeline = MergePipeline(pass.localPipeline, PipelineState.Default);

            var result = BuildFinalShaderCode(pass.amplificationShader.shaderPath, pass.includes.AsSpan(), pass.hlsl, propertiesCode, virtualShaders, shaderPath);
            if (result.IsFailure)
            {
                return Result.Failure($"Failed to build shader code for pass '{pass.name}': {result.Message}");
            }

            var amplificationShaderCode = new ShaderCode { code = result.Value, entryPoint = pass.amplificationShader.entry ?? string.Empty };

            result = BuildFinalShaderCode(pass.meshShader.shaderPath, pass.includes.AsSpan(), pass.hlsl, propertiesCode, virtualShaders, shaderPath);
            if (result.IsFailure)
            {
                return Result.Failure($"Failed to build shader code for pass '{pass.name}': {result.Message}");
            }

            var meshShaderCode = new ShaderCode { code = result.Value, entryPoint = pass.meshShader.entry ?? string.Empty };

            result = BuildFinalShaderCode(pass.pixelShader.shaderPath, pass.includes.AsSpan(), pass.hlsl, propertiesCode, virtualShaders, shaderPath);
            if (result.IsFailure)
            {
                return Result.Failure($"Failed to build shader code for pass '{pass.name}': {result.Message}");
            }

            var pixelShaderCode = new ShaderCode { code = result.Value, entryPoint = pass.pixelShader.entry ?? string.Empty };

            var stageMask = ShaderStageMask.Mesh | ShaderStageMask.Pixel;
            if (amplificationShaderCode.IsCreated)
            {
                stageMask |= ShaderStageMask.Amplification;
            }

            passes[i] = new PassDescriptor
            {
                name = pass.name,
                semantic = PassSemanticExtensions.FromName(pass.name),
                localPipeline = localPipeline,
                stageMask = stageMask,
                amplificationShaderCode = amplificationShaderCode,
                meshShaderCode = meshShaderCode,
                pixelShaderCode = pixelShaderCode,
                defines = pass.defines?.ToArray() ?? Array.Empty<string>(),
            };
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

    public static Result<ComputeShaderSyntax> ParseComputeShaderSyntax(string shaderCode)
    {
        var parseErrors = new List<DSLShaderError>();
        var syntax = AntlrShaderCompiler.ParseComputeShaders(shaderCode, parseErrors);

        if (parseErrors.Count != 0)
        {
            var errorMessages = new StringBuilder();
            foreach (var error in parseErrors)
            {
                errorMessages.AppendLine(error.ToString());
            }

            return Result.Failure("Failed to parse compute shader due to errors:\n" + errorMessages.ToString());
        }

        return syntax;
    }

    public static Result<ComputeShaderSemantics> GetShaderSemantics(ComputeShaderSyntax syntax)
    {
        var semantics = AntlrShaderCompiler.ConvertToComputeSemantics(syntax, out var errors);
        if (errors.Count != 0 || semantics == null)
        {
            var errorMessages = new StringBuilder();
            foreach (var error in errors)
            {
                errorMessages.AppendLine(error.ToString());
            }

            return Result.Failure("Failed to compile compute shader due to errors:\n" + errorMessages.ToString());
        }

        return semantics;
    }

    public static Result<ComputeShaderDescriptor> ResolveShader(ComputeShaderSemantics semantics, ShaderReflectionData reflectionData, IReadOnlyDictionary<string, string> virtualShaders, string? shaderPath = null)
    {
        var propertiesCode = BuildPropertiesStruct(semantics.name, semantics.properties);
        if (string.IsNullOrEmpty(propertiesCode) && !string.IsNullOrEmpty(reflectionData?.Code))
        {
            propertiesCode = reflectionData.Code;
        }

        var propertyBufferSize = CalculatePropertyBufferSize(semantics.properties);
        if (propertyBufferSize == 0 && reflectionData != null && reflectionData.Size > 0)
        {
            propertyBufferSize = reflectionData.Size;
        }

        var shaderCodes = new ShaderCode[semantics.entryPoints.Count];
        for (var i = 0; i < shaderCodes.Length; i++)
        {
            var result = BuildFinalShaderCode(semantics.entryPoints[i].shaderPath, semantics.includes.AsSpan(), semantics.hlsl, propertiesCode, virtualShaders, shaderPath);
            if (result.IsFailure)
            {
                return Result.Failure($"Failed to build shader code for entry point '{semantics.entryPoints[i].entry}': {result.Message}");
            }

            shaderCodes[i] = new ShaderCode { code = result.Value, entryPoint = semantics.entryPoints[i].entry ?? string.Empty };
        }

        return new ComputeShaderDescriptor
        {
            Name = semantics.name,
            PropertyBufferSize = propertyBufferSize,
            ShaderModel = semantics.shaderModel,
            ShaderCodes = shaderCodes,
            Defines = semantics.defines?.ToArray() ?? Array.Empty<string>(),
        };
    }
}
