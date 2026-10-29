using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.DSL.ShaderCompiler;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Ghost.AssetForge.Core.Bakers;

public partial class ShaderBakeSettings : ObservableObject, IBakeSettings
{
    public const int VERSION = 1;

    public int Version { get; set; } = VERSION;

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
    public partial ShaderModel ShaderModel
    {
        get; set;
    } = ShaderModel.SM_6_6;

    [ObservableProperty]
    public partial string[] Defines
    {
        get; set;
    } = Array.Empty<string>();

    [ObservableProperty]
    public partial Dictionary<string, string[]> DefineGroups
    {
        get; set;
    } = new Dictionary<string, string[]>();
}

[AssetBaker(Extensions = [".gshdr"], Type = AssetType.Shader, SettingsType = typeof(ShaderBakeSettings), SettingsVersion = ShaderBakeSettings.VERSION)]
internal partial class ShaderBaker : IAssetBaker, IAssetDependencyScanner
{
    private static readonly DXCShaderCompiler s_compiler = new DXCShaderCompiler();
    private static readonly ConcurrentDictionary<ulong, byte[]> s_compileCache = new();

    internal static readonly SemaphoreSlim s_compileLock = new SemaphoreSlim(1, 1);

    internal static UnsafeArray<byte> CompileStage(in ShaderCompilationConfig config)
    {
        var key = config.ComputeHash();
        if (s_compileCache.TryGetValue(key, out var cached))
        {
            var arr = new UnsafeArray<byte>(cached.Length, AllocationHandle.TLSF);
            cached.AsSpan().CopyTo(arr.AsSpan());
            return arr;
        }

        var result = s_compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow();
        var bytes = result.ToArray();
        s_compileCache.TryAdd(key, bytes);
        return result;
    }

    public static IEnumerable<string> FindDependencies(string sourceFile, AssetBakerContext ctx)
    {
        if (!File.Exists(sourceFile))
        {
            return Array.Empty<string>();
        }

        var codeStr = File.ReadAllText(sourceFile);
        return ShaderIncludeResolver.ResolveDependencies(sourceFile, codeStr, ctx.AssetDirectories);
    }

    public IEnumerable<string> ScanDependencies(string sourceFile, IBakeSettings settings, AssetBakerContext ctx)
    {
        return FindDependencies(sourceFile, ctx);
    }

    private static ulong GetLayoutHash(uint propertyBufferSize, string shaderName)
    {
        var nameHash = XxHash64.HashToUInt64(MemoryMarshal.AsBytes(shaderName.AsSpan()));
        return Hash.Combine64(nameHash, propertyBufferSize);
    }

    private static void WriteName(Stream stream, long assetStartOffset, string name, ref long nameOffset, ref uint nameSize)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name);
        nameOffset = stream.Position - assetStartOffset;
        nameSize = (uint)nameBytes.Length;
        stream.Write(nameBytes);
    }

    private static List<string[]> GenerateKeywordPermutations(IReadOnlyList<KeywordGroupDef>? groups)
    {
        var result = new List<string[]>();
        if (groups == null || groups.Count == 0)
        {
            result.Add(Array.Empty<string>());
            return result;
        }

        void Recurse(int groupIndex, List<string> current)
        {
            if (groupIndex == groups.Count)
            {
                result.Add(current.ToArray());
                return;
            }

            var group = groups[groupIndex];
            if (group.Keywords.Length == 0)
            {
                Recurse(groupIndex + 1, current);
                return;
            }

            for (var i = 0; i < group.Keywords.Length; i++)
            {
                current.Add(group.Keywords[i]);
                Recurse(groupIndex + 1, current);
                current.RemoveAt(current.Count - 1);
            }
        }

        Recurse(0, new List<string>());
        return result;
    }

    private static List<(ShaderStage stage, UnsafeArray<byte> bytecode)> CompileGraphicsPassEntries(
        PassDescriptor pass, ShaderCompilationConfig configTemplate, string[] permutationDefines)
    {
        var activeDefines = pass.defines.Concat(configTemplate.defines).Concat(permutationDefines);
        if (pass.computeShaderCode.IsCreated)
        {
            var config = configTemplate with
            {
                stage = ShaderStage.ComputeShader,
                defines = activeDefines,
                entryPoint = pass.computeShaderCode.entryPoint,
                shaderCode = pass.computeShaderCode.code,
            };

            var csByteCode = CompileStage(in config);
            return new List<(ShaderStage stage, UnsafeArray<byte> bytecode)> { (ShaderStage.ComputeShader, csByteCode) };
        }

        if (!pass.meshShaderCode.IsCreated || !pass.stageMask.HasFlag(ShaderStageMask.Mesh))
        {
            throw new InvalidOperationException($"Shader pass '{pass.name}' is missing required graphics shader stages.");
        }

        var entries = new List<(ShaderStage stage, UnsafeArray<byte> bytecode)>();
        var msConfig = configTemplate with
        {
            stage = ShaderStage.MeshShader,
            defines = activeDefines,
            entryPoint = pass.meshShaderCode.entryPoint,
            shaderCode = pass.meshShaderCode.code,
        };

        var msByteCode = CompileStage(in msConfig);
        entries.Add((ShaderStage.MeshShader, msByteCode));

        if (pass.pixelShaderCode.IsCreated)
        {
            var psConfig = configTemplate with
            {
                stage = ShaderStage.PixelShader,
                defines = activeDefines,
                entryPoint = pass.pixelShaderCode.entryPoint,
                shaderCode = pass.pixelShaderCode.code,
            };
            var psByteCode = CompileStage(in psConfig);
            entries.Add((ShaderStage.PixelShader, psByteCode));
        }

        if (pass.amplificationShaderCode.IsCreated)
        {
            var asConfig = configTemplate with
            {
                stage = ShaderStage.AmplificationShader,
                defines = activeDefines,
                entryPoint = pass.amplificationShaderCode.entryPoint,
                shaderCode = pass.amplificationShaderCode.code,
            };
            var asByteCode = CompileStage(in asConfig);
            entries.Add((ShaderStage.AmplificationShader, asByteCode));
        }

        return entries;
    }

    private static List<(ShaderStage stage, UnsafeArray<byte> bytecode)> CompileComputeShaderEntries(
        ComputeShaderDescriptor descriptor, ShaderCompilationConfig configTemplate, string[] permutationDefines)
    {
        var activeDefines = descriptor.Defines.Concat(configTemplate.defines).Concat(permutationDefines);
        var entries = new List<(ShaderStage stage, UnsafeArray<byte> bytecode)>(descriptor.ShaderCodes.Length);

        for (var j = 0; j < descriptor.ShaderCodes.Length; j++)
        {
            var shaderCode = descriptor.ShaderCodes[j];
            var config = configTemplate with
            {
                stage = ShaderStage.ComputeShader,
                defines = activeDefines,
                entryPoint = shaderCode.entryPoint,
                shaderCode = shaderCode.code,
            };

            entries.Add((ShaderStage.ComputeShader, CompileStage(in config)));
        }

        return entries;
    }

    private static async Task WriteMultiVariantPass(
        Stream dst,
        long assetStartOffset,
        long passDataStart,
        uint entryPointCount,
        List<string[]> permutations,
        Func<string[], List<(ShaderStage stage, UnsafeArray<byte> bytecode)>> compileFunc,
        CancellationToken cancellationToken)
    {
        if (permutations.Count <= 1)
        {
            var entries = compileFunc(permutations.Count == 1 ? permutations[0] : Array.Empty<string>());
            try
            {
                await WriteShaderEntries(dst, passDataStart, cancellationToken, entries);
            }
            finally
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    entries[i].bytecode.Dispose();
                }
            }
            return;
        }

        var variantHeadersPos = dst.Position;
        for (var v = 0; v < permutations.Count; v++)
        {
            dst.Write(default(ShaderContentHeader.VariantHeader));
        }

        var variantHeaders = new ShaderContentHeader.VariantHeader[permutations.Count];
        for (var v = 0; v < permutations.Count; v++)
        {
            var permutation = permutations[v];
            var kwStr = string.Join(';', permutation);
            var kwBytes = Encoding.UTF8.GetBytes(kwStr);
            var kwOffset = dst.Position - assetStartOffset;
            dst.Write(kwBytes);

            var variantDataStart = dst.Position;
            var entries = compileFunc(permutation);
            try
            {
                await WriteShaderEntries(dst, variantDataStart, cancellationToken, entries);
            }
            finally
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    entries[i].bytecode.Dispose();
                }
            }

            variantHeaders[v] = new ShaderContentHeader.VariantHeader
            {
                entryPointCount = entryPointCount,
                keywordCount = (uint)permutation.Length,
                keywordOffset = kwOffset,
                keywordSize = (uint)kwBytes.Length,
                dataOffset = variantDataStart - assetStartOffset,
                dataSize = dst.Position - variantDataStart,
            };
        }

        var endOfPassData = dst.Position;
        dst.Position = variantHeadersPos;
        for (var v = 0; v < permutations.Count; v++)
        {
            dst.Write(variantHeaders[v]);
        }
        dst.Position = endOfPassData;
    }

    private static async Task WriteShaderEntries(Stream stream, long passDataOffset, CancellationToken cancellationToken, params IReadOnlyList<(ShaderStage stage, UnsafeArray<byte> bytecode)> entries)
    {
        var baseByteCodeOffset = (stream.Position - passDataOffset) + (entries.Count * Unsafe.SizeOf<ShaderContentHeader.EntryPointHeader>());

        for (var i = 0; i < entries.Count; i++)
        {
            var (stage, bytecode) = entries[i];
            if (!bytecode.IsCreated)
            {
                continue;
            }

            var byteCodeOffset = baseByteCodeOffset;
            for (var j = 0; j < i; j++)
            {
                byteCodeOffset += entries[j].bytecode.Length;
            }

            var bytecodeHash = bytecode.IsCreated && bytecode.Length > 0
                ? XxHash64.HashToUInt64(bytecode.AsSpan())
                : 0UL;

            var entryPointHeader = new ShaderContentHeader.EntryPointHeader
            {
                stage = stage,
                bytecodeHash = bytecodeHash,
                byteCodeSize = bytecode.Length,
                byteCodeOffset = byteCodeOffset
            };

            stream.Write(entryPointHeader);
        }

        for (var i = 0; i < entries.Count; i++)
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
            await BakeAssetCoreAsync(src, AssetType.Shader, dst, settings, ctx, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            s_compileLock.Release();
        }
    }

    internal static async Task BakeAssetCoreAsync(string src, AssetType assetType, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        if (settings is not ShaderBakeSettings shaderSettings)
        {
            throw new ArgumentException("Invalid settings type. Expected ShaderBakeSettings.", nameof(settings));
        }

        var codeStr = await File.ReadAllTextAsync(src, cancellationToken).ConfigureAwait(false);
        var ext = Path.GetExtension(src);

        var dependencies = ShaderIncludeResolver.ResolveDependencies(src, codeStr, ctx.AssetDirectories);
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

        var options = shaderSettings.Options;
        if (ctx.BakeConfig.ForceShaderDebugInfo)
        {
            options |= CompilerOption.KeepDebugInfo;
        }

        var configTemplate = new ShaderCompilationConfig
        {
            defines = shaderSettings.Defines,
            optimizeLevel = shaderSettings.OptimizeLevel,
            options = options,
            includeDirectories = includeDirs,
            shaderModel = shaderSettings.ShaderModel,
        };

        await BakeShaderAssetAsync(src, assetType, dst, ctx, codeStr, ext, configTemplate, cancellationToken);

        foreach (var group in shaderSettings.DefineGroups)
        {
            using var subAssetStream = ctx.AddSubAsset(group.Key, assetType);
            var configWithGroup = configTemplate with
            {
                defines = shaderSettings.Defines.Concat(group.Value),
            };

            await BakeShaderAssetAsync(src, assetType, subAssetStream, ctx, codeStr, ext, configWithGroup, cancellationToken);
        }
    }

    private static async Task BakeShaderAssetAsync(string src, AssetType assetType, Stream dst, AssetBakerContext ctx, string codeStr, string ext, ShaderCompilationConfig configTemplate, CancellationToken cancellationToken)
    {
        switch (assetType)
        {
            case AssetType.Shader:
            {
                var syntax = DSLShaderCompiler.ParseGraphicsShaderSyntax(codeStr).GetValueOrThrow();
                var semantics = DSLShaderCompiler.GetShaderSemantics(syntax).GetValueOrThrow();

                var descriptor = DSLShaderCompiler.ResolveShader(semantics, src).GetValueOrThrow();

                var assetStartOffset = dst.Position;
                var header = new ShaderContentHeader
                {
                    shaderType = ShaderType.Graphics,
                    passCount = (uint)descriptor.Passes.Length,
                    propertyBufferSize = descriptor.PropertyBufferSize,
                    shaderId = ShaderIdentity.GetShaderId(descriptor.Name),
                    familyId = ShaderIdentity.GetShaderId(semantics.templateName ?? descriptor.Name),
                    layoutHash = GetLayoutHash(descriptor.PropertyBufferSize, descriptor.Name),
                };

                dst.Write(header);
                WriteName(dst, assetStartOffset, descriptor.Name, ref header.nameOffset, ref header.nameSize);

                for (var passIdx = 0; passIdx < descriptor.Passes.Length; passIdx++)
                {
                    var pass = descriptor.Passes[passIdx];
                    var permutations = GenerateKeywordPermutations(pass.keywordGroups);
                    var passHeaderOffset = dst.Position;
                    var passHeader = new ShaderContentHeader.PassHeader
                    {
                        entryPointCount = pass.computeShaderCode.IsCreated ? 1u : (1 + (pass.amplificationShaderCode.IsCreated ? 1u : 0u) + (pass.pixelShaderCode.IsCreated ? 1u : 0u)),
                        semantic = pass.semantic,
                        stageMask = pass.stageMask,
                        variantCount = (ushort)permutations.Count,
                        passId = ShaderIdentity.GetPassId(header.shaderId, passIdx),
                        localPipeline = pass.localPipeline,
                        shadingModelId = pass.shadingModelId,
                    };
                    dst.Write(passHeader); // Placeholder
                    WriteName(dst, assetStartOffset, pass.name, ref passHeader.nameOffset, ref passHeader.nameSize);
                    var passDataStart = dst.Position;

                    await WriteMultiVariantPass(
                        dst, assetStartOffset, passDataStart, passHeader.entryPointCount, permutations,
                        perm => CompileGraphicsPassEntries(pass, configTemplate, perm),
                        cancellationToken);

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
                break;
            }

            case AssetType.ComputeShader:
            {
                var syntax = DSLShaderCompiler.ParseComputeShaderSyntax(codeStr).GetValueOrThrow();
                var semantics = DSLShaderCompiler.GetShaderSemantics(syntax).GetValueOrThrow();

                var descriptor = DSLShaderCompiler.ResolveShader(semantics, src).GetValueOrThrow();

                var assetStartOffset = dst.Position;
                var header = new ShaderContentHeader
                {
                    shaderType = ShaderType.Compute,
                    passCount = 1,
                    propertyBufferSize = descriptor.PropertyBufferSize,
                    shaderId = ShaderIdentity.GetShaderId(descriptor.Name),
                    familyId = ShaderIdentity.GetShaderId(descriptor.Name),
                    layoutHash = GetLayoutHash(descriptor.PropertyBufferSize, descriptor.Name),
                };

                dst.Write(header);
                WriteName(dst, assetStartOffset, descriptor.Name, ref header.nameOffset, ref header.nameSize);

                var permutations = GenerateKeywordPermutations(descriptor.KeywordGroups);
                var passHeaderOffset = dst.Position;
                var passHeader = new ShaderContentHeader.PassHeader
                {
                    entryPointCount = (uint)descriptor.ShaderCodes.Length,
                    semantic = PassSemantic.Custom,
                    stageMask = ShaderStageMask.Compute,
                    variantCount = (ushort)permutations.Count,
                    passId = ShaderIdentity.GetPassId(header.shaderId, 0),
                    localPipeline = PipelineState.Default,
                };

                dst.Write(passHeader); // Placeholder
                WriteName(dst, assetStartOffset, descriptor.Name, ref passHeader.nameOffset, ref passHeader.nameSize);
                var passDataStart = dst.Position;

                await WriteMultiVariantPass(
                    dst, assetStartOffset, passDataStart, passHeader.entryPointCount, permutations,
                    perm => CompileComputeShaderEntries(descriptor, configTemplate, perm),
                    cancellationToken);

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
                break;
            }

            case AssetType.WorkGraph:
            {
                var syntax = DSLShaderCompiler.ParseComputeShaderSyntax(codeStr).GetValueOrThrow();
                var semantics = DSLShaderCompiler.GetShaderSemantics(syntax).GetValueOrThrow();

                var descriptor = DSLShaderCompiler.ResolveShader(semantics, src).GetValueOrThrow();

                if (configTemplate.shaderModel < ShaderModel.SM_6_8)
                {
                    Logger.Warning($"Shader '{descriptor.Name}' is a work graph shader and requires at least Shader Model 6.8. Overriding shader model to SM_6_8.");
                    configTemplate.shaderModel = ShaderModel.SM_6_8;
                }

                var assetStartOffset = dst.Position;
                var header = new ShaderContentHeader
                {
                    shaderType = ShaderType.WorkGraph,
                    passCount = 1,
                    propertyBufferSize = descriptor.PropertyBufferSize,
                    shaderId = ShaderIdentity.GetShaderId(descriptor.Name),
                    familyId = ShaderIdentity.GetShaderId(descriptor.Name),
                    layoutHash = GetLayoutHash(descriptor.PropertyBufferSize, descriptor.Name),
                };

                dst.Write(header);
                WriteName(dst, assetStartOffset, descriptor.Name, ref header.nameOffset, ref header.nameSize);

                var permutations = GenerateKeywordPermutations(descriptor.KeywordGroups);
                var passHeaderOffset = dst.Position;
                var passHeader = new ShaderContentHeader.PassHeader
                {
                    entryPointCount = 1,
                    semantic = PassSemantic.Custom,
                    stageMask = ShaderStageMask.None,
                    variantCount = (ushort)permutations.Count,
                    passId = ShaderIdentity.GetPassId(header.shaderId, 0),
                    localPipeline = PipelineState.Default,
                };

                dst.Write(passHeader); // Placeholder
                WriteName(dst, assetStartOffset, descriptor.Name, ref passHeader.nameOffset, ref passHeader.nameSize);
                var passDataStart = dst.Position;

                await WriteMultiVariantPass(
                    dst, assetStartOffset, passDataStart, 1, permutations,
                    perm =>
                    {
                        var shaderCode = descriptor.ShaderCodes.Length > 0 ? descriptor.ShaderCodes[0] : default;
                        var activeDefines = descriptor.Defines.Concat(configTemplate.defines).Concat(perm);
                        var config = configTemplate with
                        {
                            stage = ShaderStage.Library,
                            defines = activeDefines,
                            entryPoint = string.Empty,
                            shaderCode = shaderCode.code,
                        };

                        var libraryByteCode = CompileStage(in config);
                        return new List<(ShaderStage, UnsafeArray<byte>)> { (ShaderStage.Library, libraryByteCode) };
                    },
                    cancellationToken);

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
                break;
            }

            default:
                throw new NotSupportedException($"Unsupported shader file extension: {ext}");
        }
    }
}

[AssetBaker(Extensions = [".gcomp"], Type = AssetType.ComputeShader, SettingsType = typeof(ShaderBakeSettings), SettingsVersion = ShaderBakeSettings.VERSION)]
internal class ComputeShaderBaker : IAssetBaker, IAssetDependencyScanner
{
    public async Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        await ShaderBaker.s_compileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await ShaderBaker.BakeAssetCoreAsync(src, AssetType.ComputeShader, dst, settings, ctx, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ShaderBaker.s_compileLock.Release();
        }
    }

    public IEnumerable<string> ScanDependencies(string sourceFile, IBakeSettings settings, AssetBakerContext ctx)
    {
        return ShaderBaker.FindDependencies(sourceFile, ctx);
    }
}

[AssetBaker(Extensions = [".ggraph"], Type = AssetType.WorkGraph, SettingsType = typeof(ShaderBakeSettings), SettingsVersion = ShaderBakeSettings.VERSION)]
internal class WorkGraphBaker : IAssetBaker, IAssetDependencyScanner
{
    public async Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        await ShaderBaker.s_compileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await ShaderBaker.BakeAssetCoreAsync(src, AssetType.WorkGraph, dst, settings, ctx, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ShaderBaker.s_compileLock.Release();
        }
    }

    public IEnumerable<string> ScanDependencies(string sourceFile, IBakeSettings settings, AssetBakerContext ctx)
    {
        return ShaderBaker.FindDependencies(sourceFile, ctx);
    }
}