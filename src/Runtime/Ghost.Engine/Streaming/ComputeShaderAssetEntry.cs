using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Streaming;

internal unsafe class ComputeShaderAssetEntry : AssetEntry, ILoadableAssetEntry, IShaderCommitableAssetEntry
{
    private Handle<ComputeShader> _actualHandle;
    private MemoryBlock _payload;

    public ComputeShaderAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, AssetType assetType, Guid[] dependencies)
        : base(manager, resourceDatabase, resourceManager, assetId, assetType, dependencies)
    {
        if (!manager.ComputeShaders.TryGetShaderHandle(assetId, out _actualHandle))
        {
            throw new InvalidDataException($"Compute shader asset {assetId} is missing from the runtime shader catalog.");
        }
    }

    protected override void OnReleaseResource()
    {
        if (_payload.IsCreated)
        {
            _payload.Dispose();
        }
    }

    public override void ReadAssetData(Span<byte> dst)
    {
        Logger.DebugAssert(dst.Length == sizeof(Handle<ComputeShader>));
        MemoryMarshal.Write(dst, in _actualHandle);
    }

    public override void ReadAssetData<T>(ref T dst)
    {
        Logger.DebugAssert(typeof(T) == typeof(Handle<ComputeShader>));
        dst = Unsafe.BitCast<Handle<ComputeShader>, T>(_actualHandle);
    }

    public Result OnLoadContent(Stream contentStream)
    {
        try
        {
            contentStream.Position = 0;
            _payload = contentStream.ReadMemory(AllocationHandle.Persistent);
            var payload = (byte*)_payload.GetUnsafePtr();
            var payloadSize = (long)_payload.Size;
            var header = ReadAt<ShaderContentHeader>(payload, 0, payloadSize);
            if (header.magic != ShaderContentHeader.MAGIC || header.version != ShaderContentHeader.VERSION ||
                header.shaderType != ShaderType.Compute || header.passCount != 1 ||
                !IsRangeValid(header.nameOffset, header.nameSize, payloadSize))
            {
                return Result.Failure($"Compute shader asset {AssetId} uses an unsupported content format.");
            }

            if (!Manager.ComputeShaders.TryGetShaderIndex(AssetId, out var index))
            {
                return Result.Failure($"Compute shader asset {AssetId} is missing from the runtime shader catalog.");
            }

            ref readonly var record = ref Manager.ComputeShaders.GetShader(index);
            if (header.shaderId != record.ShaderId || header.layoutHash != record.LayoutHash ||
                header.propertyBufferSize != record.PropertyBufferSize ||
                header.passCount != 1)
            {
                return Result.Failure($"Compute shader asset {AssetId} does not match its catalog metadata.");
            }

            var passOffset = header.nameOffset + header.nameSize;
            var pass = ReadAt<ShaderContentHeader.PassHeader>(payload, passOffset, payloadSize);
            if (pass.entryPointCount != record.EntryPointCount || pass.entryPointCount == 0 || pass.entryPointCount > 8 ||
                !IsRangeValid(pass.nameOffset, pass.nameSize, payloadSize) ||
                !IsRangeValid(pass.dataOffset, pass.dataSize, payloadSize))
            {
                return Result.Failure($"Compute shader asset {AssetId} contains invalid entry-point metadata.");
            }

            var entryHeadersSize = (long)pass.entryPointCount * sizeof(ShaderContentHeader.EntryPointHeader);
            if (entryHeadersSize > pass.dataSize)
            {
                return Result.Failure($"Compute shader asset {AssetId} contains an invalid entry-point table.");
            }

            for (var entryIndex = 0; entryIndex < pass.entryPointCount; entryIndex++)
            {
                var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(payload, pass.dataOffset + (entryIndex * sizeof(ShaderContentHeader.EntryPointHeader)), payloadSize);
                if (!IsRangeValid(entry.byteCodeOffset, entry.byteCodeSize, pass.dataSize - entryHeadersSize))
                {
                    return Result.Failure($"Compute shader asset {AssetId} contains an invalid bytecode range.");
                }
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            if (_payload.IsCreated)
            {
                _payload.Dispose();
            }

            return Result.Failure($"Failed to stage compute shader asset {AssetId}: {ex.Message}");
        }
    }

    public Result CommitShaderBytecode(ShaderLibrary shaderLibrary)
    {
        if (!_payload.IsCreated)
        {
            return Result.Failure($"Compute shader asset {AssetId} has no staged payload.");
        }

        try
        {
            var payload = (byte*)_payload.GetUnsafePtr();
            var payloadSize = (long)_payload.Size;
            var header = ReadAt<ShaderContentHeader>(payload, 0, payloadSize);
            var pass = ReadAt<ShaderContentHeader.PassHeader>(payload, header.nameOffset + header.nameSize, payloadSize);
            Span<ShaderByteCode> entries = stackalloc ShaderByteCode[8];
            for (var entryIndex = 0; entryIndex < pass.entryPointCount; entryIndex++)
            {
                var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(payload, pass.dataOffset + (entryIndex * sizeof(ShaderContentHeader.EntryPointHeader)), payloadSize);
                entries[entryIndex] = new ShaderByteCode
                {
                    pCode = payload + pass.dataOffset + entry.byteCodeOffset,
                    size = (ulong)entry.byteCodeSize,
                };
            }

            shaderLibrary.CacheCompiledResult(header.shaderId, 0, entries[..(int)pass.entryPointCount]);
            Manager.ComputeShaders.PublishBytecodeReady(AssetId);
            _payload.Dispose();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to commit compute shader asset {AssetId}: {ex.Message}");
        }
    }

    private static bool IsRangeValid(long offset, long size, long length)
    {
        return offset >= 0 && size >= 0 && offset <= length && size <= length - offset;
    }

    private static T ReadAt<T>(byte* payload, long offset, long payloadSize) where T : unmanaged
    {
        if (!IsRangeValid(offset, sizeof(T), payloadSize))
        {
            throw new InvalidDataException("Shader payload contains an out-of-range structure.");
        }

        return Unsafe.ReadUnaligned<T>(payload + offset);
    }
}
