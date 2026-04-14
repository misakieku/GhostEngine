using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DSL.ShaderParser;
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

internal static class DSLShaderCompiler
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

    private static Result<string> BuildFinalShaderCode(string shaderPath, ReadOnlySpan<string> includes, string? injectedCode, string? properties)
    {
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
            sb.AppendLine($"#include \"{includePath}\"");
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

    // TODO: Implement shader inheritance resolution, including property and pass merging.
    // Currently, we just ignore inheritance.
    public static Result<GraphicsShaderDescriptor> ResolveShader(DSLShaderSemantics semantics)
    {
        if (!ShaderPropertiesRegistry.TryGetInfo(semantics.name, out var propertyInfo))
        {
            propertyInfo = default;
        }

        var passes = semantics.passes == null ? Array.Empty<PassDescriptor>() : new PassDescriptor[semantics.passes.Count];
        for (var i = 0; i < passes.Length; i++)
        {
            var pass = semantics.passes![i];
            var localPipeline = MeragePipeline(pass.localPipeline, PipelineState.Default);

            var result = BuildFinalShaderCode(pass.amplificationShader.shaderPath, pass.includes.AsSpan(), pass.hlsl, propertyInfo.code);
            if (result.IsFailure)
            {
                return Result.Failure($"Failed to build shader code for pass '{pass.name}': {result.Message}");
            }

            var amplificationShaderCode = new ShaderCode { code = result.Value, entryPoint = pass.amplificationShader.entry };

            result = BuildFinalShaderCode(pass.meshShader.shaderPath, pass.includes.AsSpan(), pass.hlsl, propertyInfo.code);
            if (result.IsFailure)
            {
                return Result.Failure($"Failed to build shader code for pass '{pass.name}': {result.Message}");
            }

            var meshShaderCode = new ShaderCode { code = result.Value, entryPoint = pass.meshShader.entry };

            result = BuildFinalShaderCode(pass.pixelShader.shaderPath, pass.includes.AsSpan(), pass.hlsl, propertyInfo.code);
            if (result.IsFailure)
            {
                return Result.Failure($"Failed to build shader code for pass '{pass.name}': {result.Message}");
            }

            var pixelShaderCode = new ShaderCode { code = result.Value, entryPoint = pass.pixelShader.entry };

            passes[i] = new PassDescriptor
            {
                name = pass.name,

                amplificationShaderCode = amplificationShaderCode,
                meshShaderCode = meshShaderCode,
                pixelShaderCode = pixelShaderCode,

                localPipeline = localPipeline,
                defines = pass.defines?.ToArray() ?? Array.Empty<string>(),
                keywords = pass.keywords?.ToArray() ?? Array.Empty<KeywordsGroup>()
            };
        }

        var descriptor = new GraphicsShaderDescriptor
        {
            name = semantics.name,
            propertyBufferSize = propertyInfo.size,

            shaderModel = semantics.shaderModel,
            passes = passes
        };

        for (var i = 0; i < descriptor.passes.Length; i++)
        {
            descriptor.passes[i].shader = descriptor;
        }

        return descriptor;
    }

    public static Result<GraphicsShaderDescriptor> CompileGraphicsShader(string shaderPath)
    {
        try
        {
            var source = File.ReadAllText(shaderPath);

            // Use ANTLR4 parser
            var shaderModels = AntlrShaderCompiler.ParseShaders(source, out var parseErrors);

            if (parseErrors.Count != 0)
            {
                var errorMessages = new StringBuilder();
                foreach (var error in parseErrors)
                {
                    errorMessages.AppendLine(error.ToString());
                }

                return Result.Failure("Failed to parse shader due to errors:\n" + errorMessages.ToString());
            }

            if (shaderModels.Count == 0)
            {
                return Result.Failure("No shader found in the provided file.");
            }

            // Convert to semantics
            var model = AntlrShaderCompiler.ConvertToSemantics(shaderModels[0], out var errors);

            if (errors.Count != 0 || model == null)
            {
                var errorMessages = new StringBuilder();
                foreach (var error in errors)
                {
                    errorMessages.AppendLine(error.ToString());
                }

                return Result.Failure("Failed to compile shader due to errors:\n" + errorMessages.ToString());
            }

            var result = ResolveShader(model);
            if (result.IsFailure)
            {
                return result;
            }

            return result.Value;
        }
        catch (Exception ex)
        {
            return Result.Failure("Failed to compile shader: " + ex.Message);
        }
    }

    public static Result<ComputeShaderDescriptor> CompileComputeShader(string shaderPath)
    {
        try
        {
            var source = File.ReadAllText(shaderPath);

            var shaderModels = AntlrShaderCompiler.ParseComputeShaders(source, out var parseErrors);

            if (parseErrors.Count != 0)
            {
                var errorMessages = new StringBuilder();
                foreach (var error in parseErrors)
                {
                    errorMessages.AppendLine(error.ToString());
                }

                return Result.Failure("Failed to parse compute shader due to errors:\n" + errorMessages.ToString());
            }

            if (shaderModels.Count == 0)
            {
                return Result.Failure("No compute shader found in the provided file.");
            }

            var model = AntlrShaderCompiler.ConvertToComputeSemantics(shaderModels[0], out var errors);

            if (errors.Count != 0 || model == null)
            {
                var errorMessages = new StringBuilder();
                foreach (var error in errors)
                {
                    errorMessages.AppendLine(error.ToString());
                }

                return Result.Failure("Failed to compile compute shader due to errors:\n" + errorMessages.ToString());
            }

            var result = ResolveComputeShader(model);
            if (result.IsFailure)
            {
                return result;
            }

            return result.Value;
        }
        catch (Exception ex)
        {
            return Result.Failure("Failed to compile compute shader: " + ex.Message);
        }
    }

    public static Result<ComputeShaderDescriptor> ResolveComputeShader(DSLComputeShaderSemantics semantics)
    {
        if (!ShaderPropertiesRegistry.TryGetInfo(semantics.name, out var propertyInfo))
        {
            propertyInfo = default;
        }

        var shaderCodes = new ShaderCode[semantics.entryPoints.Count];
        for (var i = 0; i < shaderCodes.Length; i++)
        {
            var result = BuildFinalShaderCode(semantics.entryPoints[i].shaderPath, semantics.includes.AsSpan(), semantics.hlsl, propertyInfo.code);
            if (result.IsFailure)
            {
                return Result.Failure($"Failed to build shader code for entry point '{semantics.entryPoints[i].entry}': {result.Message}");
            }

            shaderCodes[i] = new ShaderCode { code = result.Value, entryPoint = semantics.entryPoints[i].entry };
        }

        return new ComputeShaderDescriptor
        {
            name = semantics.name,
            propertyBufferSize = propertyInfo.size,
            shaderModel = semantics.shaderModel,
            shaderCodes = shaderCodes,
            defines = semantics.defines?.ToArray() ?? Array.Empty<string>(),
            keywords = semantics.keywords?.ToArray() ?? Array.Empty<KeywordsGroup>()
        };
    }
}
