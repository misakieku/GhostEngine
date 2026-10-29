using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Services;

internal struct ShaderCache
{
    public MemoryBlock byteCode;
    public ulong compiledHash;

    public void Dispose()
    {
        byteCode.Dispose();
    }
}

internal struct ShaderCacheView
{
    public ReadOnlyView<byte> byteCode;
    public ulong compiledHash;
}

public readonly struct CompiledVariantData
{
    public readonly KeywordMask128 RuntimeMask;
    public readonly ShaderByteCode[] ByteCodes;

    public CompiledVariantData(KeywordMask128 runtimeMask, ShaderByteCode[] byteCodes)
    {
        RuntimeMask = runtimeMask;
        ByteCodes = byteCodes;
    }
}

public readonly struct CompiledPassData
{
    public readonly KeywordMask128 SupportedMask;
    public readonly CompiledVariantData[] Variants;

    public CompiledPassData(KeywordMask128 supportedMask, CompiledVariantData[] variants)
    {
        SupportedMask = supportedMask;
        Variants = variants;
    }
}

public unsafe class ShaderLibrary : IDisposable
{
    public struct CacheHeader
    {
        public ulong id;
        public int index;
        public int byteCodeOffsetCount;
        public int byteCodeSize;
    }

    internal struct PassVariantCache
    {
        public KeywordMask128 runtimeMask;
        public ShaderCache cache;
    }

    internal struct PassCache : IDisposable
    {
        public KeywordMask128 supportedMask;
        public UnsafeArray<PassVariantCache> variants;

        public readonly void Dispose()
        {
            if (variants.IsCreated)
            {
                for (var i = 0; i < variants.Length; i++)
                {
                    variants[i].cache.Dispose();
                }

                variants.Dispose();
            }
        }
    }

    private struct CacheEntry : IDisposable
    {
        public UnsafeArray<PassCache> passes;

        public readonly void Dispose()
        {
            if (passes.IsCreated)
            {
                for (var i = 0; i < passes.Length; i++)
                {
                    passes[i].Dispose();
                }

                passes.Dispose();
            }
        }
    }

    private struct RetiredShaderCache
    {
        public ShaderCache cache;
        public ulong retireAfterFrame;
    }

    private struct RetiredCacheEntry
    {
        public CacheEntry entry;
        public ulong retireAfterFrame;
    }

    private UnsafeHashMap<ulong, CacheEntry> _inMemoryCache;
    private UnsafeList<RetiredShaderCache> _retiredShaderCaches;
    private UnsafeList<RetiredCacheEntry> _retiredCacheEntries;

    private readonly string? _cacheDirectory;
    private readonly IShaderCompilationBridge? _shaderCompilationBridge;
    private readonly IPipelineLibrary? _pipelineLibrary;
    private ulong _currentFrame;
    internal int RetiredCacheCount => _retiredShaderCaches.Count + _retiredCacheEntries.Count;

    internal ShaderLibrary(IShaderCompilationBridge? shaderCompilationBridge, IPipelineLibrary? pipelineLibrary, string? cacheDirectory)
    {
        _inMemoryCache = new UnsafeHashMap<ulong, CacheEntry>(16, AllocationHandle.Persistent);
        _retiredShaderCaches = new UnsafeList<RetiredShaderCache>(8, AllocationHandle.Persistent);
        _retiredCacheEntries = new UnsafeList<RetiredCacheEntry>(4, AllocationHandle.Persistent);

        _cacheDirectory = cacheDirectory;
        _shaderCompilationBridge = shaderCompilationBridge;
        _pipelineLibrary = pipelineLibrary;

        if (_shaderCompilationBridge != null)
        {
            _shaderCompilationBridge.OnShaderCompiled += OnShaderCompiled;
            _shaderCompilationBridge.OnShaderInvalidated += OnShaderInvalidated;
        }
    }

    private void OnShaderCompiled(ulong shaderId, int passIndex, ReadOnlySpan<ShaderByteCode> byteCodes)
    {
        CacheCompiledResult(shaderId, passIndex, byteCodes);
    }

    private void OnShaderInvalidated(ulong shaderId)
    {
        InvalidateShaderCache(shaderId);
    }

    private string GetShaderCacheFilePath(ulong hash)
    {
        if (string.IsNullOrEmpty(_cacheDirectory))
        {
            throw new InvalidOperationException("Shader cache directory is not set.");
        }

        var hashString = hash.ToString("X16");
        var folderName = hashString[..2];
        var folderPath = Path.Combine(_cacheDirectory, folderName);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        return Path.Combine(folderPath, $"shader_cache_{hashString}.bin");
    }

    internal static void ParseCacheData(ReadOnlySpan<byte> data, out CacheHeader header, out ReadOnlySpan<ulong> offsets, out ReadOnlySpan<byte> byteCodes)
    {
        Logger.DebugAssert(!data.IsEmpty);

        var reader = new SpanReader(data);
        header = reader.Read<CacheHeader>();
        offsets = reader.ReadSpan<ulong>(header.byteCodeOffsetCount);
        byteCodes = reader.ReadSpan<byte>(header.byteCodeSize);
    }

    private static ShaderCache CreateShaderCache(ulong id, int index, ReadOnlySpan<ShaderByteCode> byteCodes)
    {
        var header = new CacheHeader
        {
            id = id,
            index = index,
            byteCodeOffsetCount = byteCodes.Length,
        };

        var offsets = stackalloc ulong[byteCodes.Length];
        var headerSize = (nuint)(sizeof(CacheHeader) + (sizeof(ulong) * byteCodes.Length));
        var offset = headerSize;
        for (var i = 0; i < byteCodes.Length; i++)
        {
            offsets[i] = offset - headerSize;
            offset += checked((nuint)byteCodes[i].size);
        }
        header.byteCodeSize = checked((int)(offset - headerSize));

        var alignment = Math.Max(Math.Max(MemoryUtility.AlignOf<CacheHeader>(), MemoryUtility.AlignOf<ulong>()), 8);
        var alignedSize = MemoryUtility.AlignUp(offset, alignment);
        MemoryBlock data = default;

        try
        {
            data = new MemoryBlock(alignedSize, alignment, AllocationHandle.Persistent);
            var writer = new SpanWriter(data.AsSpan<byte>());
            writer.Write(header);

            for (var i = 0; i < byteCodes.Length; i++)
            {
                writer.Write(offsets[i]);
            }

            for (var i = 0; i < byteCodes.Length; i++)
            {
                var byteCode = byteCodes[i];
                var src = new ReadOnlySpan<byte>(byteCode.pCode, checked((int)byteCode.size));
                writer.WriteSpan(src);
            }

            var codeHash = byteCodes.Length == 0
                ? 0UL
                : XxHash64.HashToUInt64(data.AsSpan<byte>().Slice((int)headerSize, header.byteCodeSize));
            return new ShaderCache
            {
                byteCode = data,
                compiledHash = codeHash,
            };
        }
        catch
        {
            if (data.IsCreated)
            {
                data.Dispose();
            }

            throw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void CacheCompiledResult(ulong id, int index, ReadOnlySpan<ShaderByteCode> byteCodes)
    {
        if (index < 0 || byteCodes.IsEmpty)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var newCache = CreateShaderCache(id, index, byteCodes);
        ref var entry = ref _inMemoryCache.GetValueRefOrAddDefault(id, out var exists);
        if (!exists)
        {
            entry.passes = new UnsafeArray<PassCache>(index + 1, AllocationHandle.Persistent, AllocationOption.Clear);
        }
        else if (index >= entry.passes.Length)
        {
            var newPasses = new UnsafeArray<PassCache>(index + 1, AllocationHandle.Persistent, AllocationOption.Clear);
            for (var i = 0; i < entry.passes.Length; i++)
            {
                newPasses[i] = entry.passes[i];
            }

            entry.passes.Dispose();
            entry.passes = newPasses;
        }

        ref var pass = ref entry.passes[index];
        if (!pass.variants.IsCreated || pass.variants.Length == 0)
        {
            pass.variants = new UnsafeArray<PassVariantCache>(1, AllocationHandle.Persistent, AllocationOption.Clear);
        }

        var oldCache = pass.variants[0].cache;
        pass.variants[0] = new PassVariantCache
        {
            runtimeMask = default,
            cache = newCache,
        };

        if (oldCache.byteCode.IsCreated)
        {
            EvictPipelineIfUnused(oldCache.compiledHash);
            _retiredShaderCaches.Add(new RetiredShaderCache
            {
                cache = oldCache,
                retireAfterFrame = _currentFrame,
            });
        }
    }

    internal Result PublishCompiledGeneration(ulong id, ReadOnlySpan<CompiledPassData> passes)
    {
        if (passes.IsEmpty)
        {
            return Result.Failure("Shader generation contains no passes.");
        }

        var newEntry = new CacheEntry
        {
            passes = new UnsafeArray<PassCache>(passes.Length, AllocationHandle.Persistent, AllocationOption.Clear),
        };

        try
        {
            for (var passIndex = 0; passIndex < passes.Length; passIndex++)
            {
                var passData = passes[passIndex];
                var variants = passData.Variants ?? Array.Empty<CompiledVariantData>();
                var variantCount = variants.Length;
                if (variantCount == 0)
                {
                    newEntry.Dispose();
                    return Result.Failure($"Shader generation pass {passIndex} contains no variants.");
                }

                var passCache = new PassCache
                {
                    supportedMask = passData.SupportedMask,
                    variants = new UnsafeArray<PassVariantCache>(variantCount, AllocationHandle.Persistent, AllocationOption.Clear),
                };

                for (var v = 0; v < variantCount; v++)
                {
                    var variantData = variants[v];
                    var byteCodes = variantData.ByteCodes ?? Array.Empty<ShaderByteCode>();
                    passCache.variants[v] = new PassVariantCache
                    {
                        runtimeMask = variantData.RuntimeMask,
                        cache = CreateShaderCache(id, passIndex, byteCodes),
                    };
                }

                newEntry.passes[passIndex] = passCache;
            }
        }
        catch (Exception ex)
        {
            newEntry.Dispose();
            return Result.Failure($"Failed to stage compiled shader generation: {ex.Message}");
        }

        ref var currentEntry = ref _inMemoryCache.GetValueRefOrAddDefault(id, out var exists);
        if (!exists)
        {
            currentEntry = newEntry;
            return Result.Success();
        }

        var oldEntry = currentEntry;
        currentEntry = newEntry;
        EvictPipelinesIfUnused(in oldEntry);
        _retiredCacheEntries.Add(new RetiredCacheEntry
        {
            entry = oldEntry,
            retireAfterFrame = _currentFrame,
        });
        return Result.Success();
    }

    internal Result PublishCompiledGeneration(ulong id, ReadOnlySpan<int> passEntryOffsets, ReadOnlySpan<ShaderByteCode> byteCodes)
    {
        if (passEntryOffsets.Length < 2 || passEntryOffsets[0] != 0 || passEntryOffsets[^1] != byteCodes.Length)
        {
            return Result.Failure("Shader generation contains invalid pass entry offsets.");
        }

        var passCount = passEntryOffsets.Length - 1;
        var passData = new CompiledPassData[passCount];

        for (var i = 0; i < passCount; i++)
        {
            if (passEntryOffsets[i] < 0 || passEntryOffsets[i] >= passEntryOffsets[i + 1])
            {
                return Result.Failure($"Shader generation pass {i} contains no bytecode or invalid entry offsets.");
            }

            var start = passEntryOffsets[i];
            var length = passEntryOffsets[i + 1] - start;
            var subBytes = byteCodes.Slice(start, length).ToArray();
            var variant = new CompiledVariantData(default, subBytes);
            passData[i] = new CompiledPassData(default, new[] { variant });
        }

        return PublishCompiledGeneration(id, passData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Result<ShaderCacheView, Error> GetCompiledCache(ulong id, int index, KeywordMask128 activeKeywords = default)
    {
        if (_inMemoryCache.TryGetValue(id, out var entry) && entry.passes.IsCreated && (uint)index < (uint)entry.passes.Length)
        {
            ref readonly var pass = ref entry.passes[index];
            if (!pass.variants.IsCreated || pass.variants.Length == 0)
            {
                return Error.NotFound;
            }

            var selectedIndex = 0;
            if (pass.variants.Length > 1)
            {
                var targetMask = activeKeywords & pass.supportedMask;
                for (var v = 0; v < pass.variants.Length; v++)
                {
                    if (pass.variants[v].runtimeMask == targetMask)
                    {
                        selectedIndex = v;
                        break;
                    }
                }
            }

            var shaderCache = pass.variants[selectedIndex].cache;
            if (shaderCache.byteCode.IsCreated)
            {
                return new ShaderCacheView
                {
                    byteCode = new ReadOnlyView<byte>((byte*)shaderCache.byteCode.GetUnsafePtr(), (int)shaderCache.byteCode.Size),
                    compiledHash = shaderCache.compiledHash,
                };
            }
        }

        return Error.NotFound;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Result<ulong, Error> GetCompiledHash(ulong id, int passIndex, KeywordMask128 activeKeywords = default)
    {
        if (_inMemoryCache.TryGetValue(id, out var entry) && entry.passes.IsCreated && (uint)passIndex < (uint)entry.passes.Length)
        {
            ref readonly var pass = ref entry.passes[passIndex];
            if (!pass.variants.IsCreated || pass.variants.Length == 0)
            {
                return Error.NotFound;
            }

            if (pass.variants.Length == 1)
            {
                if (pass.variants[0].cache.byteCode.IsCreated)
                {
                    return pass.variants[0].cache.compiledHash;
                }
            }
            else
            {
                var targetMask = activeKeywords & pass.supportedMask;
                for (var v = 0; v < pass.variants.Length; v++)
                {
                    if (pass.variants[v].runtimeMask == targetMask && pass.variants[v].cache.byteCode.IsCreated)
                    {
                        return pass.variants[v].cache.compiledHash;
                    }
                }

                if (pass.variants[0].cache.byteCode.IsCreated)
                {
                    return pass.variants[0].cache.compiledHash;
                }
            }
        }

        _shaderCompilationBridge?.RequestCompilation(id, passIndex);
        return Error.NotFound;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Result<ShaderCacheView, Error> GetCompiledCache(ulong id, int index)
    {
        return GetCompiledCache(id, index, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Result<ulong, Error> GetCompiledHash(ulong id, int passIndex)
    {
        return GetCompiledHash(id, passIndex, default);
    }

    internal void BeginFrame(ulong submittedFrame)
    {
        _currentFrame = submittedFrame;
    }

    internal void EndFrame(ulong completedFrame)
    {
        for (var i = _retiredShaderCaches.Count - 1; i >= 0; i--)
        {
            ref var retired = ref _retiredShaderCaches[i];
            if (completedFrame < retired.retireAfterFrame)
            {
                continue;
            }

            retired.cache.Dispose();
            _retiredShaderCaches.RemoveAtSwapBack(i);
        }

        for (var i = _retiredCacheEntries.Count - 1; i >= 0; i--)
        {
            ref var retired = ref _retiredCacheEntries[i];
            if (completedFrame < retired.retireAfterFrame)
            {
                continue;
            }

            retired.entry.Dispose();
            _retiredCacheEntries.RemoveAtSwapBack(i);
        }
    }

    internal void InvalidateShaderCache(ulong id)
    {
        if (!_inMemoryCache.TryGetValue(id, out var entry))
        {
            return;
        }

        _inMemoryCache.Remove(id);
        EvictPipelinesIfUnused(in entry);
        _retiredCacheEntries.Add(new RetiredCacheEntry
        {
            entry = entry,
            retireAfterFrame = _currentFrame,
        });
    }

    private void EvictPipelinesIfUnused(scoped in CacheEntry entry)
    {
        if (!entry.passes.IsCreated)
        {
            return;
        }

        for (var p = 0; p < entry.passes.Length; p++)
        {
            ref readonly var pass = ref entry.passes[p];
            if (!pass.variants.IsCreated)
            {
                continue;
            }

            for (var v = 0; v < pass.variants.Length; v++)
            {
                var hash = pass.variants[v].cache.compiledHash;
                if (hash == 0)
                {
                    continue;
                }

                var duplicate = false;
                for (var prevP = 0; prevP <= p; prevP++)
                {
                    ref readonly var checkPass = ref entry.passes[prevP];
                    if (!checkPass.variants.IsCreated)
                    {
                        continue;
                    }

                    var maxV = (prevP == p) ? v : checkPass.variants.Length;
                    for (var prevV = 0; prevV < maxV; prevV++)
                    {
                        if (checkPass.variants[prevV].cache.compiledHash == hash)
                        {
                            duplicate = true;
                            break;
                        }
                    }

                    if (duplicate)
                    {
                        break;
                    }
                }

                if (!duplicate)
                {
                    EvictPipelineIfUnused(hash);
                }
            }
        }
    }

    private void EvictPipelineIfUnused(ulong compiledHash)
    {
        if (_pipelineLibrary == null || compiledHash == 0)
        {
            return;
        }

        foreach (var pair in _inMemoryCache)
        {
            if (!pair.Value.passes.IsCreated)
            {
                continue;
            }

            for (var p = 0; p < pair.Value.passes.Length; p++)
            {
                ref readonly var pass = ref pair.Value.passes[p];
                if (!pass.variants.IsCreated)
                {
                    continue;
                }

                for (var v = 0; v < pass.variants.Length; v++)
                {
                    if (pass.variants[v].cache.byteCode.IsCreated && pass.variants[v].cache.compiledHash == compiledHash)
                    {
                        return;
                    }
                }
            }
        }

        _pipelineLibrary.EvictStalePipelines(compiledHash);
    }

    public void Dispose()
    {
        if (_shaderCompilationBridge != null)
        {
            _shaderCompilationBridge.OnShaderCompiled -= OnShaderCompiled;
            _shaderCompilationBridge.OnShaderInvalidated -= OnShaderInvalidated;
        }

        foreach (var pair in _inMemoryCache)
        {
            pair.Value.Dispose();
        }
        _inMemoryCache.Dispose();

        for (var i = 0; i < _retiredShaderCaches.Count; i++)
        {
            _retiredShaderCaches[i].cache.Dispose();
        }
        _retiredShaderCaches.Dispose();

        for (var i = 0; i < _retiredCacheEntries.Count; i++)
        {
            _retiredCacheEntries[i].entry.Dispose();
        }
        _retiredCacheEntries.Dispose();

        GC.SuppressFinalize(this);
    }
}
