using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.DSL.ShaderCompiler;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

    [ObservableProperty]
    public partial string[] Defines
    {
        get; set;
    } = Array.Empty<string>();
}

[AssetBaker(Extensions = [".gshdr"], Type = AssetType.Shader, SettingsType = typeof(ShaderBakeSettings))]
internal partial class ShaderBaker : IAssetBaker, IAssetDependencyScanner
{
    internal static readonly DXCShaderCompiler s_compiler = new DXCShaderCompiler();
    internal static readonly SemaphoreSlim s_compileLock = new SemaphoreSlim(1, 1);

    public static IEnumerable<string> FindDependencies(string sourceFile, IBakeSettings settings, AssetBakerContext ctx)
    {
        if (!File.Exists(sourceFile))
        {
            return Array.Empty<string>();
        }

        var codeStr = File.ReadAllText(sourceFile);
        return ShaderIncludeResolver.ResolveDependencies(sourceFile, codeStr, ctx.AssetDirectories, ctx.ShaderMetadata.VirtualShader);
    }

    public IEnumerable<string> ScanDependencies(string sourceFile, IBakeSettings settings, AssetBakerContext ctx)
    {
        return FindDependencies(sourceFile, settings, ctx);
    }

    private static ulong GetLayoutHash(DSL.Models.ShaderReflectionData reflectionData, uint propertyBufferSize, string shaderName)
    {
        if (!string.IsNullOrEmpty(reflectionData.Code))
        {
            var codeHash = XxHash64.HashToUInt64(MemoryMarshal.AsBytes(reflectionData.Code.AsSpan()));
            return Hash.Combine64(codeHash, reflectionData.Size);
        }

        var nameHash = XxHash64.HashToUInt64(MemoryMarshal.AsBytes(shaderName.AsSpan()));
        return Hash.Combine64(nameHash, propertyBufferSize);
    }

    private static void WriteName(Stream stream, long assetStartOffset, string name, ref long nameOffset, ref uint nameSize)
    {
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
        nameOffset = stream.Position - assetStartOffset;
        nameSize = (uint)nameBytes.Length;
        stream.Write(nameBytes);
    }

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
        await s_compileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await BakeAssetCoreAsync(src, dst, settings, ctx, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            s_compileLock.Release();
        }
    }

    internal static async Task BakeAssetCoreAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        if (settings is not ShaderBakeSettings shaderSettings)
        {
            throw new ArgumentException("Invalid settings type. Expected ShaderBakeSettings.", nameof(settings));
        }

        var codeStr = await File.ReadAllTextAsync(src, cancellationToken).ConfigureAwait(false);
        var ext = Path.GetExtension(src);

        var dependencies = ShaderIncludeResolver.ResolveDependencies(src, codeStr, ctx.AssetDirectories, ctx.ShaderMetadata.VirtualShader);
        foreach (var dep in dependencies)
        {
            ctx.AddDependency(dep);
        }

        var srcDir = Path.GetDirectoryName(Path.GetFullPath(src));
        var includeDirs = new List<string>(ctx.AssetDirectories.Count + 1);
        if (!string.IsNullOrEmpty(srcDir))
        {
            includeDirs.Add(srcDir);
        }

        foreach (var dir in ctx.AssetDirectories)
        {
            if (!includeDirs.Contains(dir, StringComparer.OrdinalIgnoreCase))
            {
                includeDirs.Add(dir);
            }
        }

        var configTemplate = new ShaderCompilationConfig
        {
            optimizeLevel = shaderSettings.OptimizeLevel,
            options = shaderSettings.Options,
            includeDirectories = includeDirs.ToArray(),
        };

        if (string.Equals(ext, ".gshdr", StringComparison.Ordinal))
        {
            var syntax = DSLShaderCompiler.ParseGraphicsShaderSyntax(codeStr).GetValueOrThrow();
            var semantics = DSLShaderCompiler.GetShaderSemantics(syntax).GetValueOrThrow();

            var reflectionData = ctx.ShaderMetadata.ReflectionDatas.GetValueOrDefault(semantics.name, new DSL.Models.ShaderReflectionData());
            var descriptor = DSLShaderCompiler.ResolveShader(semantics, reflectionData, ctx.ShaderMetadata.VirtualShader).GetValueOrThrow();

            var assetStartOffset = dst.Position;
            var header = new ShaderContentHeader
            {
                shaderType = ShaderType.Graphics,
                passCount = (uint)descriptor.Passes.Length,
                propertyBufferSize = descriptor.PropertyBufferSize,
                shaderModel = descriptor.ShaderModel,
                shaderId = ShaderIdentity.GetShaderId(descriptor.Name),
                familyId = ShaderIdentity.GetShaderId(semantics.templateName ?? descriptor.Name),
                layoutHash = GetLayoutHash(reflectionData, descriptor.PropertyBufferSize, descriptor.Name),
            };

            dst.Write(header);
            WriteName(dst, assetStartOffset, descriptor.Name, ref header.nameOffset, ref header.nameSize);

            for (var passIdx = 0; passIdx < descriptor.Passes.Length; passIdx++)
            {
                var pass = descriptor.Passes[passIdx];
                var passHeaderOffset = dst.Position;
                var passHeader = new ShaderContentHeader.PassHeader
                {
                    entryPointCount = pass.computeShaderCode.IsCreated ? 1u : (pass.amplificationShaderCode.IsCreated ? 3u : 2u),
                    semantic = pass.semantic,
                    stageMask = pass.stageMask,
                    passId = ShaderIdentity.GetPassId(header.shaderId, passIdx),
                    localPipeline = pass.localPipeline,
                };
                dst.Write(passHeader); // Placeholder
                WriteName(dst, assetStartOffset, pass.name, ref passHeader.nameOffset, ref passHeader.nameSize);
                var passDataStart = dst.Position;

                if (pass.computeShaderCode.IsCreated)
                {
                    var config = configTemplate with
                    {
                        stage = ShaderStage.ComputeShader,
                        model = descriptor.ShaderModel,
                        defines = pass.defines,
                        entryPoint = pass.computeShaderCode.entryPoint,
                        shaderCode = pass.computeShaderCode.code,
                    };

                    using var csByteCode = s_compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow();
                    await WriteShaderEntries(dst, passDataStart, cancellationToken,
                        (ShaderStage.ComputeShader, csByteCode));
                }
                else
                {
                    if (!pass.meshShaderCode.IsCreated || !pass.pixelShaderCode.IsCreated ||
                        (pass.stageMask & (ShaderStageMask.Mesh | ShaderStageMask.Pixel)) != (ShaderStageMask.Mesh | ShaderStageMask.Pixel))
                    {
                        throw new InvalidOperationException($"Shader pass '{pass.name}' is missing required graphics shader stages.");
                    }

                    var config = configTemplate with
                    {
                        stage = ShaderStage.MeshShader,
                        model = descriptor.ShaderModel,
                        defines = pass.defines,
                        entryPoint = pass.meshShaderCode.entryPoint,
                        shaderCode = pass.meshShaderCode.code,
                    };
                    using var msByteCode = s_compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow();

                    config.stage = ShaderStage.PixelShader;
                    config.entryPoint = pass.pixelShaderCode.entryPoint;
                    config.shaderCode = pass.pixelShaderCode.code;
                    using var psByteCode = s_compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow();

                    if (pass.amplificationShaderCode.IsCreated)
                    {
                        config.stage = ShaderStage.AmplificationShader;
                        config.entryPoint = pass.amplificationShaderCode.entryPoint;
                        config.shaderCode = pass.amplificationShaderCode.code;
                        using var asByteCode = s_compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow();
                        await WriteShaderEntries(dst, passDataStart, cancellationToken,
                            (ShaderStage.AmplificationShader, asByteCode),
                            (ShaderStage.MeshShader, msByteCode),
                            (ShaderStage.PixelShader, psByteCode));
                    }
                    else
                    {
                        await WriteShaderEntries(dst, passDataStart, cancellationToken,
                            (ShaderStage.MeshShader, msByteCode),
                            (ShaderStage.PixelShader, psByteCode));
                    }
                }

                passHeader.dataOffset = passDataStart - assetStartOffset;
                passHeader.dataSize = dst.Position - passDataStart;
                var endOfPass = dst.Position;
                dst.Position = passHeaderOffset;
                dst.Write(passHeader);
                dst.Position = endOfPass;
            }

            var endOfAsset = dst.Position;
            dst.Position = assetStartOffset;
            dst.Write(header);
            dst.Position = endOfAsset;
        }
        else if (string.Equals(ext, ".gcomp", StringComparison.Ordinal))
        {
            var syntax = DSLShaderCompiler.ParseComputeShaderSyntax(codeStr).GetValueOrThrow();
            var semantics = DSLShaderCompiler.GetShaderSemantics(syntax).GetValueOrThrow();

            var reflectionData = ctx.ShaderMetadata.ReflectionDatas.GetValueOrDefault(semantics.name, new DSL.Models.ShaderReflectionData());
            var descriptor = DSLShaderCompiler.ResolveShader(semantics, reflectionData, ctx.ShaderMetadata.VirtualShader).GetValueOrThrow();

            var assetStartOffset = dst.Position;
            var header = new ShaderContentHeader
            {
                shaderType = ShaderType.Compute,
                passCount = 1,
                propertyBufferSize = descriptor.PropertyBufferSize,
                shaderModel = descriptor.ShaderModel,
                shaderId = ShaderIdentity.GetShaderId(descriptor.Name),
                familyId = ShaderIdentity.GetShaderId(descriptor.Name),
                layoutHash = GetLayoutHash(reflectionData, descriptor.PropertyBufferSize, descriptor.Name),
            };

            dst.Write(header);
            WriteName(dst, assetStartOffset, descriptor.Name, ref header.nameOffset, ref header.nameSize);

            var passHeaderOffset = dst.Position;
            var passHeader = new ShaderContentHeader.PassHeader
            {
                entryPointCount = (uint)descriptor.ShaderCodes.Length,
                semantic = PassSemantic.Custom,
                stageMask = ShaderStageMask.Compute,
                passId = ShaderIdentity.GetPassId(header.shaderId, 0),
                localPipeline = PipelineState.Default,
            };
            dst.Write(passHeader); // Placeholder
            WriteName(dst, assetStartOffset, descriptor.Name, ref passHeader.nameOffset, ref passHeader.nameSize);
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

                    byteCodes[j] = s_compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow();
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
            var endOfAsset = dst.Position;
            dst.Position = assetStartOffset;
            dst.Write(header);
            dst.Position = endOfAsset;
        }
        else if (string.Equals(ext, ".ggraph", StringComparison.Ordinal))
        {
            var syntax = DSLShaderCompiler.ParseComputeShaderSyntax(codeStr).GetValueOrThrow();
            var semantics = DSLShaderCompiler.GetShaderSemantics(syntax).GetValueOrThrow();

            var reflectionData = ctx.ShaderMetadata.ReflectionDatas.GetValueOrDefault(semantics.name, new DSL.Models.ShaderReflectionData());
            var descriptor = DSLShaderCompiler.ResolveShader(semantics, reflectionData, ctx.ShaderMetadata.VirtualShader).GetValueOrThrow();

            var assetStartOffset = dst.Position;
            var header = new ShaderContentHeader
            {
                shaderType = ShaderType.WorkGraph,
                passCount = 1,
                propertyBufferSize = descriptor.PropertyBufferSize,
                shaderModel = ShaderModel.SM_6_8,
                shaderId = ShaderIdentity.GetShaderId(descriptor.Name),
                familyId = ShaderIdentity.GetShaderId(descriptor.Name),
                layoutHash = GetLayoutHash(reflectionData, descriptor.PropertyBufferSize, descriptor.Name),
            };

            dst.Write(header);
            WriteName(dst, assetStartOffset, descriptor.Name, ref header.nameOffset, ref header.nameSize);

            var passHeaderOffset = dst.Position;
            var passHeader = new ShaderContentHeader.PassHeader
            {
                entryPointCount = 1,
                semantic = PassSemantic.Custom,
                stageMask = ShaderStageMask.None,
                passId = ShaderIdentity.GetPassId(header.shaderId, 0),
                localPipeline = PipelineState.Default,
            };
            dst.Write(passHeader); // Placeholder
            WriteName(dst, assetStartOffset, descriptor.Name, ref passHeader.nameOffset, ref passHeader.nameSize);
            var passDataStart = dst.Position;

            var shaderCode = descriptor.ShaderCodes.Length > 0 ? descriptor.ShaderCodes[0] : default;
            var config = configTemplate with
            {
                stage = ShaderStage.Library,
                model = ShaderModel.SM_6_8,
                defines = descriptor.Defines,
                entryPoint = string.Empty,
                shaderCode = shaderCode.code,
            };

            using var libraryByteCode = s_compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow();
            await WriteShaderEntries(dst, passDataStart, cancellationToken, (ShaderStage.Library, libraryByteCode));

            passHeader.dataOffset = passDataStart - assetStartOffset;
            passHeader.dataSize = dst.Position - passDataStart;
            var endOfPass = dst.Position;
            dst.Position = passHeaderOffset;
            dst.Write(passHeader);
            dst.Position = endOfPass;
            var endOfAsset = dst.Position;
            dst.Position = assetStartOffset;
            dst.Write(header);
            dst.Position = endOfAsset;
        }
        else
        {
            throw new NotSupportedException($"Unsupported shader file extension: {ext}");
        }
    }
}

[AssetBaker(Extensions = [".gcomp"], Type = AssetType.ComputeShader, SettingsType = typeof(ShaderBakeSettings))]
internal class ComputeShaderBaker : IAssetBaker, IAssetDependencyScanner
{
    public async Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        await ShaderBaker.s_compileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await ShaderBaker.BakeAssetCoreAsync(src, dst, settings, ctx, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ShaderBaker.s_compileLock.Release();
        }
    }

    public IEnumerable<string> ScanDependencies(string sourceFile, IBakeSettings settings, AssetBakerContext ctx)
    {
        return ShaderBaker.FindDependencies(sourceFile, settings, ctx);
    }
}

[AssetBaker(Extensions = [".ggraph"], Type = AssetType.WorkGraph, SettingsType = typeof(ShaderBakeSettings))]
internal class WorkGraphBaker : IAssetBaker, IAssetDependencyScanner
{
    public async Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        await ShaderBaker.s_compileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await ShaderBaker.BakeAssetCoreAsync(src, dst, settings, ctx, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ShaderBaker.s_compileLock.Release();
        }
    }

    public IEnumerable<string> ScanDependencies(string sourceFile, IBakeSettings settings, AssetBakerContext ctx)
    {
        return ShaderBaker.FindDependencies(sourceFile, settings, ctx);
    }
}