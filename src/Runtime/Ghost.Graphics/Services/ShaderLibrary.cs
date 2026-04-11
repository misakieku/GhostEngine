using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Services;

internal class ShaderLibrary : IDisposable
{
    private struct CacheEntry: IDisposable
    {
        public UnsafeArray<UnsafeArray<byte>> byteCode;

        public void Insert(int index, ReadOnlySpan<byte> data)
        {
            if (index >= byteCode.Length)
            {
                var newByteCode = new UnsafeArray<UnsafeArray<byte>>(index + 1, Allocator.Persistent);
                for (int i = 0; i < byteCode.Length; i++)
                {
                    newByteCode[i] = byteCode[i];
                }

                byteCode.Dispose();
                byteCode = newByteCode;
            }

            var byteData = new UnsafeArray<byte>(data.Length, Allocator.Persistent);
            byteData.CopyFrom(data);
            byteCode[index] = byteData;
        }

        public readonly void Dispose()
        {
            for (int i = 0; i < byteCode.Length; i++)
            {
                byteCode[i].Dispose();
            }
        }
    }

    private UnsafeHashMap<ulong, CacheEntry> _inMemoryCache;

    private readonly string _cacheDirectory;
    private readonly IShaderCompilationBridge? _shaderCompilationBridge;

    internal ShaderLibrary(IShaderCompilationBridge? shaderCompilationBridge, string cacheDirectory)
    {
        _inMemoryCache = new UnsafeHashMap<ulong, CacheEntry>(16, Allocator.Persistent);

        _cacheDirectory = cacheDirectory;
        _shaderCompilationBridge = shaderCompilationBridge;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CacheCompiledResult(ulong id, int index, ReadOnlySpan<byte> byteCode)
    {
        var data = new UnsafeArray<byte>(byteCode.Length, Allocator.Persistent);
        data.CopyFrom(byteCode);

        ref var entry = ref _inMemoryCache.GetValueRefOrAddDefault(id, out var exists);
        entry.Insert(index, byteCode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<UnsafeArray<byte>, Error> GetCache(ulong id, int index, AllocationHandle allocationHandle)
    {
        if (_inMemoryCache.TryGetValue(id, out var entry))
        {
            if (index < entry.byteCode.Length)
            {
                var byteCode = entry.byteCode[index];
                var result = new UnsafeArray<byte>(byteCode.Length, allocationHandle);
                result.CopyFrom(byteCode);
                return result;
            }
        }

        return Error.NotFound;
    }

    public void Dispose()
    {
        foreach (var kvp in _inMemoryCache)
        {
            kvp.Value.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
