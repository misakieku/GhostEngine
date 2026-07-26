using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DSL.Models;
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
    private static PipelineState MeragePipeline(PipelineSemantic? semantic, PipelineState parent)
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

    private static Result<string> BuildFinalShaderCode(string? shaderPath, ReadOnlySpan<string> includes, string? injectedCode, string? properties, IReadOnlyDictionary<string, string> virtualShaders)
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

        if (!string.IsNullOrEmpty(properties))
        {
            sb.AppendLine($"#line 0 \"properties\"");
            sb.AppendLine(properties);
        }

        if (!string.IsNullOrEmpty(injectedCode))
        {
            sb.AppendLine($"#line 0 \"injected_code\"");
            sb.AppendLine(injectedCode);
        }

        if (!string.IsNullOrEmpty(shaderCode))
        {
            sb.AppendLine($"#line 0 \"{shaderPath}\"");
            sb.AppendLine(shaderCode);
        }

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

    public static Result<GraphicsShaderDescriptor> ResolveShader(GraphicsShaderSemantics semantics, ShaderReflectionData reflectionData, IReadOnlyDictionary<string, string> virtualShaders)
    {
        var passes = semantics.passes == null ? Array.Empty<PassDescriptor>() : new PassDescriptor[semantics.passes.Count];
        for (var i = 0; i < passes.Length; i++)
        {
            var pass = semantics.passes![i];
            
            var localPipeline = MeragePipeline(pass.localPipeline, PipelineState.Default);

            var result = BuildFinalShaderCode(pass.amplificationShader.shaderPath, pass.includes.AsSpan(), pass.hlsl, reflectionData.Code, virtualShaders);
            if (result.IsFailure)
            {
                return Result.Failure($"Failed to build shader code for pass '{pass.name}': {result.Message}");
            }

            var amplificationShaderCode = new ShaderCode { code = result.Value, entryPoint = pass.amplificationShader.entry ?? string.Empty };

            result = BuildFinalShaderCode(pass.meshShader.shaderPath, pass.includes.AsSpan(), pass.hlsl, reflectionData.Code, virtualShaders);
            if (result.IsFailure)
            {
                return Result.Failure($"Failed to build shader code for pass '{pass.name}': {result.Message}");
            }

            var meshShaderCode = new ShaderCode { code = result.Value, entryPoint = pass.meshShader.entry ?? string.Empty };

            result = BuildFinalShaderCode(pass.pixelShader.shaderPath, pass.includes.AsSpan(), pass.hlsl, reflectionData.Code, virtualShaders);
            if (result.IsFailure)
            {
                return Result.Failure($"Failed to build shader code for pass '{pass.name}': {result.Message}");
            }

            var pixelShaderCode = new ShaderCode { code = result.Value, entryPoint = pass.pixelShader.entry ?? string.Empty };

            passes[i] = new PassDescriptor
            {
                name = pass.name,
                localPipeline = localPipeline,
                amplificationShaderCode = amplificationShaderCode,
                meshShaderCode = meshShaderCode,
                pixelShaderCode = pixelShaderCode,
                defines = pass.defines?.ToArray() ?? Array.Empty<string>(),
                keywords = pass.keywords?.ToArray() ?? Array.Empty<KeywordsGroup>()
            };
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

    public static Result<ComputeShaderDescriptor> ResolveShader(ComputeShaderSemantics semantics, ShaderReflectionData reflectionData, IReadOnlyDictionary<string, string> virtualShaders)
    {
        var shaderCodes = new ShaderCode[semantics.entryPoints.Count];
        for (var i = 0; i < shaderCodes.Length; i++)
        {
            var result = BuildFinalShaderCode(semantics.entryPoints[i].shaderPath, semantics.includes.AsSpan(), semantics.hlsl, reflectionData.Code, virtualShaders);
            if (result.IsFailure)
            {
                return Result.Failure($"Failed to build shader code for entry point '{semantics.entryPoints[i].entry}': {result.Message}");
            }

            shaderCodes[i] = new ShaderCode { code = result.Value, entryPoint = semantics.entryPoints[i].entry ?? string.Empty };
        }

        return new ComputeShaderDescriptor
        {
            Name = semantics.name,
            PropertyBufferSize = reflectionData.Size,
            ShaderModel = semantics.shaderModel,
            ShaderCodes = shaderCodes,
            Defines = semantics.defines?.ToArray() ?? Array.Empty<string>(),
            Keywords = semantics.keywords?.ToArray() ?? Array.Empty<KeywordsGroup>()
        };
    }
}
