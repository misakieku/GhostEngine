using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Streaming;

internal unsafe class WorkGraphAssetEntry : AssetEntry, ILoadableAssetEntry, IShaderCommitableAssetEntry
{
    private readonly ShaderCatalogEntry _catalogEntry;
    private MemoryBlock _payload;
    private byte* _bytecodePtr;
    private int _bytecodeSize;
    private ulong _shaderId;
    private string _name = string.Empty;
    private bool _bytecodeReady;

    public ReadOnlySpan<byte> Bytecode => _bytecodePtr != null && _bytecodeSize > 0
        ? new ReadOnlySpan<byte>(_bytecodePtr, _bytecodeSize)
        : ReadOnlySpan<byte>.Empty;

    public string Name => _name;
    public ulong ShaderId => _shaderId;

    internal override AssetState FailureState => _bytecodeReady ? AssetState.Ready : AssetState.Failed;

    public WorkGraphAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, AssetType assetType, Guid[] dependencies)
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
            throw new InvalidDataException($"Work graph asset {assetId} is missing from the runtime shader catalog.");
        }

        _catalogEntry = entry;
        _name = entry.Name;
        _shaderId = entry.ShaderId;
    }

    protected override void OnReleaseResource()
    {
        DiscardStagedPayload();
    }

    private void DiscardStagedPayload()
    {
        if (_payload.IsCreated)
        {
            _payload.Dispose();
            _payload = default;
            _bytecodePtr = null;
            _bytecodeSize = 0;
        }
    }

    public override void ReadAssetData(Span<byte> dst)
    {
        if (Bytecode.Length > 0 && dst.Length >= Bytecode.Length)
        {
            Bytecode.CopyTo(dst);
        }
    }

    public override void ReadAssetData<T>(ref T dst)
    {
        if (typeof(T) == typeof(WorkGraphAssetEntry))
        {
            dst = (T)(object)this;
        }
        else if (typeof(T) == typeof(ReadOnlyMemory<byte>))
        {
            dst = (T)(object)new ReadOnlyMemory<byte>(Bytecode.ToArray());
        }
    }

    public Result OnLoadContent([Owner] Stream contentStream, long contentSize)
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

            var payload = (byte*)_payload.GetUnsafePtr();
            var payloadSize = (long)_payload.Size;
            var header = ReadAt<ShaderContentHeader>(payload, 0, payloadSize);
            var passOffset = header.nameOffset + header.nameSize;
            var pass = ReadAt<ShaderContentHeader.PassHeader>(payload, passOffset, payloadSize);
            var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(payload, pass.dataOffset, payloadSize);

            if (entry.bytecodeHash != 0 && Manager.TryGetPooledBytecode(entry.bytecodeHash, out var pooledCode))
            {
                _bytecodePtr = pooledCode.pCode;
                _bytecodeSize = (int)pooledCode.size;
            }
            else
            {
                _bytecodePtr = payload + pass.dataOffset + entry.byteCodeOffset;
                _bytecodeSize = (int)entry.byteCodeSize;
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            if (stagedPayload.IsCreated)
            {
                stagedPayload.Dispose();
            }

            return Result.Failure($"Failed to stage work graph asset {AssetId}: {ex.Message}");
        }
        finally
        {
            contentStream.Dispose();
        }
    }

    internal static Result ValidatePayload(MemoryBlock payloadBlock, Guid assetId, ShaderCatalogEntry catalogEntry)
    {
        var payload = (byte*)payloadBlock.GetUnsafePtr();
        var payloadSize = (long)payloadBlock.Size;
        var header = ReadAt<ShaderContentHeader>(payload, 0, payloadSize);
        if (header.magic != ShaderContentHeader.MAGIC || header.version != ShaderContentHeader.VERSION ||
            header.shaderType != ShaderType.WorkGraph || header.passCount != 1 ||
            !IsRangeValid(header.nameOffset, header.nameSize, payloadSize))
        {
            return Result.Failure($"Work graph asset {assetId} uses an unsupported content format.");
        }

        if (header.shaderId != catalogEntry.ShaderId ||
            header.familyId != catalogEntry.FamilyId ||
            header.layoutHash != catalogEntry.LayoutHash ||
            header.propertyBufferSize != catalogEntry.PropertyBufferSize)
        {
            return Result.Failure($"Work graph asset {assetId} does not match its catalog metadata.");
        }

        var passOffset = header.nameOffset + header.nameSize;
        var pass = ReadAt<ShaderContentHeader.PassHeader>(payload, passOffset, payloadSize);
        if (pass.entryPointCount != 1 ||
            !IsRangeValid(pass.nameOffset, pass.nameSize, payloadSize) ||
            !IsRangeValid(pass.dataOffset, pass.dataSize, payloadSize))
        {
            return Result.Failure($"Work graph asset {assetId} contains invalid pass metadata.");
        }

        var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(payload, pass.dataOffset, payloadSize);
        var isPooled = entry.bytecodeHash != 0;
        if (!isPooled && !IsRangeValid(entry.byteCodeOffset, entry.byteCodeSize, pass.dataSize))
        {
            return Result.Failure($"Work graph asset {assetId} contains invalid library bytecode.");
        }

        if (entry.stage != ShaderStage.Library)
        {
            return Result.Failure($"Work graph asset {assetId} contains incompatible stage for library bytecode.");
        }

        return Result.Success();
    }

    public Result CommitShaderBytecode(ShaderLibrary shaderLibrary)
    {
        if (!_payload.IsCreated || _bytecodePtr == null)
        {
            return Result.Failure($"Work graph asset {AssetId} has no staged payload.");
        }

        try
        {
            Span<ShaderByteCode> byteCodes = stackalloc ShaderByteCode[1];
            Span<int> entryOffsets = stackalloc int[2];
            entryOffsets[0] = 0;
            entryOffsets[1] = 1;

            byteCodes[0] = new ShaderByteCode
            {
                pCode = _bytecodePtr,
                size = (ulong)_bytecodeSize,
            };

            var publishResult = shaderLibrary.PublishCompiledGeneration(
                _shaderId,
                entryOffsets,
                byteCodes);
            if (publishResult.IsFailure)
            {
                return publishResult;
            }

            _bytecodeReady = true;
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to commit work graph asset {AssetId}: {ex.Message}");
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

