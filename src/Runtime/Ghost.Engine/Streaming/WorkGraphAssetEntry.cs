using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Streaming;

internal unsafe class WorkGraphAssetEntry : AssetEntry, ILoadableAssetEntry, IShaderCommitableAssetEntry
{
    private struct WorkGraphVariant
    {
        public KeywordMask128 mask;
        public byte* ptr;
        public int size;
    }

    private MemoryBlock _payload;
    private WorkGraphVariant[]? _variants;
    private KeywordMask128 _supportedMask;
    private ulong _shaderId;
    private bool _bytecodeReady;

    public ReadOnlySpan<byte> GetBytecode(KeywordMask128 activeKeywords = default)
    {
        if (_variants == null || _variants.Length == 0)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        if (_variants.Length == 1)
        {
            return new ReadOnlySpan<byte>(_variants[0].ptr, _variants[0].size);
        }

        var target = activeKeywords & _supportedMask;
        for (var i = 0; i < _variants.Length; i++)
        {
            if (_variants[i].mask == target)
            {
                return new ReadOnlySpan<byte>(_variants[i].ptr, _variants[i].size);
            }
        }

        return new ReadOnlySpan<byte>(_variants[0].ptr, _variants[0].size);
    }

    public ReadOnlySpan<byte> Bytecode => GetBytecode(default);

    public ulong ShaderId => _shaderId;

    internal override AssetState FailureState => _bytecodeReady ? AssetState.Ready : AssetState.Failed;

    public WorkGraphAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, AssetType assetType, Guid[] dependencies, string assetName)
        : base(manager, resourceDatabase, resourceManager, assetId, assetType, dependencies, assetName)
    {
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
            _variants = null;
            _supportedMask = default;
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

            var validation = ValidatePayload(stagedPayload, AssetId);
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
            _shaderId = header.shaderId;

            var passOffset = header.nameOffset + header.nameSize;
            var pass = ReadAt<ShaderContentHeader.PassHeader>(payload, passOffset, payloadSize);

            var variantCount = pass.variantCount > 0 ? (int)pass.variantCount : 1;
            _variants = new WorkGraphVariant[variantCount];
            _supportedMask = KeywordMask128.Empty;

            if (pass.variantCount <= 1)
            {
                var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(payload, pass.dataOffset, payloadSize);
                byte* ptr;
                int size;
                if (entry.bytecodeHash != 0 && Manager.TryGetPooledBytecode(entry.bytecodeHash, out var pooledCode))
                {
                    ptr = pooledCode.pCode;
                    size = (int)pooledCode.size;
                }
                else
                {
                    ptr = payload + pass.dataOffset + entry.byteCodeOffset;
                    size = (int)entry.byteCodeSize;
                }

                _variants[0] = new WorkGraphVariant
                {
                    mask = KeywordMask128.Empty,
                    ptr = ptr,
                    size = size,
                };
            }
            else
            {
                for (var v = 0; v < pass.variantCount; v++)
                {
                    var vh = ReadAt<ShaderContentHeader.VariantHeader>(payload, pass.dataOffset + (v * sizeof(ShaderContentHeader.VariantHeader)), payloadSize);
                    var kwString = vh.keywordSize > 0
                        ? System.Text.Encoding.UTF8.GetString(payload + vh.keywordOffset, (int)vh.keywordSize)
                        : string.Empty;
                    var mask = PipelineKeywordRegistry.ParseMask(kwString);
                    _supportedMask |= mask;

                    var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(payload, vh.dataOffset, payloadSize);
                    byte* ptr;
                    int size;
                    if (entry.bytecodeHash != 0 && Manager.TryGetPooledBytecode(entry.bytecodeHash, out var pooledCode))
                    {
                        ptr = pooledCode.pCode;
                        size = (int)pooledCode.size;
                    }
                    else
                    {
                        ptr = payload + vh.dataOffset + entry.byteCodeOffset;
                        size = (int)entry.byteCodeSize;
                    }

                    _variants[v] = new WorkGraphVariant
                    {
                        mask = mask,
                        ptr = ptr,
                        size = size,
                    };
                }
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

    internal static Result ValidatePayload(MemoryBlock payloadBlock, Guid assetId)
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

        var passOffset = header.nameOffset + header.nameSize;
        var pass = ReadAt<ShaderContentHeader.PassHeader>(payload, passOffset, payloadSize);
        if (pass.entryPointCount != 1 ||
            !IsRangeValid(pass.nameOffset, pass.nameSize, payloadSize) ||
            !IsRangeValid(pass.dataOffset, pass.dataSize, payloadSize))
        {
            return Result.Failure($"Work graph asset {assetId} contains invalid pass metadata.");
        }

        if (pass.variantCount <= 1)
        {
            var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(payload, pass.dataOffset, payloadSize);
            var isPooled = entry.bytecodeHash != 0;
            if (!isPooled && !IsRangeValid(pass.dataOffset + entry.byteCodeOffset, entry.byteCodeSize, payloadSize))
            {
                return Result.Failure($"Work graph asset {assetId} contains invalid library bytecode.");
            }

            if (entry.stage != ShaderStage.Library)
            {
                return Result.Failure($"Work graph asset {assetId} contains incompatible stage for library bytecode.");
            }
        }
        else
        {
            var variantHeadersSize = pass.variantCount * sizeof(ShaderContentHeader.VariantHeader);
            if (variantHeadersSize > pass.dataSize)
            {
                return Result.Failure($"Work graph asset {assetId} contains an invalid variant table.");
            }

            for (var v = 0; v < pass.variantCount; v++)
            {
                var vh = ReadAt<ShaderContentHeader.VariantHeader>(payload, pass.dataOffset + (v * sizeof(ShaderContentHeader.VariantHeader)), payloadSize);
                if (!IsRangeValid(vh.keywordOffset, vh.keywordSize, payloadSize) ||
                    !IsRangeValid(vh.dataOffset, vh.dataSize, payloadSize))
                {
                    return Result.Failure($"Work graph asset {assetId} contains invalid variant {v} ranges.");
                }

                var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(payload, vh.dataOffset, payloadSize);
                var isPooled = entry.bytecodeHash != 0;
                if (!isPooled && !IsRangeValid(vh.dataOffset + entry.byteCodeOffset, entry.byteCodeSize, payloadSize))
                {
                    return Result.Failure($"Work graph asset {assetId} contains invalid library bytecode for variant {v}.");
                }

                if (entry.stage != ShaderStage.Library)
                {
                    return Result.Failure($"Work graph asset {assetId} contains incompatible stage for variant {v}.");
                }
            }
        }

        return Result.Success();
    }

    public Result CommitShaderBytecode(ShaderLibrary shaderLibrary)
    {
        if (!_payload.IsCreated || _variants == null || _variants.Length == 0)
        {
            return Result.Failure($"Work graph asset {AssetId} has no staged payload.");
        }

        try
        {
            var variantCount = _variants.Length;
            var compiledVariants = new CompiledVariantData[variantCount];

            for (var i = 0; i < variantCount; i++)
            {
                var byteCodes = new ShaderByteCode[1];
                byteCodes[0] = new ShaderByteCode
                {
                    pCode = _variants[i].ptr,
                    size = (ulong)_variants[i].size,
                };
                compiledVariants[i] = new CompiledVariantData(_variants[i].mask, byteCodes);
            }

            var passData = new CompiledPassData(_supportedMask, compiledVariants);
            var publishResult = shaderLibrary.PublishCompiledGeneration(_shaderId, new ReadOnlySpan<CompiledPassData>(ref passData));
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

