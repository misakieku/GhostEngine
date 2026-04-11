using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Editor.Core.Contracts;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Editor.Core.Utilities;

internal struct GraphicsCompiledResult : IDisposable
{
    public UnsafeArray<byte> asResult;
    public UnsafeArray<byte> msResult;
    public UnsafeArray<byte> psResult;

    public void Dispose()
    {
        asResult.Dispose();
        msResult.Dispose();
        psResult.Dispose();
    }
}

internal static class ShaderCompilerUtility
{
    private static ReadOnlySpan<string> CombineDefines(ReadOnlySpan<string> a, ReadOnlySpan<string> b)
    {
        ReadOnlySpan<string> combined;
        if (b.Length == 0)
        {
            combined = a;
        }
        else if (a.Length == 0)
        {
            combined = b;
        }
        else
        {
            var combinedDefines = new string[a.Length + b.Length];
            a.CopyTo(combinedDefines);
            b.CopyTo(combinedDefines.AsSpan(a.Length));
            combined = combinedDefines;
        }

        return combined;
    }

    public static Result<GraphicsCompiledResult> CompileShaderPass(this IShaderCompiler shaderCompiler, ref readonly PassDescriptor descriptor, ref readonly ShaderCompilationConfig additionalConfig, AllocationHandle allocationHandle)
    {
        var fullDefines = CombineDefines(descriptor.defines, additionalConfig.defines);

        var config = new ShaderCompilationConfig
        {
            defines = fullDefines,
            model = additionalConfig.model,
            optimizeLevel = additionalConfig.optimizeLevel,
            options = additionalConfig.options
        };

        UnsafeArray<byte> asResult = default;
        if (descriptor.amplificationShaderCode.IsCreated)
        {
            config.shaderCode = descriptor.amplificationShaderCode.code;
            config.entryPoint = descriptor.amplificationShaderCode.entryPoint;
            config.stage = ShaderStage.TaskShader;

            var result = shaderCompiler.Compile(ref config, allocationHandle);
            if (result.IsFailure)
            {
                return Result.Failure(result.Message);
            }

            asResult = result.Value;
        }

        UnsafeArray<byte> msResult;
        if (descriptor.meshShaderCode.IsCreated)
        {
            config.shaderCode = descriptor.meshShaderCode.code;
            config.entryPoint = descriptor.meshShaderCode.entryPoint;
            config.stage = ShaderStage.MeshShader;

            var result = shaderCompiler.Compile(ref config, allocationHandle);
            if (result.IsFailure)
            {
                asResult.Dispose();
                return Result.Failure(result.Message);
            }

            msResult = result.Value;
        }
        else
        {
            asResult.Dispose();
            return Result.Failure("Mesh shader expected.");
        }

        UnsafeArray<byte> psResult;
        if (descriptor.pixelShaderCode.IsCreated)
        {
            config.shaderCode = descriptor.pixelShaderCode.code;
            config.entryPoint = descriptor.pixelShaderCode.entryPoint;
            config.stage = ShaderStage.PixelShader;

            var result = shaderCompiler.Compile(ref config, allocationHandle);
            if (result.IsFailure)
            {
                asResult.Dispose();
                msResult.Dispose();
                return Result.Failure(result.Message);
            }

            psResult = result.Value;
        }
        else
        {
            asResult.Dispose();
            msResult.Dispose();
            return Result.Failure("Pixel shader expected.");
        }

        var compiled = new GraphicsCompiledResult
        {
            asResult = asResult,
            msResult = msResult,
            psResult = psResult,
        };

        return compiled;
    }

    public static Result<UnsafeArray<UnsafeArray<byte>>> CompileComputeShader(this IShaderCompiler shaderCompiler, ComputeShaderDescriptor descriptor, ref readonly ShaderCompilationConfig additionalConfig, AllocationHandle allocationHandle)
    {
        var fullDefines = CombineDefines(descriptor.defines, additionalConfig.defines);

        var config = new ShaderCompilationConfig
        {
            defines = fullDefines,
            model = additionalConfig.model,
            optimizeLevel = additionalConfig.optimizeLevel,
            options = additionalConfig.options,
            stage = ShaderStage.ComputeShader,
        };
        
        var compiled = new UnsafeArray<UnsafeArray<byte>>(descriptor.shaderCodes.Length, allocationHandle);
        for (int i = 0; i < descriptor.shaderCodes.Length; i++)
        {
            config.shaderCode = descriptor.shaderCodes[i].code;
            config.entryPoint = descriptor.shaderCodes[i].entryPoint;

            var result = shaderCompiler.Compile(ref config, allocationHandle);
            if (result.IsFailure)
            {
                for (int j = 0; j < i; j++)
                {
                    compiled[j].Dispose();
                }

                compiled.Dispose();
                return Result.Failure(result.Message);
            }

            compiled[i] = result.Value;
        }

        return compiled;
    }
}