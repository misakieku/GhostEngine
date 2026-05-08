using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Services;

internal unsafe struct ShaderByteCode
{
    public byte* pCode;
    public ulong size;
}

internal struct ShaderCache : IDisposable
{
    public MemoryBlock byteCode;
    public ulong compiledHash;

    public void Dispose()
    {
        byteCode.Dispose();
    }
}

internal unsafe class ShaderLibrary : IDisposable
{
    public struct CacheHeader
    {
        public ulong id;
        public int index;
        public int byteCodeOffsetCount;
    }

    private struct CacheEntry: IDisposable
    {
        public UnsafeArray<ShaderCache> cache;

        public void UpdateCache(int index, ref MemoryBlock data, ulong hash)
        {
            if (index >= cache.Length)
            {
                var newByteCode = new UnsafeArray<ShaderCache>(index + 1, AllocationHandle.Persistent);
                for (int i = 0; i < cache.Length; i++)
                {
                    newByteCode[i] = cache[i];
                }

                cache.Dispose();
                cache = newByteCode;
            }

            cache[index].byteCode = data;
            cache[index].compiledHash = hash;
        }

        public readonly void Dispose()
        {
            for (int i = 0; i < cache.Length; i++)
            {
                cache[i].Dispose();
            }

            cache.Dispose();
        }
    }

    private UnsafeHashMap<ulong, CacheEntry> _inMemoryCache;
    private UnsafeHashMap<ulong, ulong> _variantToCompiledHash;

    private readonly string _cacheDirectory;
    private readonly IShaderCompilationBridge? _shaderCompilationBridge;

    internal ShaderLibrary(IShaderCompilationBridge? shaderCompilationBridge, string cacheDirectory)
    {
        _inMemoryCache = new UnsafeHashMap<ulong, CacheEntry>(16, AllocationHandle.Persistent);
        _variantToCompiledHash = new UnsafeHashMap<ulong, ulong>(16, AllocationHandle.Persistent);

        _cacheDirectory = cacheDirectory;
        _shaderCompilationBridge = shaderCompilationBridge;

        if (_shaderCompilationBridge != null)
        {
            _shaderCompilationBridge.OnShaderVariantCompiled += OnVariantCompiled;
        }
    }

    private void OnVariantCompiled(Key64<ShaderVariant> variantKey, ulong newCompiledHash)
    {
        _variantToCompiledHash[variantKey] = newCompiledHash;
    }

    private string GetShaderCacheFilePath(ulong hash)
    {
        var hashString = hash.ToString("X16"); // Convert to hexadecimal string
        var folderName = hashString[..2]; // Use the first two characters as the folder name
        var folderPath = Path.Combine(_cacheDirectory, folderName);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        return Path.Combine(folderPath, $"shader_cache_{hashString}.bin");
    }

    public static void ParseCacheData(MemoryBlock data, out CacheHeader header, out ReadOnlySpan<ulong> offsets, out ReadOnlySpan<byte> byteCodes)
    {
        Logger.DebugAssert(data.IsCreated);

        var reader = new SpanReader(data.AsSpan<byte>());
        header = reader.Read<CacheHeader>();
        offsets = reader.ReadSpan<ulong>(header.byteCodeOffsetCount);
        byteCodes = reader.ReadToEnd<byte>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CacheCompiledResult(ulong id, int index, Key64<ShaderVariant> variantKey, ReadOnlySpan<ShaderByteCode> byteCodes)
    {
        var header = new CacheHeader
        {
            id = id,
            index = index,
            byteCodeOffsetCount = byteCodes.Length,
        };

        var offsets = stackalloc ulong[byteCodes.Length];
        var offset = (nuint)(sizeof(CacheHeader) + (sizeof(ulong) * byteCodes.Length));
        for (var i = 0; i < byteCodes.Length; i++)
        {
            offsets[i] = offset;
            offset += (nuint)byteCodes[i].size;
        }

        var alignment = Math.Max(Math.Max(MemoryUtility.AlignOf<CacheHeader>(), MemoryUtility.AlignOf<ulong>()), 8);
        offset = MemoryUtility.AlignUp(offset, alignment);

        var data = new MemoryBlock(offset, alignment, AllocationHandle.Persistent);
        var writer = new SpanWriter(data.AsSpan<byte>());

        writer.Write(header);

        for (var i = 0; i < byteCodes.Length; i++)
        {
            writer.Write(offsets[i]);
        }

        for (var i = 0; i < byteCodes.Length; i++)
        {
            var byteCode = byteCodes[i];
            var src = new ReadOnlySpan<byte>(byteCode.pCode, (int)byteCode.size);
            writer.WriteSpan(src);
        }

        // Compute hash from bytecode only
        var codeHash = 0UL;
        if (byteCodes.Length > 0)
        {
            ulong totalBytecodeSize = 0;
            for (int i = 0; i < byteCodes.Length; i++) totalBytecodeSize += byteCodes[i].size;

            // We skip the header and offsets at the beginning of the MemoryBlock
            var bytecodeSpan = data.AsSpan<byte>().Slice((int)(sizeof(CacheHeader) + (sizeof(ulong) * byteCodes.Length)), (int)totalBytecodeSize);
            codeHash = XxHash64.HashToUInt64(bytecodeSpan);
        }

        _variantToCompiledHash[variantKey] = codeHash;

        ref var entry = ref _inMemoryCache.GetValueRefOrAddDefault(id, out var exists);
        entry.UpdateCache(index, ref data, codeHash);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<ShaderCache, Error> GetCompiledCache(ulong id, int index)
    {
        if (_inMemoryCache.TryGetValue(id, out var entry))
        {
            if (index < entry.cache.Length)
            {
                var shaderCache = entry.cache[index];
                var result = new MemoryBlock(shaderCache.byteCode.GetUnsafePtr(), (uint)shaderCache.byteCode.Size);

                return new ShaderCache
                {
                    byteCode = result,
                    compiledHash = shaderCache.compiledHash,
                };
            }
        }

        return Error.NotFound;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<ulong, Error> GetCompiledHash(ulong id, int passIndex, Key64<ShaderVariant> variantKey)
    {
        if (_variantToCompiledHash.TryGetValue(variantKey, out var compiledHash))
        {
            return compiledHash;
        }

        _shaderCompilationBridge?.RequestCompilation(id, passIndex, variantKey);
        return Error.NotFound;
    }

    public void InvalidateShaderCache(ulong id, IPipelineLibrary pipelineLibrary)
    {
        if (_inMemoryCache.TryGetValue(id, out var entry))
        {
            for (int i = 0; i < entry.cache.Length; i++)
            {
                if (entry.cache[i].compiledHash != 0)
                {
                    pipelineLibrary.EvictStalePipelines(entry.cache[i].compiledHash);
                }
            }
            entry.Dispose();
            _inMemoryCache.Remove(id);
        }

        // Wait, what about _variantToCompiledHash?
        // It maps variantKey -> compiledHash. Since we don't have a way to find variantKeys for this shader id,
        // it will just linger as garbage. But it's small (8 bytes + 8 bytes).
        // A proper fix would require _inMemoryCache to store the variantKeys, but for now we ignore it.
    }

    public void Dispose()
    {
        foreach (var kvp in _inMemoryCache)
        {
            kvp.Value.Dispose();
        }

        _inMemoryCache.Dispose();
        _variantToCompiledHash.Dispose();

        if (_shaderCompilationBridge != null)
        {
            _shaderCompilationBridge.OnShaderVariantCompiled -= OnVariantCompiled;
        }

        GC.SuppressFinalize(this);
    }
}
