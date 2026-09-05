using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Services;

internal struct ShaderCache : IDisposable
{
    public MemoryBlock byteCode;
    public ulong compiledHash;

    public void Dispose()
    {
        byteCode.Dispose();
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

    private struct CacheEntry : IDisposable
    {
        public UnsafeArray<ShaderCache> cache;

        public readonly void Dispose()
        {
            for (var i = 0; i < cache.Length; i++)
            {
                cache[i].Dispose();
            }

            cache.Dispose();
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

    private readonly string _cacheDirectory;
    private readonly IShaderCompilationBridge? _shaderCompilationBridge;
    private readonly IPipelineLibrary? _pipelineLibrary;
    private ulong _currentFrame;
    internal int RetiredCacheCount => _retiredShaderCaches.Count + _retiredCacheEntries.Count;

    internal ShaderLibrary(IShaderCompilationBridge? shaderCompilationBridge, IPipelineLibrary? pipelineLibrary, string cacheDirectory)
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
        var hashString = hash.ToString("X16");
        var folderName = hashString[..2];
        var folderPath = Path.Combine(_cacheDirectory, folderName);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        return Path.Combine(folderPath, $"shader_cache_{hashString}.bin");
    }

    internal static void ParseCacheData(MemoryBlock data, out CacheHeader header, out ReadOnlySpan<ulong> offsets, out ReadOnlySpan<byte> byteCodes)
    {
        Logger.DebugAssert(data.IsCreated);

        var reader = new SpanReader(data.AsSpan<byte>());
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
            entry.cache = new UnsafeArray<ShaderCache>(index + 1, AllocationHandle.Persistent);
        }
        else if (index >= entry.cache.Length)
        {
            var newCaches = new UnsafeArray<ShaderCache>(index + 1, AllocationHandle.Persistent);
            for (var i = 0; i < entry.cache.Length; i++)
            {
                newCaches[i] = entry.cache[i];
            }

            entry.cache.Dispose();
            entry.cache = newCaches;
        }

        var oldCache = entry.cache[index];
        entry.cache[index] = newCache;
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

    internal Result PublishCompiledGeneration(ulong id, ReadOnlySpan<int> passEntryOffsets, ReadOnlySpan<ShaderByteCode> byteCodes)
    {
        if (passEntryOffsets.Length < 2 || passEntryOffsets[0] != 0 || passEntryOffsets[^1] != byteCodes.Length)
        {
            return Result.Failure("Shader generation contains invalid pass entry offsets.");
        }

        for (var i = 0; i < passEntryOffsets.Length - 1; i++)
        {
            if (passEntryOffsets[i] < 0 || passEntryOffsets[i] >= passEntryOffsets[i + 1])
            {
                return Result.Failure($"Shader generation pass {i} contains no bytecode or invalid entry offsets.");
            }
        }

        var newEntry = new CacheEntry
        {
            cache = new UnsafeArray<ShaderCache>(passEntryOffsets.Length - 1, AllocationHandle.Persistent),
        };

        try
        {
            for (var passIndex = 0; passIndex < newEntry.cache.Length; passIndex++)
            {
                var start = passEntryOffsets[passIndex];
                var length = passEntryOffsets[passIndex + 1] - start;
                newEntry.cache[passIndex] = CreateShaderCache(id, passIndex, byteCodes.Slice(start, length));
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Result<ShaderCache, Error> GetCompiledCache(ulong id, int index)
    {
        if (_inMemoryCache.TryGetValue(id, out var entry) && (uint)index < (uint)entry.cache.Length)
        {
            var shaderCache = entry.cache[index];
            var result = new MemoryBlock(shaderCache.byteCode.GetUnsafePtr(), (uint)shaderCache.byteCode.Size);

            return new ShaderCache
            {
                byteCode = result,
                compiledHash = shaderCache.compiledHash,
            };
        }

        return Error.NotFound;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Result<ulong, Error> GetCompiledHash(ulong id, int passIndex)
    {
        if (_inMemoryCache.TryGetValue(id, out var entry) &&
            (uint)passIndex < (uint)entry.cache.Length &&
            entry.cache[passIndex].byteCode.IsCreated)
        {
            return entry.cache[passIndex].compiledHash;
        }

        _shaderCompilationBridge?.RequestCompilation(id, passIndex);
        return Error.NotFound;
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
        for (var i = 0; i < entry.cache.Length; i++)
        {
            var hash = entry.cache[i].compiledHash;
            if (hash == 0)
            {
                continue;
            }

            var duplicate = false;
            for (var previous = 0; previous < i; previous++)
            {
                if (entry.cache[previous].compiledHash == hash)
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
            {
                EvictPipelineIfUnused(hash);
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
            for (var i = 0; i < pair.Value.cache.Length; i++)
            {
                if (pair.Value.cache[i].byteCode.IsCreated && pair.Value.cache[i].compiledHash == compiledHash)
                {
                    return;
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
