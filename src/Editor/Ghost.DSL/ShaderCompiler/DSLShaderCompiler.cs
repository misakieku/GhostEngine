using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DSL.ShaderParser;
using System.IO.Hashing;
using System.Runtime.InteropServices;
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
    private static ulong GetPassUniqueId(DSLShaderSemantics shader, PassSemantic pass)
    {
        return XxHash64.HashToUInt64(MemoryMarshal.AsBytes($"{shader.name}_{pass.name}".AsSpan()));
    }

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

    // TODO: Implement shader inheritance resolution, including property and pass merging.
    // Currently, we just ignore inheritance.
    public static Result<ShaderDescriptor> ResolveShader(DSLShaderSemantics semantics)
    {
        var descriptor = new ShaderDescriptor
        {
            name = semantics.name,
        };

        if (!ShaderPropertiesRegistry.TryGetCode(semantics.name, out var info))
        {
            info = default;
        }

        descriptor.propertiesCode = info.code ?? string.Empty;
        descriptor.propertyBufferSize = info.size;

        if (semantics.passes != null)
        {
            descriptor.passes = new PassDescriptor[semantics.passes.Count];
            for (var i = 0; i < semantics.passes.Count; i++)
            {
                var pass = semantics.passes[i];
                var localPipeline = MeragePipeline(pass.localPipeline, PipelineState.Default);
                descriptor.passes[i] = new PassDescriptor
                {
                    shader = descriptor,
                    identifier = GetPassUniqueId(semantics, pass),
                    name = pass.name,
                    taskShader = pass.taskShader,
                    meshShader = pass.meshShader,
                    pixelShader = pass.pixelShader,
                    localPipeline = localPipeline,
                    defines = pass.defines?.ToArray() ?? Array.Empty<string>(),
                    includes = pass.includes?.ToArray() ?? Array.Empty<string>(),
                    keywords = pass.keywords?.ToArray() ?? Array.Empty<KeywordsGroup>(),
                    hlsl = pass.hlsl
                };
            }
        }
        else
        {
            descriptor.passes = Array.Empty<PassDescriptor>();
        }

        return descriptor;
    }

    public static Result<ShaderDescriptor> CompileShader(string shaderPath, string generatedOutputDirectory)
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
}
