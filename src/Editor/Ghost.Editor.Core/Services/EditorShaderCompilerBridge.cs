using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Editor.Core.Assets;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Ghost.Editor.Core.Services;

internal sealed class EditorShaderCompilerBridge : IShaderCompilationBridge
{
    private readonly IAssetRegistry _assetRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly IShaderCompiler _compiler;

    private readonly ConcurrentDictionary<ulong, Guid> _shaderIdToAssetId = new();
    private readonly ConcurrentDictionary<Guid, Dictionary<int, string>[]> _assetKeywordMappings = new();
    private Task? _shaderDictionaryPopulated;

    public event ShaderVariantCompiledHandler? OnShaderVariantCompiled;
    public event Action<ulong>? OnShaderInvalidated;

    public EditorShaderCompilerBridge(IAssetRegistry assetRegistry, IServiceProvider serviceProvider, IShaderCompiler shaderCompiler)
    {
        _assetRegistry = assetRegistry;
        _serviceProvider = serviceProvider;
        _compiler = shaderCompiler;

        _assetRegistry.OnAssetImported += OnAssetImported;
    }

    private void OnAssetImported(object? sender, Guid guid)
    {
        var path = _assetRegistry.GetAssetPath(guid);
        if (path != null && (path.EndsWith(".gshdr") || path.EndsWith(".gcomp")))
        {
            var result = _assetRegistry.LoadAssetAsync(guid).AsTask().Result;
            if (result.IsSuccess)
            {
                var nameHash = ExtractNameHash(result.Value);
                if (nameHash != 0)
                {
                    _shaderIdToAssetId[nameHash] = guid;
                    BuildKeywordMappings(result.Value, guid);

                    OnShaderInvalidated?.Invoke(nameHash);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ExtractNameHash(Asset asset)
    {
        if (asset is GraphicsShaderAsset graphicsAsset)
        {
            return RHIUtility.GetShaderID(graphicsAsset.Descriptor.Name);
        }

        if (asset is ComputeShaderAsset computeAsset)
        {
            return RHIUtility.GetShaderID(computeAsset.Descriptor.Name);
        }

        return 0;
    }

    private Task EnsureShaderDictionaryPopulatedAsync()
    {
        var existing = Volatile.Read(ref _shaderDictionaryPopulated);
        if (existing != null)
        {
            return existing;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var original = Interlocked.CompareExchange(ref _shaderDictionaryPopulated, tcs.Task, null);
        if (original != null)
        {
            return original;
        }

        Task.Run(async () =>
        {
            try
            {
                var catalog = _assetRegistry.GetAssetCatalog();
                var assetGuids = catalog.EnumerateByTypes(typeof(GraphicsShaderAsset).GUID, typeof(ComputeShaderAsset).GUID);

                foreach (var assetGuid in assetGuids)
                {
                    var result = await _assetRegistry.LoadAssetAsync(assetGuid);
                    if (result.IsSuccess)
                    {
                        var nameHash = ExtractNameHash(result.Value);
                        if (nameHash != 0)
                        {
                            _shaderIdToAssetId[nameHash] = assetGuid;
                            BuildKeywordMappings(result.Value, assetGuid);
                        }
                    }
                }

                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    private void BuildKeywordMappings(Asset asset, Guid assetId)
    {
        if (asset is GraphicsShaderAsset graphicsAsset)
        {
            var passes = graphicsAsset.Descriptor.Passes;
            var mappings = new Dictionary<int, string>[passes.Length];
            for (var i = 0; i < passes.Length; i++)
            {
                mappings[i] = BuildKeywordMappingFromGroups(passes[i].keywords);
            }

            _assetKeywordMappings[assetId] = mappings;
        }
        else if (asset is ComputeShaderAsset computeAsset)
        {
            var entryCount = computeAsset.Descriptor.ShaderCodes.Length;
            var mappings = new Dictionary<int, string>[entryCount];
            var sharedMapping = BuildKeywordMappingFromGroups(computeAsset.Descriptor.Keywords);
            for (var i = 0; i < entryCount; i++)
            {
                mappings[i] = sharedMapping;
            }

            _assetKeywordMappings[assetId] = mappings;
        }
    }

    private static Dictionary<int, string> BuildKeywordMappingFromGroups(KeywordsGroup[] groups)
    {
        var mapping = new Dictionary<int, string>();
        var localIndex = 0;

        foreach (var group in groups)
        {
            if (group.keywords == null)
            {
                continue;
            }

            if (group.space != KeywordSpace.Local)
            {
                continue;
            }

            foreach (var kw in group.keywords)
            {
                mapping[localIndex++] = kw;
            }
        }

        return mapping;
    }

    private static string[] BuildVariantDefines(LocalKeywordSet keywordMask, Dictionary<int, string>? keywordMapping)
    {
        if (keywordMapping == null || keywordMapping.Count == 0)
        {
            return Array.Empty<string>();
        }

        var defines = new List<string>(keywordMapping.Count);
        foreach (var (localIndex, keywordName) in keywordMapping)
        {
            if (keywordMask.IsKeywordEnabled(localIndex))
            {
                defines.Add(keywordName);
            }
        }

        return defines.ToArray();
    }

    private static ReadOnlySpan<string> CombineDefines(ReadOnlySpan<string> staticDefines, ReadOnlySpan<string> variantDefines)
    {
        if (variantDefines.Length == 0)
        {
            return staticDefines;
        }

        if (staticDefines.Length == 0)
        {
            return variantDefines;
        }

        var combined = new string[staticDefines.Length + variantDefines.Length];
        staticDefines.CopyTo(combined);
        variantDefines.CopyTo(combined.AsSpan(staticDefines.Length));
        return combined;
    }

    public void RequestCompilation(ulong shaderId, int passIndex, Key64<ShaderVariant> variantKey, LocalKeywordSet keywordMask)
    {
        Task.Run(async () =>
        {
            await EnsureShaderDictionaryPopulatedAsync();

            if (!_shaderIdToAssetId.TryGetValue(shaderId, out var assetId))
            {
                return;
            }

            var assetResult = await _assetRegistry.LoadAssetAsync(assetId);
            if (assetResult.IsFailure)
            {
                return;
            }

            Dictionary<int, string>? keywordMapping = null;
            if (_assetKeywordMappings.TryGetValue(assetId, out var mappings) && passIndex < mappings.Length)
            {
                keywordMapping = mappings[passIndex];
            }

            if (assetResult.Value is GraphicsShaderAsset graphicsAsset)
            {
                var pass = graphicsAsset.Descriptor.Passes[passIndex];
                await CompileGraphicsPassAsync(shaderId, passIndex, variantKey, keywordMask, pass, graphicsAsset.Descriptor.ShaderModel, keywordMapping);
            }
            else if (assetResult.Value is ComputeShaderAsset computeAsset)
            {
                await CompileComputePassAsync(shaderId, passIndex, variantKey, keywordMask, computeAsset.Descriptor, passIndex, keywordMapping);
            }
        });
    }

    private unsafe Task CompileGraphicsPassAsync(ulong shaderId, int passIndex, Key64<ShaderVariant> variantKey, LocalKeywordSet keywordMask, PassDescriptor descriptor, ShaderModel shaderModel, Dictionary<int, string>? keywordMapping)
    {
        var variantDefines = BuildVariantDefines(keywordMask, keywordMapping);

        var additionalConfig = new ShaderCompilationConfig
        {
            defines = variantDefines,
            model = shaderModel,
            optimizeLevel = CompilerOptimizeLevel.O3,
            options = CompilerOption.None
        };

        var compileResult = _compiler.CompileShaderPass(ref descriptor, ref additionalConfig, AllocationHandle.Persistent);
        if (compileResult.IsFailure)
        {
            Logger.Error($"Failed to compile graphics shader {shaderId}: {compileResult.Message}");
            return Task.CompletedTask;
        }

        using var compiled = compileResult.Value;

        var stageCount = 0;
        if (compiled.asResult.IsCreated)
        {
            stageCount++;
        }

        if (compiled.msResult.IsCreated)
        {
            stageCount++;
        }

        if (compiled.psResult.IsCreated)
        {
            stageCount++;
        }

        var byteCodes = stackalloc ShaderByteCode[stageCount];
        var idx = 0;
        if (compiled.asResult.IsCreated)
        {
            byteCodes[idx++] = new ShaderByteCode { pCode = (byte*)compiled.asResult.GetUnsafePtr(), size = (ulong)compiled.asResult.Length };
        }

        if (compiled.msResult.IsCreated)
        {
            byteCodes[idx++] = new ShaderByteCode { pCode = (byte*)compiled.msResult.GetUnsafePtr(), size = (ulong)compiled.msResult.Length };
        }

        if (compiled.psResult.IsCreated)
        {
            byteCodes[idx++] = new ShaderByteCode { pCode = (byte*)compiled.psResult.GetUnsafePtr(), size = (ulong)compiled.psResult.Length };
        }

        OnShaderVariantCompiled?.Invoke(shaderId, passIndex, variantKey, new ReadOnlySpan<ShaderByteCode>(byteCodes, stageCount));

        return Task.CompletedTask;
    }

    private unsafe Task CompileComputePassAsync(ulong shaderId, int passIndex, Key64<ShaderVariant> variantKey, LocalKeywordSet keywordMask, ComputeShaderDescriptor descriptor, int entryIndex, Dictionary<int, string>? keywordMapping)
    {
        var variantDefines = BuildVariantDefines(keywordMask, keywordMapping);
        var fullDefines = CombineDefines(descriptor.Defines, variantDefines);

        var code = descriptor.ShaderCodes[entryIndex];
        var config = new ShaderCompilationConfig
        {
            shaderCode = code.code,
            entryPoint = code.entryPoint,
            stage = ShaderStage.ComputeShader,
            defines = fullDefines,
            model = descriptor.ShaderModel,
            optimizeLevel = CompilerOptimizeLevel.O3,
            options = CompilerOption.None
        };

        var compileResult = _compiler.Compile(ref config, AllocationHandle.Persistent);
        if (compileResult.IsFailure)
        {
            Logger.Error($"Failed to compile compute shader {shaderId}: {compileResult.Message}");
            return Task.CompletedTask;
        }

        using var bytecodeArray = compileResult.Value;

        var byteCode = new ShaderByteCode
        {
            pCode = (byte*)bytecodeArray.GetUnsafePtr(),
            size = (ulong)bytecodeArray.Length
        };

        OnShaderVariantCompiled?.Invoke(shaderId, passIndex, variantKey, new ReadOnlySpan<ShaderByteCode>(ref byteCode));

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _assetRegistry.OnAssetImported -= OnAssetImported;
    }
}
