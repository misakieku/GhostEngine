using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Streaming;

internal unsafe class ShaderAssetEntry : AssetEntry, ILoadableAssetEntry, IShaderCommitableAssetEntry
{
    private const int MAX_PASS_COUNT = 16;
    private const int MAX_ENTRY_POINT_COUNT = 8;

    private Handle<Shader> _actualHandle;
    private MemoryBlock _payload;

    internal override AssetState FailureState
    {
        get
        {
            if (Manager.ShaderVariants.TryGetVariantIndex(AssetId, out var index) &&
                Manager.ShaderVariants.GetState(index) == ShaderVariantState.BytecodeReady)
            {
                return AssetState.Ready;
            }

            return AssetState.Failed;
        }
    }

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
        DiscardStagedPayload();
    }

    private void DiscardStagedPayload()
    {
        if (_payload.IsCreated)
        {
            _payload.Dispose();
            _payload = default;
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
        MemoryBlock stagedPayload = default;
        try
        {
            contentStream.Position = 0;
            stagedPayload = contentStream.ReadMemory(AllocationHandle.Persistent);

            if (!Manager.ShaderVariants.TryGetVariantIndex(AssetId, out var variantIndex))
            {
                stagedPayload.Dispose();
                return Result.Failure($"Shader asset {AssetId} is missing from the runtime shader catalog.");
            }

            ref readonly var variant = ref Manager.ShaderVariants.GetVariant(variantIndex);
            var validation = ValidatePayload(stagedPayload, AssetId, in variant);
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

            return Result.Failure($"Failed to stage shader asset {AssetId}: {ex.Message}");
        }
    }

    internal static Result ValidatePayload(MemoryBlock payloadBlock, Guid assetId, scoped in ShaderVariantRecord variant)
    {
        var payload = (byte*)payloadBlock.GetUnsafePtr();
        var payloadSize = (long)payloadBlock.Size;
        var header = ReadAt<ShaderContentHeader>(payload, 0, payloadSize);

        if (header.magic != ShaderContentHeader.MAGIC || header.version != ShaderContentHeader.VERSION ||
            header.shaderType != ShaderType.Graphics)
        {
            return Result.Failure($"Shader asset {assetId} uses an unsupported content format.");
        }

        if (header.passCount == 0 || header.passCount > MAX_PASS_COUNT ||
            !IsRangeValid(header.nameOffset, header.nameSize, payloadSize))
        {
            return Result.Failure($"Shader asset {assetId} contains invalid header ranges.");
        }

        if (header.shaderId != variant.ShaderId ||
            header.familyId != variant.FamilyId ||
            header.layoutHash != variant.LayoutHash ||
            header.propertyBufferSize != variant.PropertyBufferSize ||
            header.shaderModel != variant.ShaderModel ||
            header.passCount != variant.PassCount ||
            variant.Passes.Length != variant.PassCount)
        {
            return Result.Failure($"Shader asset {assetId} does not match its catalog metadata.");
        }

        var passOffset = header.nameOffset + header.nameSize;
        for (var passIndex = 0; passIndex < header.passCount; passIndex++)
        {
            var pass = ReadAt<ShaderContentHeader.PassHeader>(payload, passOffset, payloadSize);
            ref readonly var catalogPass = ref variant.Passes[passIndex];
            if (!IsRangeValid(pass.nameOffset, pass.nameSize, payloadSize) ||
                !IsRangeValid(pass.dataOffset, pass.dataSize, payloadSize) ||
                pass.entryPointCount == 0 || pass.entryPointCount > MAX_ENTRY_POINT_COUNT)
            {
                return Result.Failure($"Shader asset {assetId} contains invalid pass {passIndex} ranges.");
            }

            if (pass.semantic != catalogPass.Semantic ||
                pass.stageMask != catalogPass.StageMask ||
                pass.passId != catalogPass.PassId ||
                pass.entryPointCount != catalogPass.EntryPointCount ||
                pass.localPipeline.GetHashCode64() != catalogPass.LocalPipeline.GetHashCode64())
            {
                return Result.Failure($"Shader asset {assetId} pass {passIndex} is incompatible with the active catalog generation.");
            }

            var entryHeadersSize = pass.entryPointCount * sizeof(ShaderContentHeader.EntryPointHeader);
            if (entryHeadersSize > pass.dataSize)
            {
                return Result.Failure($"Shader asset {assetId} contains an invalid entry-point table.");
            }

            for (var entryIndex = 0; entryIndex < pass.entryPointCount; entryIndex++)
            {
                var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(
                    payload,
                    pass.dataOffset + (entryIndex * sizeof(ShaderContentHeader.EntryPointHeader)),
                    payloadSize);
                if (entry.byteCodeOffset < entryHeadersSize ||
                    !IsRangeValid(entry.byteCodeOffset, entry.byteCodeSize, pass.dataSize) ||
                    !IsExpectedStage(pass.stageMask, pass.entryPointCount, entryIndex, entry.stage))
                {
                    return Result.Failure($"Shader asset {assetId} contains incompatible bytecode topology for pass {passIndex}.");
                }
            }

            passOffset = pass.dataOffset + pass.dataSize;
        }

        return Result.Success();
    }

    private static bool IsExpectedStage(ShaderStageMask stageMask, uint entryPointCount, int entryIndex, ShaderStage actualStage)
    {
        if (stageMask == ShaderStageMask.Compute)
        {
            return entryPointCount == 1 && actualStage == ShaderStage.ComputeShader;
        }

        const ShaderStageMask graphicsStages = ShaderStageMask.Amplification | ShaderStageMask.Mesh | ShaderStageMask.Pixel;
        if ((stageMask & (ShaderStageMask.Mesh | ShaderStageMask.Pixel)) != (ShaderStageMask.Mesh | ShaderStageMask.Pixel) ||
            (stageMask & ~graphicsStages) != 0)
        {
            return false;
        }

        var hasAmplification = (stageMask & ShaderStageMask.Amplification) != 0;
        if (entryPointCount != (hasAmplification ? 3u : 2u))
        {
            return false;
        }

        if (hasAmplification)
        {
            return entryIndex switch
            {
                0 => actualStage == ShaderStage.AmplificationShader,
                1 => actualStage == ShaderStage.MeshShader,
                2 => actualStage == ShaderStage.PixelShader,
                _ => false,
            };
        }

        return entryIndex switch
        {
            0 => actualStage == ShaderStage.MeshShader,
            1 => actualStage == ShaderStage.PixelShader,
            _ => false,
        };
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
            Span<ShaderByteCode> byteCodes = stackalloc ShaderByteCode[MAX_PASS_COUNT * MAX_ENTRY_POINT_COUNT];
            Span<int> passEntryOffsets = stackalloc int[MAX_PASS_COUNT + 1];
            var byteCodeCount = 0;
            passEntryOffsets[0] = 0;

            for (var passIndex = 0; passIndex < header.passCount; passIndex++)
            {
                var pass = ReadAt<ShaderContentHeader.PassHeader>(payload, passOffset, payloadSize);
                for (var entryIndex = 0; entryIndex < pass.entryPointCount; entryIndex++)
                {
                    var entryHeaderOffset = pass.dataOffset + (entryIndex * sizeof(ShaderContentHeader.EntryPointHeader));
                    var entry = ReadAt<ShaderContentHeader.EntryPointHeader>(payload, entryHeaderOffset, payloadSize);
                    byteCodes[byteCodeCount++] = new ShaderByteCode
                    {
                        pCode = payload + pass.dataOffset + entry.byteCodeOffset,
                        size = (ulong)entry.byteCodeSize,
                    };
                }

                passEntryOffsets[passIndex + 1] = byteCodeCount;
                passOffset = pass.dataOffset + pass.dataSize;
            }

            var publishResult = shaderLibrary.PublishCompiledGeneration(
                header.shaderId,
                passEntryOffsets[..((int)header.passCount + 1)],
                byteCodes[..byteCodeCount]);
            if (publishResult.IsFailure)
            {
                DiscardStagedPayload();
                return publishResult;
            }

            Manager.ShaderVariants.PublishBytecodeReady(AssetId);
            DiscardStagedPayload();
            return Result.Success();
        }
        catch (Exception ex)
        {
            DiscardStagedPayload();
            return Result.Failure($"Failed to commit shader asset {AssetId}: {ex.Message}");
        }
    }
}
