using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Engine.ShaderProperties;
using Ghost.Engine.Streaming;
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
        public Identifier<RGBuffer> visibleMaskedMeshletsPass1;
        public Identifier<RGBuffer> visibleMeshletsPass2;
        public Identifier<RGBuffer> visibleMaskedMeshletsPass2;
        public Identifier<RGBuffer> occludedMeshlets;
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
    }

    private struct MeshletCullPass1Data
    {
        public Identifier<RGBuffer> visibleMeshletsPass1;
        public Identifier<RGBuffer> visibleMaskedMeshletsPass1;
        public Identifier<RGBuffer> occludedMeshlets;
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGTexture> hzbAtlas;
        public uint hzbMipCount;
        public uint renderWidth;
        public uint renderHeight;
        public uint4 hzbOffsets0;
        public uint4 hzbOffsets1;
        public uint4 hzbOffsets2;
        public uint4 hzbOffsets3;
        public uint instanceCount;
        public uint maxVisibleMeshlets;
        public float lodErrorThreshold;
        public uint threadGroupCount;
        public uint entrypointIndex;
        public ProgramIdentifier programIdentifier;
        public ulong backingMemoryAddress;
        public ulong backingMemorySize;
        public SetWorkGraphFlags flags;
    }

    private struct MeshletCullPass2Data
    {
        public Identifier<RGBuffer> visibleMeshletsPass2;
        public Identifier<RGBuffer> visibleMaskedMeshletsPass2;
        public Identifier<RGBuffer> occludedMeshlets;
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGTexture> hzbAtlas;
        public uint hzbMipCount;
        public uint renderWidth;
        public uint renderHeight;
        public uint4 hzbOffsets0;
        public uint4 hzbOffsets1;
        public uint4 hzbOffsets2;
        public uint4 hzbOffsets3;
        public uint instanceCount;
        public uint maxVisibleMeshlets;
        public float lodErrorThreshold;
        public uint threadGroupCount;
        public uint entrypointIndex;
        public ProgramIdentifier programIdentifier;
        public ulong backingMemoryAddress;
        public ulong backingMemorySize;
        public SetWorkGraphFlags flags;
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
        public Identifier<RGTexture> srcTex;
        public Identifier<RGTexture> dstTex;
        public Handle<ComputeShader> shader;
        public uint2 srcOffset;
        public uint2 dstOffset;
        public uint srcWidth;
        public uint srcHeight;
        public uint dstWidth;
        public uint dstHeight;
    }

    internal partial class CullingResource : IPipelineResource
    {
        [ResolveAsset("EngineResources/Shaders/MeshletCullGraph")]
        public IAssetEntry cullWorkGraphEntry = null!;

        [ResolveAsset("EngineResources/Shaders/BuildHZB")]
        public Handle<ComputeShader> buildHZBShader;

        [ResolveAsset("EngineResources/Shaders/PrepareMeshletIndirectArgs")]
        public Handle<ComputeShader> prepareIndirectArgsShader;

        [ResolveAsset("EngineResources/Shaders/VisibilityBuffer")]
        public Handle<Shader> visibilityShader;

        [ResolveAsset("EngineResources/Shaders/VisibilityBufferMasked")]
        public Handle<Shader> visibilityMaskedShader;

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

    public static class CullConstants
    {
        public const uint COUNTER_BUFFER_SIZE = 128;
        public const uint INDIRECT_ARGS_BUFFER_SIZE = 64;

        // Counter byte offsets matching CullCommon.hlsl
        public const uint OFFSET_PASS1_OPAQUE_COUNT = 0;
        public const uint OFFSET_PASS1_OCCLUDED_COUNT = 4;
        public const uint OFFSET_PASS2_OPAQUE_COUNT = 8;
        public const uint OFFSET_PASS1_MASKED_COUNT = 12;
        public const uint OFFSET_PASS2_MASKED_COUNT = 16;

        // Indirect draw arguments byte offsets matching CullCommon.hlsl
        public const uint INDIRECT_OFFSET_PASS1_OPAQUE = 0;
        public const uint INDIRECT_OFFSET_PASS1_MASKED = 16;
        public const uint INDIRECT_OFFSET_PASS2_OPAQUE = 32;
        public const uint INDIRECT_OFFSET_PASS2_MASKED = 48;
    }

    private readonly CullingResource _cullingResource = new();
    private static ICommandSignature s_dispatchMeshCommandSignature = null!;

    private void InitializeCulling(RenderEngine renderEngine, AssetManager assetManager)
    {
        _cullingResource.Resolve(assetManager);

        var indirectDesc = new CommandSignatureDesc
        {
            Stride = 12,
            Arguments = new IndirectArgumentDesc[]
            {
                new() { Type = IndirectArgumentType.DispatchMesh }
            }
        };
        s_dispatchMeshCommandSignature = renderEngine.GraphicsEngine.CreateCommandSignature(in indirectDesc, default);
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
            visibleMaskedMeshletsPass1 = rg.CreateBuffer(in visibleDesc, "VisibleMaskedMeshlets_Pass1"),
            visibleMeshletsPass2 = rg.CreateBuffer(in visibleDesc, "VisibleMeshlets_Pass2"),
            visibleMaskedMeshletsPass2 = rg.CreateBuffer(in visibleDesc, "VisibleMaskedMeshlets_Pass2"),
            occludedMeshlets = rg.CreateBuffer(in occludedDesc, "OccludedInstances"),
            counterBuffer = rg.CreateBuffer(in counterDesc, "CullCounters"),
            indirectArgsBuffer = rg.CreateBuffer(in indirectDesc, "MeshletIndirectArgs")
        };

        rg.AddClearBufferPass(buffers.counterBuffer, CullConstants.COUNTER_BUFFER_SIZE, 0, "InitCullingCounters");
    }

    private unsafe void AddMeshletCullPass1(
        RenderGraph rg,
        in CameraCullingBuffers buffers,
        Identifier<RGTexture> hzbAtlas,
        uint hzbMipCount,
        uint screenWidth,
        uint screenHeight,
        in uint4 o0,
        in uint4 o1,
        in uint4 o2,
        in uint4 o3,
        uint instanceCount)
    {
        _cullingResource.EnsureWorkGraphProgram(_renderEngine);
        if (_cullingResource.cullWorkGraphProgram == null)
        {
            return;
        }

        var cullProgram = _cullingResource.cullWorkGraphProgram;
        var flags = SetWorkGraphFlags.Initialize;

        var entrypointIndex = cullProgram.GetEntrypointIndex("InstanceCullNode");
        if (entrypointIndex == uint.MaxValue)
        {
            entrypointIndex = 0;
        }

        using var builder = rg.AddComputeRenderPass<MeshletCullPass1Data>("MeshletCull_Pass1");
        builder.UseBuffer(buffers.visibleMeshletsPass1, AccessFlags.Write);
        builder.UseBuffer(buffers.visibleMaskedMeshletsPass1, AccessFlags.Write);
        builder.UseBuffer(buffers.occludedMeshlets, AccessFlags.Write);
        builder.UseBuffer(buffers.counterBuffer, AccessFlags.ReadWrite);

        if (hzbAtlas.IsValid)
        {
            builder.UseTexture(hzbAtlas, AccessFlags.Read);
        }

        var passData = new MeshletCullPass1Data
        {
            visibleMeshletsPass1 = buffers.visibleMeshletsPass1,
            visibleMaskedMeshletsPass1 = buffers.visibleMaskedMeshletsPass1,
            occludedMeshlets = buffers.occludedMeshlets,
            counterBuffer = buffers.counterBuffer,
            hzbAtlas = hzbAtlas,
            hzbMipCount = hzbMipCount,
            renderWidth = screenWidth,
            renderHeight = screenHeight,
            hzbOffsets0 = o0,
            hzbOffsets1 = o1,
            hzbOffsets2 = o2,
            hzbOffsets3 = o3,
            instanceCount = instanceCount,
            maxVisibleMeshlets = _settings.MaxVisibleMeshletsOnScreen,
            lodErrorThreshold = _settings.MeshletLodErrorThreshold,
            threadGroupCount = Math.Max(1u, (instanceCount + 63) / 64),
            entrypointIndex = entrypointIndex,
            programIdentifier = cullProgram.ProgramIdentifier,
            backingMemoryAddress = cullProgram.BackingMemoryAddress,
            backingMemorySize = cullProgram.BackingMemorySize,
            flags = flags
        };

        builder.SetPassData(passData);
        builder.SetRenderFunc<MeshletCullPass1Data>(static (ref readonly passData, computeCtx) =>
        {
            var visibleUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visibleMeshletsPass1).AsResource(), BindlessAccess.UnorderedAccess);
            var visibleMaskedUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visibleMaskedMeshletsPass1).AsResource(), BindlessAccess.UnorderedAccess);
            var occludedUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.occludedMeshlets).AsResource(), BindlessAccess.UnorderedAccess);
            var counterUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.counterBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            var hzbAtlasIndex = (passData.hzbMipCount > 0 && passData.hzbAtlas.IsValid)
                ? computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualTexture(passData.hzbAtlas).AsResource(), BindlessAccess.ShaderResource)
                : uint.MaxValue;

            var props = new InternalMeshletCullGraphShaderProperties
            {
                hzbMipCount = passData.hzbMipCount,
                renderWidth = passData.renderWidth,
                renderHeight = passData.renderHeight,
                cullPassIndex = 0,
                visibleMeshletsUav = visibleUav,
                visibleMaskedMeshletsUav = visibleMaskedUav,
                occludedMeshletsUav = occludedUav,
                counterBufferUav = counterUav,
                maxVisibleMeshlets = passData.maxVisibleMeshlets,
                lodErrorThreshold = passData.lodErrorThreshold,
                instanceCount = passData.instanceCount,
                hzbAtlas = hzbAtlasIndex,
                hzbOffsets0 = passData.hzbOffsets0,
                hzbOffsets1 = passData.hzbOffsets1,
                hzbOffsets2 = passData.hzbOffsets2,
                hzbOffsets3 = passData.hzbOffsets3,
            };

            var setProgramDesc = SetProgramDesc.ForWorkGraph(
                passData.programIdentifier,
                passData.backingMemoryAddress,
                passData.backingMemorySize,
                passData.flags);
            computeCtx.SetProgram(in setProgramDesc);
            computeCtx.SetProperties(in props);

            var record = new WorkGraphDispatchGridRecord(passData.threadGroupCount, 1, 1);
            var dispatchDesc = DispatchGraphDesc.ForCPUInput(passData.entrypointIndex, 1, &record, (ulong)sizeof(WorkGraphDispatchGridRecord));
            computeCtx.DispatchGraph(in dispatchDesc);
        });
    }

    private void AddPrepareIndirectArgsPass(RenderGraph rg, in CameraCullingBuffers buffers, uint cullPassIndex)
    {
        if (!_cullingResource.prepareIndirectArgsShader.IsValid)
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
            var opaqueCountOffset = passData.cullPassIndex == 0 ? CullConstants.OFFSET_PASS1_OPAQUE_COUNT : CullConstants.OFFSET_PASS2_OPAQUE_COUNT;
            var opaqueArgsOffset = passData.cullPassIndex == 0 ? CullConstants.INDIRECT_OFFSET_PASS1_OPAQUE : CullConstants.INDIRECT_OFFSET_PASS2_OPAQUE;
            var maskedCountOffset = passData.cullPassIndex == 0 ? CullConstants.OFFSET_PASS1_MASKED_COUNT : CullConstants.OFFSET_PASS2_MASKED_COUNT;
            var maskedArgsOffset = passData.cullPassIndex == 0 ? CullConstants.INDIRECT_OFFSET_PASS1_MASKED : CullConstants.INDIRECT_OFFSET_PASS2_MASKED;

            var counterUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.counterBuffer).AsResource(), BindlessAccess.UnorderedAccess);
            var indirectUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.indirectArgsBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            var props = new InternalPrepareMeshletIndirectArgsShaderProperties
            {
                counterBuffer = counterUav,
                indirectArgsBuffer = indirectUav,
                opaqueCountOffset = opaqueCountOffset,
                opaqueArgsOffset = opaqueArgsOffset,
                maskedCountOffset = maskedCountOffset,
                maskedArgsOffset = maskedArgsOffset,
                maxCount = passData.maxCount,
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetProperties(in props);
            computeCtx.DispatchCompute(1, 1, 1);
        });
    }

    private void AddBuildHZBPasses(
        RenderGraph rg,
        Identifier<RGTexture> depthBuffer,
        Identifier<RGTexture> hzbAtlas,
        uint hzbMipCount,
        uint baseW,
        uint baseH,
        uint renderWidth,
        uint renderHeight)
    {
        // Mip 0 downsamples from DepthBuffer (size: render resolution -> baseW x baseH at (0, 0))
        using (var builder = rg.AddComputeRenderPass<BuildHZBMipPassData>("BuildHZB_Mip0"))
        {
            builder.UseTexture(depthBuffer, AccessFlags.Read);
            builder.UseTexture(hzbAtlas, AccessFlags.Write);

            builder.SetPassData(new BuildHZBMipPassData
            {
                srcTex = depthBuffer,
                dstTex = hzbAtlas,
                shader = _cullingResource.buildHZBShader,
                srcOffset = default,
                dstOffset = default,
                srcWidth = renderWidth,
                srcHeight = renderHeight,
                dstWidth = baseW,
                dstHeight = baseH
            });

            builder.SetRenderFunc<BuildHZBMipPassData>(static (ref readonly passData, computeCtx) =>
            {
                ExecuteHZBDownsamplePass(in passData, computeCtx);
            });
        }

        // Mips 1..N downsample from previous HZB mip in atlas
        var prevW = baseW;
        var prevH = baseH;
        uint srcX = 0;
        uint srcY = 0;
        uint rightY = 0;

        for (uint i = 1; i < hzbMipCount; ++i)
        {
            var curW = Math.Max(1u, prevW / 2);
            var curH = Math.Max(1u, prevH / 2);
            var dstX = baseW;
            var dstY = rightY;
            rightY += curH;

            using var builder = rg.AddComputeRenderPass<BuildHZBMipPassData>(StringUtility.DebugFormat("BuildHZB_Mip{0}", i));
            builder.UseTexture(hzbAtlas, AccessFlags.ReadWrite);

            builder.SetPassData(new BuildHZBMipPassData
            {
                srcTex = hzbAtlas,
                dstTex = hzbAtlas,
                shader = _cullingResource.buildHZBShader,
                srcOffset = new uint2(srcX, srcY),
                dstOffset = new uint2(dstX, dstY),
                srcWidth = prevW,
                srcHeight = prevH,
                dstWidth = curW,
                dstHeight = curH
            });

            builder.SetRenderFunc<BuildHZBMipPassData>(static (ref readonly passData, computeCtx) =>
            {
                ExecuteHZBDownsamplePass(in passData, computeCtx);
            });

            srcX = dstX;
            srcY = dstY;
            prevW = curW;
            prevH = curH;
        }
    }

    private static void ExecuteHZBDownsamplePass(scoped in BuildHZBMipPassData passData, IComputeRenderContext computeCtx)
    {
        var srcActual = computeCtx.GetActualTexture(passData.srcTex);
        var dstActual = computeCtx.GetActualTexture(passData.dstTex);

        var dstIndex = computeCtx.ResourceDatabase.GetBindlessIndex(dstActual.AsResource(), BindlessAccess.UnorderedAccess);
        var srcIndex = (passData.srcTex == passData.dstTex)
            ? dstIndex
            : computeCtx.ResourceDatabase.GetBindlessIndex(srcActual.AsResource(), BindlessAccess.ShaderResource);

        var props = new InternalBuildHZBShaderProperties
        {
            srcTexture = srcIndex,
            dstTexture = dstIndex,
            srcOffset = passData.srcOffset,
            dstOffset = passData.dstOffset,
            srcWidth = passData.srcWidth,
            srcHeight = passData.srcHeight,
            dstWidth = passData.dstWidth,
            dstHeight = passData.dstHeight,
        };

        computeCtx.SetActiveCompute(passData.shader, 0);
        computeCtx.SetProperties(in props);

        var threadGroupsX = Math.Max(1u, (passData.dstWidth + 7) / 8);
        var threadGroupsY = Math.Max(1u, (passData.dstHeight + 7) / 8);
        computeCtx.DispatchCompute(threadGroupsX, threadGroupsY, 1);
    }

    private unsafe void AddMeshletCullPass2(
        RenderGraph rg,
        in CameraCullingBuffers buffers,
        Identifier<RGTexture> hzbAtlas,
        uint hzbMipCount,
        uint renderWidth,
        uint renderHeight,
        in uint4 o0,
        in uint4 o1,
        in uint4 o2,
        in uint4 o3,
        uint instanceCount)
    {
        _cullingResource.EnsureWorkGraphProgram(_renderEngine);
        if (_cullingResource.cullWorkGraphProgram == null)
        {
            return;
        }

        var cullProgram = _cullingResource.cullWorkGraphProgram;
        var flags = SetWorkGraphFlags.Initialize;

        var entrypointIndex = cullProgram.GetEntrypointIndex("OccludedMeshletCullNode");
        if (entrypointIndex == uint.MaxValue)
        {
            entrypointIndex = 1;
        }

        using var builder = rg.AddComputeRenderPass<MeshletCullPass2Data>("MeshletCull_Pass2");
        builder.UseBuffer(buffers.occludedMeshlets, AccessFlags.Read);
        builder.UseBuffer(buffers.visibleMeshletsPass2, AccessFlags.Write);
        builder.UseBuffer(buffers.visibleMaskedMeshletsPass2, AccessFlags.Write);
        builder.UseBuffer(buffers.counterBuffer, AccessFlags.ReadWrite);

        if (hzbAtlas.IsValid)
        {
            builder.UseTexture(hzbAtlas, AccessFlags.Read);
        }

        var passData = new MeshletCullPass2Data
        {
            visibleMeshletsPass2 = buffers.visibleMeshletsPass2,
            visibleMaskedMeshletsPass2 = buffers.visibleMaskedMeshletsPass2,
            occludedMeshlets = buffers.occludedMeshlets,
            counterBuffer = buffers.counterBuffer,
            hzbAtlas = hzbAtlas,
            hzbMipCount = hzbMipCount,
            renderWidth = renderWidth,
            renderHeight = renderHeight,
            hzbOffsets0 = o0,
            hzbOffsets1 = o1,
            hzbOffsets2 = o2,
            hzbOffsets3 = o3,
            instanceCount = instanceCount,
            maxVisibleMeshlets = _settings.MaxVisibleMeshletsOnScreen,
            lodErrorThreshold = _settings.MeshletLodErrorThreshold,
            threadGroupCount = Math.Max(1u, Math.Min(16384u, (instanceCount * 2048u + 63) / 64)),
            entrypointIndex = entrypointIndex,
            programIdentifier = cullProgram.ProgramIdentifier,
            backingMemoryAddress = cullProgram.BackingMemoryAddress,
            backingMemorySize = cullProgram.BackingMemorySize,
            flags = flags
        };

        builder.SetPassData(passData);
        builder.SetRenderFunc<MeshletCullPass2Data>(static (ref readonly passData, computeCtx) =>
        {
            var visibleUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visibleMeshletsPass2).AsResource(), BindlessAccess.UnorderedAccess);
            var visibleMaskedUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visibleMaskedMeshletsPass2).AsResource(), BindlessAccess.UnorderedAccess);
            var occludedSrv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.occludedMeshlets).AsResource(), BindlessAccess.ShaderResource);
            var counterUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.counterBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            var hzbAtlasIndex = (passData.hzbMipCount > 0 && passData.hzbAtlas.IsValid)
                ? computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualTexture(passData.hzbAtlas).AsResource(), BindlessAccess.ShaderResource)
                : uint.MaxValue;

            var props = new InternalMeshletCullGraphShaderProperties
            {
                hzbMipCount = passData.hzbMipCount,
                renderWidth = passData.renderWidth,
                renderHeight = passData.renderHeight,
                cullPassIndex = 1,
                visibleMeshletsUav = visibleUav,
                visibleMaskedMeshletsUav = visibleMaskedUav,
                occludedMeshletsUav = occludedSrv,
                counterBufferUav = counterUav,
                maxVisibleMeshlets = passData.maxVisibleMeshlets,
                lodErrorThreshold = passData.lodErrorThreshold,
                instanceCount = passData.instanceCount,
                hzbAtlas = hzbAtlasIndex,
                hzbOffsets0 = passData.hzbOffsets0,
                hzbOffsets1 = passData.hzbOffsets1,
                hzbOffsets2 = passData.hzbOffsets2,
                hzbOffsets3 = passData.hzbOffsets3,
            };

            var setProgramDesc = SetProgramDesc.ForWorkGraph(
                passData.programIdentifier,
                passData.backingMemoryAddress,
                passData.backingMemorySize,
                passData.flags);
            computeCtx.SetProgram(in setProgramDesc);
            computeCtx.SetProperties(in props);

            var record = new WorkGraphDispatchGridRecord(passData.threadGroupCount, 1, 1);
            var dispatchDesc = DispatchGraphDesc.ForCPUInput(passData.entrypointIndex, 1, &record, (ulong)sizeof(WorkGraphDispatchGridRecord));
            computeCtx.DispatchGraph(in dispatchDesc);
        });
    }

    private void DisposeCulling()
    {
        _cullingResource.Dispose();

        s_dispatchMeshCommandSignature?.Dispose();
        s_dispatchMeshCommandSignature = null!;
    }
}
