using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Engine.ShaderProperties;
using Ghost.Engine.Streaming;
using Ghost.Engine.Utilities;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VisibleMeshletEntry
    {
        public uint instanceIndex;
        public uint meshletIndex;
    }

    public struct CameraCullingBuffers
    {
        public Identifier<RGBuffer> visibleMeshletsPass1;
        public Identifier<RGBuffer> visibleMeshletsPass2;
        public Identifier<RGBuffer> occludedMeshlets;
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
    }

    private struct MeshletCullPass1Data
    {
        public Identifier<RGBuffer> visibleMeshletsPass1;
        public Identifier<RGBuffer> occludedMeshlets;
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGTexture> hzbTexture;
        public uint hzbMipCount;
        public uint2 renderSize;
        public uint2 hzbBaseSize;
        public uint instanceCount;
        public uint maxVisibleMeshlets;
        public float lodErrorThreshold;
        public float cullingThreshold;
        public uint threadGroupCount;
        public uint entrypointIndex;
        public ProgramIdentifier programIdentifier;
        public ulong backingMemoryAddress;
        public ulong backingMemorySize;
        public SetWorkGraphFlags flags;
        public uint cullPassSemantic;
    }

    private struct MeshletCullPass2Data
    {
        public Identifier<RGBuffer> visibleMeshletsPass2;
        public Identifier<RGBuffer> occludedMeshlets;
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public Identifier<RGTexture> hzbTexture;
        public Handle<ComputeShader> shader;
        public uint hzbMipCount;
        public uint2 renderSize;
        public uint2 hzbBaseSize;
        public uint maxVisibleMeshlets;
    }

    private struct PrepareIndirectArgsPassData
    {
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public Handle<ComputeShader> shader;
        public uint cullPassIndex;
        public uint maxCount;
    }

    private struct BuildHZBMipPassData
    {
        public Identifier<RGBuffer> srcBuffer;
        public Identifier<RGTexture> dstTex;
        public Handle<ComputeShader> shader;
        public uint startMip;
        public uint minDstCount;
        public uint isFirstBatch;
        public uint2 srcLimit;
        public uint4 dstSize01;
        public uint4 dstSize23;
        public uint2 dispatchSize;
    }

    internal partial class CullingResource : IPipelineResource
    {
        [ResolveAsset("EngineResources/Shaders/MeshletCullGraph")]
        public IAssetEntry cullWorkGraphEntry = null!;

        [ResolveAsset("EngineResources/Shaders/OccludedMeshletCull")]
        public Handle<ComputeShader> occludedMeshletCullShader;

        [ResolveAsset("EngineResources/Shaders/BuildHZB")]
        public Handle<ComputeShader> buildHZBShader;

        [ResolveAsset("EngineResources/Shaders/PrepareMeshletIndirectArgs")]
        public Handle<ComputeShader> prepareIndirectArgsShader;

        [ResolveAsset("EngineResources/Shaders/ExportVisibilityDepth")]
        public Handle<ComputeShader> exportVisibilityDepthShader;

        [ResolveAsset("EngineResources/Shaders/ClearVisibilityBuffer")]
        public Handle<ComputeShader> clearVisibilityBufferShader;

        [ResolveAsset("EngineResources/Shaders/Blit")]
        public Handle<Shader> blitShader;

        [ResolveAsset("EngineResources/Shaders/MeshletDebug")]
        public Handle<Shader> meshletDebugShader;

        public IWorkGraphProgram? cullWorkGraphProgram;

        public void EnsureWorkGraphProgram(RenderEngine renderEngine)
        {
            if (cullWorkGraphProgram != null)
            {
                return;
            }

            if (cullWorkGraphEntry is WorkGraphAssetEntry wgEntry && !wgEntry.Bytecode.IsEmpty)
            {
                cullWorkGraphProgram = renderEngine.GraphicsEngine.CreateWorkGraphProgram(wgEntry.Bytecode, "MeshletCullGraph");
            }
        }

        partial void OnDisposing()
        {
            cullWorkGraphProgram?.Dispose();
            cullWorkGraphProgram = null;
        }
    }

    private static class CullConstants
    {
        public const uint COUNTER_BUFFER_SIZE = 128;
        public const uint INDIRECT_ARGS_BUFFER_SIZE = 128;

        // Counter byte offsets matching CullCommon.hlsl
        public const uint OFFSET_PASS1_VISIBLE_COUNT = 0;
        public const uint OFFSET_PASS1_OCCLUDED_COUNT = 4;
        public const uint OFFSET_PASS2_VISIBLE_COUNT = 8;

        // Indirect draw arguments byte offsets matching CullCommon.hlsl
        public const uint INDIRECT_OFFSET_PASS1_VISIBLE = 0;
        public const uint INDIRECT_OFFSET_PASS2_VISIBLE = 16;
        public const uint INDIRECT_OFFSET_PASS2_CULL = 32;
    }

    private static ICommandSignature s_dispatchMeshCommandSignature = null!;
    private static ICommandSignature s_dispatchCommandSignature = null!;
    private static IShaderVariantSource s_shaderVariants = null!;

    private readonly CullingResource _cullingResource = new();

    private void InitializeCulling(RenderEngine renderEngine, AssetManager assetManager)
    {
        _cullingResource.Resolve(assetManager);
        s_shaderVariants = assetManager.ShaderVariants;

        var indirectDesc = new CommandSignatureDesc
        {
            Stride = 12,
            Arguments = new IndirectArgumentDesc[]
            {
                new() { Type = IndirectArgumentType.DispatchMesh }
            }
        };
        s_dispatchMeshCommandSignature = renderEngine.GraphicsEngine.CreateCommandSignature(in indirectDesc, default);

        var dispatchDesc = new CommandSignatureDesc
        {
            Stride = 12,
            Arguments = new IndirectArgumentDesc[]
            {
                new() { Type = IndirectArgumentType.Dispatch }
            }
        };
        s_dispatchCommandSignature = renderEngine.GraphicsEngine.CreateCommandSignature(in dispatchDesc, default);
    }

    private unsafe void AddInitializeCullingBuffersPass(RenderGraph rg, out CameraCullingBuffers buffers)
    {
        var entrySize = (uint)sizeof(VisibleMeshletEntry);
        var bufferSize = _settings.MaxVisibleMeshletsOnScreen * entrySize;

        var visibleDesc = new BufferDesc
        {
            Size = bufferSize,
            Stride = entrySize,
            Usage = BufferUsage.Structured | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };

        var occludedDesc = new BufferDesc
        {
            Size = _settings.MaxVisibleMeshletsOnScreen * 12,
            Stride = 12,
            Usage = BufferUsage.Structured | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };

        var counterDesc = new BufferDesc
        {
            Size = CullConstants.COUNTER_BUFFER_SIZE,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };

        var indirectDesc = new BufferDesc
        {
            Size = CullConstants.INDIRECT_ARGS_BUFFER_SIZE,
            Stride = 4,
            Usage = BufferUsage.IndirectArgument | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };

        buffers = new CameraCullingBuffers
        {
            visibleMeshletsPass1 = rg.CreateBuffer(in visibleDesc, "VisibleMeshlets_Pass1"),
            visibleMeshletsPass2 = rg.CreateBuffer(in visibleDesc, "VisibleMeshlets_Pass2"),
            occludedMeshlets = rg.CreateBuffer(in occludedDesc, "OccludedInstances"),
            counterBuffer = rg.CreateBuffer(in counterDesc, "CullCounters"),
            indirectArgsBuffer = rg.CreateBuffer(in indirectDesc, "MeshletIndirectArgs")
        };

        rg.AddClearBufferPass(buffers.counterBuffer, CullConstants.COUNTER_BUFFER_SIZE, 0, "InitCullingCounters");
    }

    private unsafe void AddMeshletCullPass1(RenderGraph rg, in CameraCullingBuffers buffers, Identifier<RGTexture> hzbTexture, uint hzbMipCount, uint2 screenSize, uint2 hzbBaseSize, uint instanceCount)
    {
        _cullingResource.EnsureWorkGraphProgram(_renderEngine);
        if (_cullingResource.cullWorkGraphProgram == null)
        {
            return;
        }

        var cullProgram = _cullingResource.cullWorkGraphProgram;
        var flags = cullProgram.IsInitialized ? SetWorkGraphFlags.None : SetWorkGraphFlags.Initialize;

        var entrypointIndex = cullProgram.GetEntrypointIndex("InstanceCullNode");
        if (entrypointIndex == uint.MaxValue)
        {
            entrypointIndex = 0;
        }

        using var builder = rg.AddComputeRenderPass<MeshletCullPass1Data>("MeshletCull_Pass1");

        // This pass carries the one-time backing memory initialization, so it must never be culled.
        builder.AllowPassCulling(false);

        builder.UseBuffer(buffers.visibleMeshletsPass1, AccessFlags.Write);
        builder.UseBuffer(buffers.occludedMeshlets, AccessFlags.Write);
        builder.UseBuffer(buffers.counterBuffer, AccessFlags.ReadWrite);

        if (hzbTexture.IsValid)
        {
            builder.UseTexture(hzbTexture, AccessFlags.Read);
        }

        var passData = new MeshletCullPass1Data
        {
            visibleMeshletsPass1 = buffers.visibleMeshletsPass1,
            occludedMeshlets = buffers.occludedMeshlets,
            counterBuffer = buffers.counterBuffer,
            hzbTexture = hzbTexture,
            hzbMipCount = hzbMipCount,
            renderSize = screenSize,
            hzbBaseSize = hzbBaseSize,
            instanceCount = instanceCount,
            maxVisibleMeshlets = _settings.MaxVisibleMeshletsOnScreen,
            lodErrorThreshold = _settings.MeshletLodErrorThreshold,
            cullingThreshold = _settings.InstanceCullingThreshold,
            threadGroupCount = Math.Max(1u, (instanceCount + 63) / 64),
            entrypointIndex = entrypointIndex,
            programIdentifier = cullProgram.ProgramIdentifier,
            backingMemoryAddress = cullProgram.BackingMemoryAddress,
            backingMemorySize = cullProgram.BackingMemorySize,
            flags = flags,
            cullPassSemantic = (uint)PassSemantic.Visibility
        };

        builder.SetPassData(passData);
        builder.SetRenderFunc<MeshletCullPass1Data>(static (ref readonly passData, computeCtx) =>
        {
            var visibleUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visibleMeshletsPass1).AsResource(), BindlessAccess.UnorderedAccess);
            var occludedUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.occludedMeshlets).AsResource(), BindlessAccess.UnorderedAccess);
            var counterUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.counterBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            var hzbTextureIndex = (passData.hzbMipCount > 0 && passData.hzbTexture.IsValid)
                ? computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualTexture(passData.hzbTexture).AsResource(), BindlessAccess.ShaderResource)
                : uint.MaxValue;

            var props = new InternalMeshletCullGraphShaderProperties
            {
                hzbMipCount = passData.hzbMipCount,
                renderWidth = passData.renderSize.x,
                renderHeight = passData.renderSize.y,
                cullPassIndex = 0,
                visibleMeshletsUav = visibleUav,
                occludedMeshletsUav = occludedUav,
                counterBufferUav = counterUav,
                maxVisibleMeshlets = passData.maxVisibleMeshlets,
                lodErrorThreshold = passData.lodErrorThreshold,
                cullingThreshold = passData.cullingThreshold,
                instanceCount = passData.instanceCount,
                hzbTexture = hzbTextureIndex,
                hzbBaseWidth = passData.hzbBaseSize.x,
                hzbBaseHeight = passData.hzbBaseSize.y,
                cullPassSemantic = passData.cullPassSemantic,
            };

            var setProgramDesc = SetProgramDesc.ForWorkGraph(
                passData.programIdentifier,
                passData.backingMemoryAddress,
                passData.backingMemorySize,
                passData.flags);
            computeCtx.SetProgram(in setProgramDesc);
            computeCtx.SetUserDataWithProperties(in props);

            var record = new WorkGraphDispatchGridRecord(passData.threadGroupCount, 1, 1);
            var dispatchDesc = DispatchGraphDesc.ForCPUInput(passData.entrypointIndex, 1, &record, (ulong)sizeof(WorkGraphDispatchGridRecord));
            computeCtx.DispatchGraph(in dispatchDesc);
        });
    }

    private void AddPrepareIndirectArgsPass(RenderGraph rg, in CameraCullingBuffers buffers, uint cullPassIndex)
    {
        if (_cullingResource.prepareIndirectArgsShader.IsInvalid)
        {
            return;
        }

        using var builder = rg.AddComputeRenderPass<PrepareIndirectArgsPassData>(StringUtility.DebugFormat("PrepareIndirectArgs_Pass{0}", cullPassIndex + 1));
        builder.UseBuffer(buffers.counterBuffer, AccessFlags.Read);
        builder.UseBuffer(buffers.indirectArgsBuffer, AccessFlags.Write);

        builder.SetPassData(new PrepareIndirectArgsPassData
        {
            counterBuffer = buffers.counterBuffer,
            indirectArgsBuffer = buffers.indirectArgsBuffer,
            shader = _cullingResource.prepareIndirectArgsShader,
            cullPassIndex = cullPassIndex,
            maxCount = _settings.MaxVisibleMeshletsOnScreen
        });

        builder.SetRenderFunc<PrepareIndirectArgsPassData>(static (ref readonly passData, computeCtx) =>
        {
            var visibleCountOffset = passData.cullPassIndex == 0 ? CullConstants.OFFSET_PASS1_VISIBLE_COUNT : CullConstants.OFFSET_PASS2_VISIBLE_COUNT;
            var visibleArgsOffset = passData.cullPassIndex == 0 ? CullConstants.INDIRECT_OFFSET_PASS1_VISIBLE : CullConstants.INDIRECT_OFFSET_PASS2_VISIBLE;
            var occludedCountOffset = passData.cullPassIndex == 0 ? CullConstants.OFFSET_PASS1_OCCLUDED_COUNT : uint.MaxValue;
            var cullArgsOffset = passData.cullPassIndex == 0 ? CullConstants.INDIRECT_OFFSET_PASS2_CULL : uint.MaxValue;

            var counterUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.counterBuffer).AsResource(), BindlessAccess.UnorderedAccess);
            var indirectUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.indirectArgsBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            var props = new InternalPrepareMeshletIndirectArgsShaderProperties
            {
                counterBuffer = counterUav,
                indirectArgsBuffer = indirectUav,
                visibleCountOffset = visibleCountOffset,
                visibleArgsOffset = visibleArgsOffset,
                maxCount = passData.maxCount,
                occludedCountOffset = occludedCountOffset,
                cullArgsOffset = cullArgsOffset,
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);
            computeCtx.DispatchCompute(1, 1, 1);
        });
    }

    private void AddBuildHZBPasses(RenderGraph rg, Identifier<RGBuffer> visBuffer, Identifier<RGTexture> hzbTexture, uint hzbMipCount, uint2 baseSize, uint2 renderSize)
    {
        if (hzbMipCount == 0)
        {
            return;
        }

        Span<uint2> mipSizes = stackalloc uint2[(int)hzbMipCount];
        mipSizes[0] = baseSize;

        for (var i = 1; i < (int)hzbMipCount; ++i)
        {
            var prev = mipSizes[i - 1];
            mipSizes[i] = new uint2(Math.Max(1u, prev.x / 2), Math.Max(1u, prev.y / 2));
        }

        var startMip = 0u;
        var batchIndex = 0u;

        while (startMip < hzbMipCount)
        {
            var mipsInThisBatch = Math.Min(hzbMipCount - startMip, 4u);
            var isFirstBatch = (startMip == 0);

            var srcLimit = isFirstBatch
                ? new uint2(renderSize.x - 1, renderSize.y - 1)
                : new uint2(mipSizes[(int)startMip - 1].x - 1, mipSizes[(int)startMip - 1].y - 1);

            var s0 = (mipsInThisBatch >= 1) ? mipSizes[(int)startMip] : default;
            var s1 = (mipsInThisBatch >= 2) ? mipSizes[(int)startMip + 1] : default;
            var s2 = (mipsInThisBatch >= 3) ? mipSizes[(int)startMip + 2] : default;
            var s3 = (mipsInThisBatch >= 4) ? mipSizes[(int)startMip + 3] : default;

            var passName = StringUtility.DebugFormat("BuildHZB_Batch{0}", batchIndex);
            using (var builder = rg.AddComputeRenderPass<BuildHZBMipPassData>(passName))
            {
                if (isFirstBatch)
                {
                    builder.UseBuffer(visBuffer, AccessFlags.Read);
                    builder.UseTexture(hzbTexture, AccessFlags.Write);
                }
                else
                {
                    builder.UseTexture(hzbTexture, AccessFlags.ReadWrite);
                }

                builder.SetPassData(new BuildHZBMipPassData
                {
                    srcBuffer = visBuffer,
                    dstTex = hzbTexture,
                    shader = _cullingResource.buildHZBShader,
                    startMip = startMip,
                    minDstCount = mipsInThisBatch,
                    isFirstBatch = isFirstBatch ? 1u : 0u,
                    srcLimit = srcLimit,
                    dstSize01 = new uint4(s0.x, s0.y, s1.x, s1.y),
                    dstSize23 = new uint4(s2.x, s2.y, s3.x, s3.y),
                    dispatchSize = s0
                });

                builder.SetRenderFunc<BuildHZBMipPassData>(static (ref readonly passData, computeCtx) =>
                {
                    ExecuteHZBDownsamplePass(in passData, computeCtx);
                });
            }

            startMip += mipsInThisBatch;
            batchIndex++;
        }
    }

    private static void ExecuteHZBDownsamplePass(scoped in BuildHZBMipPassData passData, IComputeRenderContext computeCtx)
    {
        var dstActual = computeCtx.GetActualTexture(passData.dstTex);
        var dstRes = dstActual.AsResource();

        uint srcBuffer = 0;
        uint srcUavMip = 0;

        if (passData.isFirstBatch != 0)
        {
            var srcActual = computeCtx.GetActualBuffer(passData.srcBuffer);
            srcBuffer = computeCtx.ResourceDatabase.GetBindlessIndex(srcActual.AsResource(), BindlessAccess.ShaderResource);
        }
        else
        {
            srcUavMip = computeCtx.ResourceDatabase.GetBindlessIndex(dstRes, BindlessAccess.UnorderedAccess, passData.startMip - 1);
        }

        Span<uint> requestedMips = stackalloc uint[4];
        Span<uint> dstUavs = stackalloc uint[4];
        for (var i = 0u; i < passData.minDstCount; i++)
        {
            requestedMips[(int)i] = passData.startMip + i;
        }

        computeCtx.GetActualBindlessIndices(passData.dstTex, requestedMips.Slice(0, (int)passData.minDstCount), dstUavs.Slice(0, (int)passData.minDstCount), BindlessAccess.UnorderedAccess);

        var props = new InternalBuildHZBShaderProperties
        {
            srcBuffer = srcBuffer,
            srcUavMip = srcUavMip,
            dstUavMip0 = dstUavs[0],
            dstUavMip1 = passData.minDstCount >= 2 ? dstUavs[1] : uint.MaxValue,
            dstUavMip2 = passData.minDstCount >= 3 ? dstUavs[2] : uint.MaxValue,
            dstUavMip3 = passData.minDstCount >= 4 ? dstUavs[3] : uint.MaxValue,
            minDstCount = passData.minDstCount,
            isFirstBatch = passData.isFirstBatch,
            srcLimit = passData.srcLimit,
            _padding = default,
            dstSize01 = passData.dstSize01,
            dstSize23 = passData.dstSize23,
        };

        computeCtx.SetActiveCompute(passData.shader, 0);
        computeCtx.SetUserDataWithProperties(in props);

        var threadGroupsX = Math.Max(1u, (passData.dispatchSize.x + 7) / 8);
        var threadGroupsY = Math.Max(1u, (passData.dispatchSize.y + 7) / 8);
        computeCtx.DispatchCompute(threadGroupsX, threadGroupsY, 1);
    }

    private void AddMeshletCullPass2(RenderGraph rg, in CameraCullingBuffers buffers, Identifier<RGTexture> hzbTexture, uint hzbMipCount, uint2 renderSize, uint2 hzbBaseSize)
    {
        if (!_cullingResource.occludedMeshletCullShader.IsValid || s_dispatchCommandSignature == null)
        {
            return;
        }

        using var builder = rg.AddComputeRenderPass<MeshletCullPass2Data>("MeshletCull_Pass2");
        builder.UseBuffer(buffers.occludedMeshlets, AccessFlags.Read);
        builder.UseBuffer(buffers.visibleMeshletsPass2, AccessFlags.Write);
        builder.UseBuffer(buffers.counterBuffer, AccessFlags.ReadWrite);
        builder.UseBuffer(buffers.indirectArgsBuffer, AccessFlags.Read);

        if (hzbTexture.IsValid)
        {
            builder.UseTexture(hzbTexture, AccessFlags.Read);
        }

        var passData = new MeshletCullPass2Data
        {
            visibleMeshletsPass2 = buffers.visibleMeshletsPass2,
            occludedMeshlets = buffers.occludedMeshlets,
            counterBuffer = buffers.counterBuffer,
            indirectArgsBuffer = buffers.indirectArgsBuffer,
            hzbTexture = hzbTexture,
            shader = _cullingResource.occludedMeshletCullShader,
            hzbMipCount = hzbMipCount,
            renderSize = renderSize,
            hzbBaseSize = hzbBaseSize,
            maxVisibleMeshlets = _settings.MaxVisibleMeshletsOnScreen
        };

        builder.SetPassData(passData);
        builder.SetRenderFunc<MeshletCullPass2Data>(static (ref readonly passData, computeCtx) =>
        {
            var visibleUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visibleMeshletsPass2).AsResource(), BindlessAccess.UnorderedAccess);
            var occludedSrv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.occludedMeshlets).AsResource(), BindlessAccess.ShaderResource);
            var counterUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.counterBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            var hzbTextureIndex = (passData.hzbMipCount > 0 && passData.hzbTexture.IsValid)
                ? computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualTexture(passData.hzbTexture).AsResource(), BindlessAccess.ShaderResource)
                : uint.MaxValue;

            var props = new InternalOccludedMeshletCullShaderProperties
            {
                hzbMipCount = passData.hzbMipCount,
                renderWidth = passData.renderSize.x,
                renderHeight = passData.renderSize.y,
                visibleMeshletsUav = visibleUav,
                occludedMeshletsUav = occludedSrv,
                counterBufferUav = counterUav,
                maxVisibleMeshlets = passData.maxVisibleMeshlets,
                hzbTexture = hzbTextureIndex,
                hzbBaseWidth = passData.hzbBaseSize.x,
                hzbBaseHeight = passData.hzbBaseSize.y,
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);

            var actualIndirectBuf = computeCtx.GetActualBuffer(passData.indirectArgsBuffer);
            computeCtx.ExecuteIndirect(s_dispatchCommandSignature, 1, actualIndirectBuf, CullConstants.INDIRECT_OFFSET_PASS2_CULL);
        });
    }

    private void DisposeCulling()
    {
        _cullingResource.Dispose();

        s_dispatchMeshCommandSignature?.Dispose();
        s_dispatchMeshCommandSignature = null!;

        s_dispatchCommandSignature?.Dispose();
        s_dispatchCommandSignature = null!;

        s_shaderVariants = null!;
    }
}
