using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.DSL.ShaderCompiler;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.AssetForge.Core.Bakers;

public partial class ShaderBakeSettings : ObservableObject, IBakeSettings
{
    [ObservableProperty]
    public partial CompilerOptimizeLevel OptimizeLevel
    {
        get; set;
    } = CompilerOptimizeLevel.O3;

    [ObservableProperty]
    public partial CompilerOption Options
    {
        get; set;
    } = CompilerOption.None;
}

[AssetBaker(Extensions = [".gshdr", ".gcomp"], Type = AssetType.Shader, SettingsType = typeof(ShaderBakeSettings))]
internal partial class ShaderBaker : IAssetBaker, IDisposable
{
    private readonly DXCShaderCompiler _compiler = new DXCShaderCompiler();

    private static async Task WriteShaderEntries(Stream stream, CancellationToken cancellationToken, params (ShaderStage stage, UnsafeArray<byte> bytecode)[] entries)
    {
        var baseByteCodeOffset = stream.Position + (entries.Length * Unsafe.SizeOf<ShaderContentHeader.EntryPointHeader>());

        for (var i = 0; i < entries.Length; i++)
        {
            var (stage, bytecode) = entries[i];
            var byteCodeOffset = baseByteCodeOffset;
            for (var j = 0; j < i; j++)
            {
                byteCodeOffset += entries[j].bytecode.Length;
            }

            var entryPointHeader = new ShaderContentHeader.EntryPointHeader
            {
                stage = stage,
                byteCodeSize = bytecode.Length,
                byteCodeOffset = byteCodeOffset
            };

            stream.Write(entryPointHeader);
        }

        for (var i = 0; i < entries.Length; i++)
        {
            var bytecode = entries[i].bytecode;
            if (!bytecode.IsCreated)
            {
                continue;
            }

            using var memory = NativeMemoryManager<byte>.FromUnsafeCollection(in bytecode);
            await stream.WriteAsync(memory.Memory, cancellationToken).ConfigureAwait(false);
        }
    }

    // TODO: Compile all variants.
    public async Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        if (settings is not ShaderBakeSettings shaderSettings)
        {
            throw new ArgumentException("Invalid settings type. Expected ShaderBakeSettings.", nameof(settings));
        }

        var codeStr = await File.ReadAllTextAsync(src, cancellationToken).ConfigureAwait(false);
        var ext = Path.GetExtension(src);

        var configTemplate = new ShaderCompilationConfig
        {
            optimizeLevel = shaderSettings.OptimizeLevel,
            options = shaderSettings.Options,
            includeDirectories = ctx.AssetDirectories.ToArray(),
        };

        if (string.Equals(ext, ".gshdr", StringComparison.Ordinal))
        {
            var syntax = DSLShaderCompiler.ParseGraphicsShaderSyntax(codeStr).GetValueOrThrow();
            var semantics = DSLShaderCompiler.GetShaderSemantics(syntax).GetValueOrThrow();

            var reflectionData = ctx.ShderMetadata.ReflectionDatas.GetValueOrDefault(semantics.name, new DSL.Models.ShaderReflectionData());
            var descriptor = DSLShaderCompiler.ResolveShader(semantics, reflectionData, ctx.ShderMetadata.VirtualShader).GetValueOrThrow();

            var header = new ShaderContentHeader
            {
                shaderType = ShaderType.Graphics,
                passCount = (uint)descriptor.Passes.Length,
            };

            dst.Write(header);

            foreach (var pass in descriptor.Passes)
            {
                var config = configTemplate with
                {
                    stage = ShaderStage.AmplificationShader,
                    model = descriptor.ShaderModel,
                    defines = pass.defines,

                    entryPoint = pass.amplificationShaderCode.entryPoint,
                    shaderCode = pass.amplificationShaderCode.code,
                };

                if (!pass.meshShaderCode.IsCreated || !pass.pixelShaderCode.IsCreated)
                {
                    throw new InvalidOperationException("Shader pass is missing required shader stages. Both mesh and pixel shaders must be present.");
                }

                using var asByteCode = pass.amplificationShaderCode.IsCreated ?
                    _compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow()
                    : default;

                config.stage = ShaderStage.MeshShader;
                config.entryPoint = pass.meshShaderCode.entryPoint;
                config.shaderCode = pass.meshShaderCode.code;

                using var msByteCode = _compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow();

                config.stage = ShaderStage.PixelShader;
                config.entryPoint = pass.pixelShaderCode.entryPoint;
                config.shaderCode = pass.pixelShaderCode.code;

                using var psByteCode = _compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow();

                var passHeader = new ShaderContentHeader.PassHeader
                {
                    entryPointCount = 3 // Amplification, Mesh, Pixel
                };

                dst.Write(passHeader);
                await WriteShaderEntries(dst, cancellationToken,
                    (ShaderStage.AmplificationShader, asByteCode),
                    (ShaderStage.MeshShader, msByteCode),
                    (ShaderStage.PixelShader, psByteCode));
            }
        }
        else if (string.Equals(ext, ".gcomp", StringComparison.Ordinal))
        {
            var syntax = DSLShaderCompiler.ParseComputeShaderSyntax(codeStr).GetValueOrThrow();
            var semantics = DSLShaderCompiler.GetShaderSemantics(syntax).GetValueOrThrow();

            var reflectionData = ctx.ShderMetadata.ReflectionDatas.GetValueOrDefault(semantics.name, new DSL.Models.ShaderReflectionData());
            var descriptor = DSLShaderCompiler.ResolveShader(semantics, reflectionData, ctx.ShderMetadata.VirtualShader).GetValueOrThrow();

            var header = new ShaderContentHeader
            {
                shaderType = ShaderType.Compute,
                passCount = 1, // Compute shaders have a single pass
            };

            dst.Write(header);

            var byteCodes = new UnsafeArray<byte>[descriptor.ShaderCodes.Length];

            try
            {
                for (var i = 0; i < descriptor.ShaderCodes.Length; i++)
                {
                    var shaderCode = descriptor.ShaderCodes[i];
                    var config = configTemplate with
                    {
                        stage = ShaderStage.ComputeShader,
                        model = descriptor.ShaderModel,
                        defines = descriptor.Defines,

                        entryPoint = shaderCode.entryPoint,
                        shaderCode = shaderCode.code,
                    };

                    byteCodes[i] = _compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow();
                }

                var entries = byteCodes.Select((bc, index) => (ShaderStage.ComputeShader, bc)).ToArray();
                await WriteShaderEntries(dst, cancellationToken, entries);
            }
            finally
            {
                foreach (var code in byteCodes)
                {
                    code.Dispose();
                }
            }
        }
        else
        {
            // This should never happen because the baker is registered for these extensions only.
            throw new NotSupportedException($"Unsupported shader file extension: {ext}");
        }
    }

    public void Dispose()
    {
        _compiler.Dispose();
        GC.SuppressFinalize(this);
    }
}
