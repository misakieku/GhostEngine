using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Streaming;

/// <summary>
/// Contiguous in-memory storage for deduplicated DXIL shader bytecodes across all game assets.
/// </summary>
public sealed unsafe class ShaderBytecodePool : IDisposable
{
    public const uint MAGIC = ShaderBytecodePoolConstants.MAGIC;
    public static readonly Guid POOL_ASSET_ID = ShaderBytecodePoolConstants.POOL_ASSET_ID;
    public const string POOL_ASSET_KEY = ShaderBytecodePoolConstants.POOL_ASSET_KEY;

    private MemoryBlock _payload;
    private readonly Dictionary<ulong, (long offset, uint size)> _entries = new();
    private bool _disposed;

    public int Count => _entries.Count;

    public ShaderBytecodePool(Stream stream)
    {
        _payload = stream.ReadMemory(AllocationHandle.Persistent);
        var ptr = (byte*)_payload.GetUnsafePtr();
        var totalSize = (long)_payload.Size;

        if (totalSize < 8)
        {
            throw new InvalidDataException("Shader bytecode pool is too small.");
        }

        var magic = Unsafe.ReadUnaligned<uint>(ptr);
        if (magic != MAGIC)
        {
            throw new InvalidDataException($"Invalid bytecode pool magic: 0x{magic:X8}");
        }

        var count = Unsafe.ReadUnaligned<uint>(ptr + 4);
        var offset = 8L;

        for (var i = 0; i < count; i++)
        {
            if (offset + 12 > totalSize)
            {
                throw new InvalidDataException("Unexpected end of bytecode pool.");
            }

            var hash = Unsafe.ReadUnaligned<ulong>(ptr + offset);
            var size = Unsafe.ReadUnaligned<uint>(ptr + offset + 8);
            offset += 12;

            if (offset + size > totalSize)
            {
                throw new InvalidDataException("Bytecode payload out of range in pool.");
            }

            _entries[hash] = (offset, size);
            offset += size;
        }
    }

    public bool TryGetBytecode(ulong hash, out ShaderByteCode byteCode)
    {
        if (_payload.IsCreated && _entries.TryGetValue(hash, out var info))
        {
            var ptr = (byte*)_payload.GetUnsafePtr();
            byteCode = new ShaderByteCode
            {
                pCode = ptr + info.offset,
                size = info.size
            };
            return true;
        }

        byteCode = default;
        return false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_payload.IsCreated)
        {
            _payload.Dispose();
            _payload = default;
        }

        _entries.Clear();
        _disposed = true;
    }
}
