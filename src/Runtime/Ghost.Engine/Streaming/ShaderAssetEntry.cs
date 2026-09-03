using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Ghost.Graphics.Services;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Streaming;
internal unsafe class ShaderAssetEntry : AssetEntry, ILoadableAssetEntry, IShaderCommitableAssetEntry
{
    private Handle<Shader> _actualHandle;
    private MemoryBlock _payload;

    public ShaderAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, AssetType assetType, Guid[] dependencies)
        : base(manager, resourceDatabase, resourceManager, assetId, assetType, dependencies)
    {
        if (!manager.ShaderVariants.TryGetShaderHandle(assetId, out _actualHandle))
        {
            throw new InvalidDataException($"Shader asset {assetId} is missing from the runtime shader catalog.");
        }
    }

    protected override void OnReleaseResource()
    {
        if (_payload.IsCreated)
        {
            _payload.Dispose();
        }
    }

    private static bool IsRangeValid(long offset, long size, long length)
    {
        return offset >= 0 && size >= 0 && offset <= length && size <= length - offset;
    }

    private static T ReadAt<T>(byte* payload, long offset, long payloadSize)
        where T : unmanaged
    {
        if (!IsRangeValid(offset, sizeof(T), payloadSize))
        {
            throw new InvalidDataException("Shader payload contains an out-of-range structure.");
        }

        return Unsafe.ReadUnaligned<T>(payload + offset);
    }

    public override void ReadAssetData(Span<byte> dst)
    {
        Logger.DebugAssert(dst.Length == sizeof(Handle<Shader>));
        Logger.DebugAssert(_actualHandle.IsValid);

        ref var address = ref MemoryMarshal.GetReference(dst);
        Unsafe.WriteUnaligned(ref address, _actualHandle);
    }

    public override void ReadAssetData<T>(ref T dst)
    {
        Logger.DebugAssert(typeof(T) == typeof(Handle<Shader>));
        Logger.DebugAssert(_actualHandle.IsValid);

        dst = Unsafe.BitCast<Handle<Shader>, T>(_actualHandle);
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
                header.shaderType != ShaderType.Graphics)
            {
                return Result.Failure($"Shader asset {AssetId} uses an unsupported content format.");
            }

            if (header.passCount == 0 || header.passCount > 16 ||
                !IsRangeValid(header.nameOffset, header.nameSize, payloadSize))
            {
                return Result.Failure($"Shader asset {AssetId} contains invalid header ranges.");
            }

            if (!Manager.ShaderVariants.TryGetVariantIndex(AssetId, out var variantIndex))
            {
                return Result.Failure($"Shader asset {AssetId} is missing from the runtime shader catalog.");
            }

            ref readonly var variant = ref Manager.ShaderVariants.GetVariant(variantIndex);
            ref readonly var shader = ref ResourceManager.GetShaderReference(_actualHandle).Value;
            if (header.shaderId != variant.ShaderId ||
                header.layoutHash != variant.LayoutHash ||
                header.propertyBufferSize != shader.PropertyBufferSize ||
                header.passCount != shader.PassCount)
            {
                return Result.Failure($"Shader asset {AssetId} does not match its catalog metadata.");
            }

            var passOffset = header.nameOffset + header.nameSize;
            for (var passIndex = 0; passIndex < header.passCount; passIndex++)
            {
                var pass = ReadAt<ShaderContentHeader.PassHeader>(payload, passOffset, payloadSize);
                if (!IsRangeValid(pass.nameOffset, pass.nameSize, payloadSize) ||
                    !IsRangeValid(pass.dataOffset, pass.dataSize, payloadSize) ||
                    pass.entryPointCount == 0 || pass.entryPointCount > 8)
                {
                    return Result.Failure($"Shader asset {AssetId} contains invalid pass {passIndex} ranges.");
                }

                var entryHeadersSize = (long)pass.entryPointCount * sizeof(ShaderContentHeader.EntryPointHeader);
                if (entryHeadersSize > pass.dataSize)
                {
                    return Result.Failure($"Shader asset {AssetId} contains an invalid entry-point table.");
                }

                for (var entryIndex = 0; entryIndex < pass.entryPointCount; entryIndex++)
                {
                    var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(
                        payload,
                        pass.dataOffset + (entryIndex * sizeof(ShaderContentHeader.EntryPointHeader)),
                        payloadSize);
                    if (!IsRangeValid(entry.byteCodeOffset, entry.byteCodeSize, pass.dataSize - entryHeadersSize))
                    {
                        return Result.Failure($"Shader asset {AssetId} contains invalid bytecode range for pass {passIndex}.");
                    }
                }

                passOffset = pass.dataOffset + pass.dataSize;
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            if (_payload.IsCreated)
            {
                _payload.Dispose();
            }

            return Result.Failure($"Failed to stage shader asset {AssetId}: {ex.Message}");
        }
    }

    public Result CommitShaderBytecode(ShaderLibrary shaderLibrary)
    {
        if (!_payload.IsCreated)
        {
            return Result.Failure($"Shader asset {AssetId} has no staged payload.");
        }

        try
        {
            var payload = (byte*)_payload.GetUnsafePtr();
            var payloadSize = (long)_payload.Size;
            var header = ReadAt<ShaderContentHeader>(payload, 0, payloadSize);
            var passOffset = header.nameOffset + header.nameSize;
            Span<ShaderByteCode> entries = stackalloc ShaderByteCode[8];

            for (var passIndex = 0; passIndex < header.passCount; passIndex++)
            {
                var pass = ReadAt<ShaderContentHeader.PassHeader>(payload, passOffset, payloadSize);
                var entryOffset = pass.dataOffset;
                for (var entryIndex = 0; entryIndex < pass.entryPointCount; entryIndex++)
                {
                    var entryHeaderOffset = entryOffset + (entryIndex * sizeof(ShaderContentHeader.EntryPointHeader));
                    var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(payload, entryHeaderOffset, payloadSize);
                    if (!IsRangeValid(entry.byteCodeOffset, entry.byteCodeSize, pass.dataSize - (entryOffset - pass.dataOffset)))
                    {
                        return Result.Failure($"Shader asset {AssetId} contains invalid bytecode range for pass {passIndex}.");
                    }

                    entries[entryIndex] = new ShaderByteCode
                    {
                        pCode = payload + entryOffset + entry.byteCodeOffset,
                        size = (ulong)entry.byteCodeSize,
                    };
                }

                shaderLibrary.CacheCompiledResult(header.shaderId, passIndex, entries[..(int)pass.entryPointCount]);
                passOffset = pass.dataOffset + pass.dataSize;
            }

            Manager.ShaderVariants.PublishBytecodeReady(AssetId);
            _payload.Dispose();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to commit shader asset {AssetId}: {ex.Message}");
        }
    }
}
