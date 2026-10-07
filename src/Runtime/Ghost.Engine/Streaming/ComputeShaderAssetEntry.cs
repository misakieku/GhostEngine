using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Streaming;

internal unsafe class ComputeShaderAssetEntry : AssetEntry, ILoadableAssetEntry, IShaderCommitableAssetEntry
{
    private const int MAX_ENTRY_POINT_COUNT = 8;

    private Handle<ComputeShader> _actualHandle;
    private MemoryBlock _payload;
    private bool _bytecodeReady;

    internal override AssetState FailureState => _bytecodeReady ? AssetState.Ready : AssetState.Failed;

    public ComputeShaderAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, AssetType assetType, Guid[] dependencies)
        : base(manager, resourceDatabase, resourceManager, assetId, assetType, dependencies)
    {
        _actualHandle = resourceManager.CreateComputeShader(null);
        if (_actualHandle.IsInvalid)
        {
            throw new InvalidOperationException($"Failed to allocate compute shader handle for '{assetId}'.");
        }
    }

    protected override void OnReleaseResource()
    {
        DiscardStagedPayload();
        if (_actualHandle.IsValid)
        {
            ResourceManager.ReleaseComputeShader(_actualHandle);
            _actualHandle = default;
        }
    }

    private void DiscardStagedPayload()
    {
        if (_payload.IsCreated)
        {
            _payload.Dispose();
            _payload = default;
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

    public Result OnLoadContent([Owner] Stream contentStream, long contentSize)
    {
        MemoryBlock stagedPayload = default;
        try
        {
            stagedPayload = contentStream.ReadMemory(contentSize, AllocationHandle.Persistent);

            var validation = ValidatePayload(stagedPayload, AssetId);
            if (validation.IsFailure)
            {
                stagedPayload.Dispose();
                return validation;
            }

            DiscardStagedPayload();
            _payload = stagedPayload;
            stagedPayload = default;
            return Result.Success();
        }
        catch (Exception ex)
        {
            if (stagedPayload.IsCreated)
            {
                stagedPayload.Dispose();
            }

            return Result.Failure($"Failed to stage compute shader asset {AssetId}: {ex.Message}");
        }
        finally
        {
            contentStream.Dispose();
        }
    }

    private static Result ValidatePayload(MemoryBlock payloadBlock, Guid assetId)
    {
        var payload = (byte*)payloadBlock.GetUnsafePtr();
        var payloadSize = (long)payloadBlock.Size;
        var header = ReadAt<ShaderContentHeader>(payload, 0, payloadSize);
        if (header.magic != ShaderContentHeader.MAGIC || header.version != ShaderContentHeader.VERSION ||
            header.shaderType != ShaderType.Compute || header.passCount != 1 ||
            !IsRangeValid(header.nameOffset, header.nameSize, payloadSize))
        {
            return Result.Failure($"Compute shader asset {assetId} uses an unsupported content format.");
        }

        var passOffset = header.nameOffset + header.nameSize;
        var pass = ReadAt<ShaderContentHeader.PassHeader>(payload, passOffset, payloadSize);
        if (pass.entryPointCount == 0 || pass.entryPointCount > MAX_ENTRY_POINT_COUNT ||
            !IsRangeValid(pass.nameOffset, pass.nameSize, payloadSize) ||
            !IsRangeValid(pass.dataOffset, pass.dataSize, payloadSize))
        {
            return Result.Failure($"Compute shader asset {assetId} contains invalid entry-point metadata.");
        }

        if (pass.stageMask != ShaderStageMask.Compute)
        {
            return Result.Failure($"Compute shader asset {assetId} is incompatible with compute stage.");
        }

        var entryHeadersSize = pass.entryPointCount * sizeof(ShaderContentHeader.EntryPointHeader);
        if (entryHeadersSize > pass.dataSize)
        {
            return Result.Failure($"Compute shader asset {assetId} contains an invalid entry-point table.");
        }

        for (var entryIndex = 0; entryIndex < pass.entryPointCount; entryIndex++)
        {
            var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(
                payload,
                pass.dataOffset + (entryIndex * sizeof(ShaderContentHeader.EntryPointHeader)),
                payloadSize);
            var isPooled = entry.bytecodeHash != 0;
            if (entry.stage != ShaderStage.ComputeShader ||
                (!isPooled && (entry.byteCodeOffset < entryHeadersSize ||
                 !IsRangeValid(entry.byteCodeOffset, entry.byteCodeSize, pass.dataSize))))
            {
                return Result.Failure($"Compute shader asset {assetId} contains incompatible entry point {entryIndex}.");
            }
        }

        return Result.Success();
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
            var name = System.Text.Encoding.UTF8.GetString(payload + header.nameOffset, (int)header.nameSize);

            var descriptor = new ComputeShaderDescriptor
            {
                Name = name,
                PropertyBufferSize = header.propertyBufferSize,
                ShaderCodes = new ShaderCode[pass.entryPointCount],
                Defines = Array.Empty<string>(),
            };

            ref var csRef = ref ResourceManager.GetComputeShaderReference(_actualHandle).Value;
            csRef = new ComputeShader(descriptor);

            Span<ShaderByteCode> byteCodes = stackalloc ShaderByteCode[MAX_ENTRY_POINT_COUNT];
            Span<int> entryOffsets = stackalloc int[MAX_ENTRY_POINT_COUNT + 1];
            entryOffsets[0] = 0;

            for (var entryIndex = 0; entryIndex < pass.entryPointCount; entryIndex++)
            {
                var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(
                    payload,
                    pass.dataOffset + (entryIndex * sizeof(ShaderContentHeader.EntryPointHeader)),
                    payloadSize);

                if (entry.bytecodeHash != 0 && Manager.TryGetPooledBytecode(entry.bytecodeHash, out var pooledCode))
                {
                    byteCodes[entryIndex] = pooledCode;
                }
                else if (entry.byteCodeSize > 0 && IsRangeValid(pass.dataOffset + entry.byteCodeOffset, entry.byteCodeSize, payloadSize))
                {
                    byteCodes[entryIndex] = new ShaderByteCode
                    {
                        pCode = payload + pass.dataOffset + entry.byteCodeOffset,
                        size = (ulong)entry.byteCodeSize,
                    };
                }
                else
                {
                    DiscardStagedPayload();
                    return Result.Failure($"Compute shader asset {AssetId} could not resolve bytecode 0x{entry.bytecodeHash:X16} for entry {entryIndex}.");
                }

                entryOffsets[entryIndex + 1] = entryIndex + 1;
            }

            var publishResult = shaderLibrary.PublishCompiledGeneration(
                header.shaderId,
                entryOffsets[..((int)pass.entryPointCount + 1)],
                byteCodes[..(int)pass.entryPointCount]);
            if (publishResult.IsFailure)
            {
                DiscardStagedPayload();
                return publishResult;
            }

            _bytecodeReady = true;
            DiscardStagedPayload();
            return Result.Success();
        }
        catch (Exception ex)
        {
            DiscardStagedPayload();
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

