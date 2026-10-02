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
    private const int MAX_ENTRY_POINT_COUNT = 8;

    private readonly ShaderCatalogEntry _catalogEntry;
    private Handle<ComputeShader> _actualHandle;
    private MemoryBlock _payload;
    private bool _bytecodeReady;

    internal override AssetState FailureState => _bytecodeReady ? AssetState.Ready : AssetState.Failed;

    public ComputeShaderAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, AssetType assetType, Guid[] dependencies)
        : base(manager, resourceDatabase, resourceManager, assetId, assetType, dependencies)
    {
        var catalog = manager.ContentProvider.ShaderCatalog;
        ShaderCatalogEntry? entry = null;
        for (var i = 0; i < catalog.Count; i++)
        {
            if (catalog[i].AssetId == assetId)
            {
                entry = catalog[i];
                break;
            }
        }

        if (entry == null)
        {
            throw new InvalidDataException($"Compute shader asset {assetId} is missing from the runtime shader catalog.");
        }

        if (entry.Passes.Length != 1 || entry.Passes[0].EntryPointCount == 0 || entry.Passes[0].EntryPointCount > MAX_ENTRY_POINT_COUNT)
        {
            throw new InvalidDataException($"Compute shader '{entry.Name}' must have one to eight entry points.");
        }

        _catalogEntry = entry;

        var descriptor = new ComputeShaderDescriptor
        {
            Name = entry.Name,
            PropertyBufferSize = entry.PropertyBufferSize,
            ShaderModel = entry.ShaderModel,
            ShaderCodes = new ShaderCode[entry.Passes[0].EntryPointCount],
            Defines = Array.Empty<string>(),
        };

        _actualHandle = resourceManager.CreateComputeShader(descriptor);
        if (_actualHandle.IsInvalid)
        {
            throw new InvalidOperationException($"Failed to register compute shader metadata for '{entry.Name}'.");
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

    public Result OnLoadContent(Stream contentStream)
    {
        MemoryBlock stagedPayload = default;
        try
        {
            stagedPayload = contentStream.ReadMemory(AllocationHandle.Persistent);

            var validation = ValidatePayload(stagedPayload, AssetId, _catalogEntry);
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
    }

    internal static Result ValidatePayload(MemoryBlock payloadBlock, Guid assetId, ShaderCatalogEntry catalogEntry)
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

        if (catalogEntry.Passes.Length != 1 ||
            header.shaderId != catalogEntry.ShaderId ||
            header.familyId != catalogEntry.FamilyId ||
            header.layoutHash != catalogEntry.LayoutHash ||
            header.propertyBufferSize != catalogEntry.PropertyBufferSize ||
            header.shaderModel != catalogEntry.ShaderModel)
        {
            return Result.Failure($"Compute shader asset {assetId} does not match its catalog metadata.");
        }

        var passOffset = header.nameOffset + header.nameSize;
        var pass = ReadAt<ShaderContentHeader.PassHeader>(payload, passOffset, payloadSize);
        ref readonly var catalogPass = ref catalogEntry.Passes[0];
        if (pass.entryPointCount == 0 || pass.entryPointCount > MAX_ENTRY_POINT_COUNT ||
            !IsRangeValid(pass.nameOffset, pass.nameSize, payloadSize) ||
            !IsRangeValid(pass.dataOffset, pass.dataSize, payloadSize))
        {
            return Result.Failure($"Compute shader asset {assetId} contains invalid entry-point metadata.");
        }

        if (pass.entryPointCount != catalogPass.EntryPointCount ||
            pass.semantic != catalogPass.Semantic ||
            pass.stageMask != ShaderStageMask.Compute ||
            pass.stageMask != catalogPass.StageMask ||
            pass.passId != catalogPass.PassId ||
            pass.localPipeline.GetHashCode64() != catalogPass.LocalPipeline.GetHashCode64())
        {
            return Result.Failure($"Compute shader asset {assetId} is incompatible with the active catalog generation.");
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

