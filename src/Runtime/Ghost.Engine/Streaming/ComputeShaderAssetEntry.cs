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
    private AssetPayload _payload;
    private bool _bytecodeReady;

    internal override AssetState FailureState => _bytecodeReady ? AssetState.Ready : AssetState.Failed;

    public ComputeShaderAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, AssetType assetType, Guid[] dependencies, string assetName)
        : base(manager, resourceDatabase, resourceManager, assetId, assetType, dependencies, assetName)
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
        _payload.Dispose();
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

    public Result OnLoadContent([Owner] ref AssetPayload payload)
    {
        try
        {
            var validation = ValidatePayload(payload.Pointer, payload.Size, AssetId);
            if (validation.IsFailure)
            {
                payload.Dispose();
                return validation;
            }

            DiscardStagedPayload();
            _payload = payload;
            return Result.Success();
        }
        catch (Exception ex)
        {
            payload.Dispose();
            return Result.Failure($"Failed to stage compute shader asset {AssetId}: {ex.Message}");
        }
    }

    private static Result ValidatePayload(byte* payload, long payloadSize, Guid assetId)
    {
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

        if (pass.variantCount <= 1)
        {
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
        }
        else
        {
            var variantHeadersSize = pass.variantCount * sizeof(ShaderContentHeader.VariantHeader);
            if (variantHeadersSize > pass.dataSize)
            {
                return Result.Failure($"Compute shader asset {assetId} contains an invalid variant table.");
            }

            for (var v = 0; v < pass.variantCount; v++)
            {
                var vh = ReadAt<ShaderContentHeader.VariantHeader>(payload, pass.dataOffset + (v * sizeof(ShaderContentHeader.VariantHeader)), payloadSize);
                if (!IsRangeValid(vh.keywordOffset, vh.keywordSize, payloadSize) ||
                    !IsRangeValid(vh.dataOffset, vh.dataSize, payloadSize))
                {
                    return Result.Failure($"Compute shader asset {assetId} contains invalid variant {v} ranges.");
                }

                for (var entryIndex = 0; entryIndex < pass.entryPointCount; entryIndex++)
                {
                    var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(
                        payload,
                        vh.dataOffset + (entryIndex * sizeof(ShaderContentHeader.EntryPointHeader)),
                        payloadSize);
                    var isPooled = entry.bytecodeHash != 0;
                    if (entry.stage != ShaderStage.ComputeShader ||
                        (!isPooled && !IsRangeValid(vh.dataOffset + entry.byteCodeOffset, entry.byteCodeSize, payloadSize)))
                    {
                        return Result.Failure($"Compute shader asset {assetId} contains incompatible entry point {entryIndex} in variant {v}.");
                    }
                }
            }
        }

        return Result.Success();
    }

    public Result CommitShaderBytecode(ShaderLibrary shaderLibrary)
    {
        if (!_payload.IsValid)
        {
            return Result.Failure($"Compute shader asset {AssetId} has no staged payload.");
        }

        try
        {
            var payload = _payload.Pointer;
            var payloadSize = _payload.Size;
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

            var entryCount = (int)pass.entryPointCount;
            var variantCount = pass.variantCount > 0 ? (int)pass.variantCount : 1;
            var compiledVariantsPerEntry = new CompiledVariantData[entryCount][];
            for (var entryIndex = 0; entryIndex < entryCount; entryIndex++)
            {
                compiledVariantsPerEntry[entryIndex] = new CompiledVariantData[variantCount];
            }

            var supportedMask = KeywordMask128.Empty;

            if (pass.variantCount <= 1)
            {
                for (var entryIndex = 0; entryIndex < entryCount; entryIndex++)
                {
                    if (!TryResolveEntryByteCode(payload, payloadSize, pass.dataOffset, entryIndex, out var byteCode, out var failedHash))
                    {
                        DiscardStagedPayload();
                        return Result.Failure($"Compute shader asset {AssetId} could not resolve bytecode 0x{failedHash:X16} for entry {entryIndex}.");
                    }

                    compiledVariantsPerEntry[entryIndex][0] = new CompiledVariantData(default, new ShaderByteCode[] { byteCode });
                }
            }
            else
            {
                for (var v = 0; v < pass.variantCount; v++)
                {
                    var vh = ReadAt<ShaderContentHeader.VariantHeader>(payload, pass.dataOffset + (v * sizeof(ShaderContentHeader.VariantHeader)), payloadSize);
                    var kwString = vh.keywordSize > 0
                        ? System.Text.Encoding.UTF8.GetString(payload + vh.keywordOffset, (int)vh.keywordSize)
                        : string.Empty;
                    var variantMask = PipelineKeywordRegistry.ParseMask(kwString);
                    supportedMask |= variantMask;

                    for (var entryIndex = 0; entryIndex < entryCount; entryIndex++)
                    {
                        if (!TryResolveEntryByteCode(payload, payloadSize, vh.dataOffset, entryIndex, out var byteCode, out var failedHash))
                        {
                            DiscardStagedPayload();
                            return Result.Failure($"Compute shader asset {AssetId} could not resolve bytecode 0x{failedHash:X16} for entry {entryIndex} in variant {v}.");
                        }

                        compiledVariantsPerEntry[entryIndex][v] = new CompiledVariantData(variantMask, new ShaderByteCode[] { byteCode });
                    }
                }
            }

            var compiledPasses = new CompiledPassData[entryCount];
            for (var entryIndex = 0; entryIndex < entryCount; entryIndex++)
            {
                compiledPasses[entryIndex] = new CompiledPassData(supportedMask, compiledVariantsPerEntry[entryIndex]);
            }

            var publishResult = shaderLibrary.PublishCompiledGeneration(header.shaderId, compiledPasses);
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

    private bool TryResolveEntryByteCode(byte* payload, long payloadSize, long baseOffset, int entryIndex, out ShaderByteCode byteCode, out ulong failedHash)
    {
        var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(payload, baseOffset + (entryIndex * sizeof(ShaderContentHeader.EntryPointHeader)), payloadSize);
        if (entry.bytecodeHash != 0 && Manager.TryGetPooledBytecode(entry.bytecodeHash, out byteCode))
        {
            failedHash = 0;
            return true;
        }

        if (entry.byteCodeSize > 0 && IsRangeValid(baseOffset + entry.byteCodeOffset, entry.byteCodeSize, payloadSize))
        {
            byteCode = new ShaderByteCode { pCode = payload + baseOffset + entry.byteCodeOffset, size = (ulong)entry.byteCodeSize };
            failedHash = 0;
            return true;
        }

        byteCode = default;
        failedHash = entry.bytecodeHash;
        return false;
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

