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
    private readonly SemaphoreSlim _compileLock = new(1, 1);

    private static async Task WriteShaderEntries(Stream stream, long passDataOffset, CancellationToken cancellationToken, params (ShaderStage stage, UnsafeArray<byte> bytecode)[] entries)
    {
        var baseByteCodeOffset = (stream.Position - passDataOffset) + (entries.Length * Unsafe.SizeOf<ShaderContentHeader.EntryPointHeader>());

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

    public async Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        // DXCShaderCompiler is a native handle and is not thread-safe. Serialize
        // concurrent shader bakes through the lock; textures keep running in parallel.
        await _compileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await BakeAssetCoreAsync(src, dst, settings, ctx, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _compileLock.Release();
        }
    }

    private async Task BakeAssetCoreAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
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

            var reflectionData = ctx.ShaderMetadata.ReflectionDatas.GetValueOrDefault(semantics.name, new DSL.Models.ShaderReflectionData());
            var descriptor = DSLShaderCompiler.ResolveShader(semantics, reflectionData, ctx.ShaderMetadata.VirtualShader).GetValueOrThrow();

            var header = new ShaderContentHeader
            {
                shaderType = ShaderType.Graphics,
                passCount = (uint)descriptor.Passes.Length,
            };

            var assetStartOffset = dst.Position;
            dst.Write(header);

            for (var passIdx = 0; passIdx < descriptor.Passes.Length; passIdx++)
            {
                var pass = descriptor.Passes[passIdx];
                var passHeaderOffset = dst.Position;
                if (pass.computeShaderCode.IsCreated)
                {
                    var passHeader = new ShaderContentHeader.PassHeader
                    {
                        entryPointCount = 1,
                    };
                    dst.Write(passHeader); // Placeholder

                    var passDataStart = dst.Position;
                    var config = configTemplate with
                    {
                        stage = ShaderStage.ComputeShader,
                        model = descriptor.ShaderModel,
                        defines = pass.defines,
                        entryPoint = pass.computeShaderCode.entryPoint,
                        shaderCode = pass.computeShaderCode.code,
                    };

                    using var csByteCode = _compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow();

                    await WriteShaderEntries(dst, passDataStart, cancellationToken,
                        (ShaderStage.ComputeShader, csByteCode));

                    passHeader.dataOffset = passDataStart - assetStartOffset;
                    passHeader.dataSize = dst.Position - passDataStart;
                    var endOfPass = dst.Position;
                    dst.Position = passHeaderOffset;
                    dst.Write(passHeader);
                    dst.Position = endOfPass;
                }
                else
                {
                    var passHeader = new ShaderContentHeader.PassHeader
                    {
                        entryPointCount = 3, // Amplification, Mesh, Pixel
                    };
                    dst.Write(passHeader); // Placeholder

                    var passDataStart = dst.Position;
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

                    await WriteShaderEntries(dst, passDataStart, cancellationToken,
                        (ShaderStage.AmplificationShader, asByteCode),
                        (ShaderStage.MeshShader, msByteCode),
                        (ShaderStage.PixelShader, psByteCode));

                    passHeader.dataOffset = passDataStart - assetStartOffset;
                    passHeader.dataSize = dst.Position - passDataStart;
                    var endOfPass = dst.Position;
                    dst.Position = passHeaderOffset;
                    dst.Write(passHeader);
                    dst.Position = endOfPass;
                }
            }
        }
        else if (string.Equals(ext, ".gcomp", StringComparison.Ordinal))
        {
            var syntax = DSLShaderCompiler.ParseComputeShaderSyntax(codeStr).GetValueOrThrow();
            var semantics = DSLShaderCompiler.GetShaderSemantics(syntax).GetValueOrThrow();

            var reflectionData = ctx.ShaderMetadata.ReflectionDatas.GetValueOrDefault(semantics.name, new DSL.Models.ShaderReflectionData());
            var descriptor = DSLShaderCompiler.ResolveShader(semantics, reflectionData, ctx.ShaderMetadata.VirtualShader).GetValueOrThrow();

            var header = new ShaderContentHeader
            {
                shaderType = ShaderType.Compute,
                passCount = 1, // Compute shaders have a single pass
            };

            var assetStartOffset = dst.Position;
            dst.Write(header);

            var passHeaderOffset = dst.Position;
            var passHeader = new ShaderContentHeader.PassHeader
            {
                entryPointCount = (uint)descriptor.ShaderCodes.Length,
            };
            dst.Write(passHeader); // Placeholder

            var passDataStart = dst.Position;
            var byteCodes = new UnsafeArray<byte>[descriptor.ShaderCodes.Length];

            try
            {
                for (var j = 0; j < descriptor.ShaderCodes.Length; j++)
                {
                    var shaderCode = descriptor.ShaderCodes[j];
                    var config = configTemplate with
                    {
                        stage = ShaderStage.ComputeShader,
                        model = descriptor.ShaderModel,
                        defines = descriptor.Defines,
                        entryPoint = shaderCode.entryPoint,
                        shaderCode = shaderCode.code,
                    };

                    byteCodes[j] = _compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow();
                }

                var entries = byteCodes.Select((bc, index) => (ShaderStage.ComputeShader, bc)).ToArray();
                await WriteShaderEntries(dst, passDataStart, cancellationToken, entries);
            }
            finally
            {
                foreach (var code in byteCodes)
                {
                    code.Dispose();
                }
            }

            passHeader.dataOffset = passDataStart - assetStartOffset;
            passHeader.dataSize = dst.Position - passDataStart;
            var endOfPass = dst.Position;
            dst.Position = passHeaderOffset;
            dst.Write(passHeader);
            dst.Position = endOfPass;
        }
        else
        {
            throw new NotSupportedException($"Unsupported shader file extension: {ext}");
        }
    }

    public void Dispose()
    {
        _compiler.Dispose();
        GC.SuppressFinalize(this);
    }
}
