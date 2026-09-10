using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.ShaderProperties;
using Ghost.Engine.Streaming;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;
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

    [InlineArray(16)]
    private struct HZBMipIdentifiers
    {
        private Identifier<RGTexture> _element0;
    }

    public struct CameraCullingBuffers
    {
        public Identifier<RGBuffer> visibleMeshletsPass1;
        public Identifier<RGBuffer> visibleMeshletsPass2;
        public Identifier<RGBuffer> occludedMeshlets;
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
    }

    private struct InitializeCullingBuffersPassData
    {
        public Identifier<RGBuffer> zeroBuffer;
        public Identifier<RGBuffer> counterBuffer;
    }

    private struct MeshletCullPass1Data
    {
        public Identifier<RGBuffer> visibleMeshletsPass1;
        public Identifier<RGBuffer> occludedMeshlets;
        public Identifier<RGBuffer> counterBuffer;
        public HZBMipHandles importedHzbTextures;
        public uint hzbMipCount;
        public uint hzbWidth;
        public uint hzbHeight;
        public uint instanceCount;
        public uint maxVisibleMeshlets;
        public uint threadGroupCount;
        public ProgramIdentifier programIdentifier;
        public ulong backingMemoryAddress;
        public ulong backingMemorySize;
        public SetWorkGraphFlags flags;
    }

    private struct MeshletCullPass2Data
    {
        public Identifier<RGBuffer> visibleMeshletsPass2;
        public Identifier<RGBuffer> occludedMeshlets;
        public Identifier<RGBuffer> counterBuffer;
        public HZBMipIdentifiers hzbMips;
        public uint hzbMipCount;
        public uint hzbWidth;
        public uint hzbHeight;
        public uint maxVisibleMeshlets;
        public uint threadGroupCount;
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

        public IWorkGraphProgram? CullWorkGraphProgram;

        public void EnsureWorkGraphProgram(RenderEngine renderEngine)
        {
            if (CullWorkGraphProgram != null)
            {
                return;
            }

            if (cullWorkGraphEntry is WorkGraphAssetEntry wgEntry && !wgEntry.Bytecode.IsEmpty)
            {
                CullWorkGraphProgram = renderEngine.GraphicsEngine.CreateWorkGraphProgram(wgEntry.Bytecode, "MeshletCullGraph");
            }
        }

        partial void OnDisposing()
        {
            CullWorkGraphProgram?.Dispose();
            CullWorkGraphProgram = null;
        }
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

    private unsafe void AddInitializeCullingBuffersPass(
        RenderGraph rg,
        out CameraCullingBuffers buffers,
        uint maxVisibleMeshlets = 1_048_576)
    {
        using var builder = rg.AddUnsafeRenderPass<InitializeCullingBuffersPassData>("InitCullingBuffers");

        var entrySize = (uint)sizeof(VisibleMeshletEntry);
        var bufferSize = maxVisibleMeshlets * entrySize;

        var visibleDesc = new BufferDesc
        {
            Size = bufferSize,
            Stride = entrySize,
            Usage = BufferUsage.Structured | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };

        var counterDesc = new BufferDesc
        {
            Size = 16,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };

        var indirectDesc = new BufferDesc
        {
            Size = 32,
            Stride = 4,
            Usage = BufferUsage.IndirectArgument | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };

        buffers = new CameraCullingBuffers
        {
            visibleMeshletsPass1 = builder.CreateBuffer(in visibleDesc, "VisibleMeshlets_Pass1"),
            visibleMeshletsPass2 = builder.CreateBuffer(in visibleDesc, "VisibleMeshlets_Pass2"),
            occludedMeshlets = builder.CreateBuffer(in visibleDesc, "OccludedMeshlets"),
            counterBuffer = builder.CreateBuffer(in counterDesc, "CullCounters"),
            indirectArgsBuffer = builder.CreateBuffer(in indirectDesc, "MeshletIndirectArgs")
        };

        var uploadDesc = new BufferDesc
        {
            Size = 16,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Upload
        };
        var zeroBuffer = builder.CreateBuffer(in uploadDesc, "ZeroCountersUpload");

        builder.UseBuffer(zeroBuffer, AccessFlags.Read);
        builder.UseRandomAccessBuffer(buffers.counterBuffer);
        builder.SetPassData(new InitializeCullingBuffersPassData
        {
            zeroBuffer = zeroBuffer,
            counterBuffer = buffers.counterBuffer
        });

        builder.SetRenderFunc<InitializeCullingBuffersPassData>(static (ref readonly passData, unsafeCtx) =>
        {
            uint* pData = stackalloc uint[4] { 0, 0, 0, 0 };
            unsafeCtx.WriteBuffer(passData.zeroBuffer, pData, 16);

            var actualCounter = unsafeCtx.GetActualBuffer(passData.counterBuffer);
            var actualZero = unsafeCtx.GetActualBuffer(passData.zeroBuffer);
            unsafeCtx.GetCommandBufferUnsafe().CopyBuffer(actualCounter, actualZero, 0, 0, 16);
        });
    }

    private unsafe void AddMeshletCullPass1(
        RenderGraph rg,
        in CameraCullingBuffers buffers,
        ref readonly RenderRequest request,
        uint instanceCount,
        uint maxVisibleMeshlets = 1_048_576)
    {
        _cullingResource.EnsureWorkGraphProgram(_renderEngine);
        if (_cullingResource.CullWorkGraphProgram == null)
        {
            Logger.Warning("Meshlet work graph program is not ready. Skipping work graph culling pass 1.");
            return;
        }

        var cullProgram = _cullingResource.CullWorkGraphProgram;
        var flags = cullProgram.IsInitialized ? SetWorkGraphFlags.None : SetWorkGraphFlags.Initialize;
        cullProgram.MarkInitialized();

        using var builder = rg.AddComputeRenderPass<MeshletCullPass1Data>("MeshletCull_Pass1");
        builder.UseBuffer(buffers.visibleMeshletsPass1, AccessFlags.Write);
        builder.UseBuffer(buffers.occludedMeshlets, AccessFlags.Write);
        builder.UseBuffer(buffers.counterBuffer, AccessFlags.ReadWrite);

        var passData = new MeshletCullPass1Data
        {
            visibleMeshletsPass1 = buffers.visibleMeshletsPass1,
            occludedMeshlets = buffers.occludedMeshlets,
            counterBuffer = buffers.counterBuffer,
            hzbMipCount = 0,
            hzbWidth = 0,
            hzbHeight = 0,
            instanceCount = instanceCount,
            maxVisibleMeshlets = maxVisibleMeshlets,
            threadGroupCount = Math.Max(1u, (instanceCount + 63) / 64),
            programIdentifier = cullProgram.ProgramIdentifier,
            backingMemoryAddress = cullProgram.BackingMemoryAddress,
            backingMemorySize = cullProgram.BackingMemorySize,
            flags = flags
        };

        if (request.hasValidHistory && request.hzbMipCount > 0)
        {
            passData.hzbMipCount = request.hzbMipCount;
            passData.hzbWidth = Math.Max(1u, request.historyWidth / 2);
            passData.hzbHeight = Math.Max(1u, request.historyHeight / 2);

            for (int m = 0; m < (int)request.hzbMipCount; m++)
            {
                var imported = rg.ImportTexture(request.hzbHistory[m]);
                builder.UseTexture(imported, AccessFlags.Read);
                passData.importedHzbTextures[m] = request.hzbHistory[m];
            }
        }

        builder.SetPassData(passData);
        builder.SetRenderFunc<MeshletCullPass1Data>(static (ref readonly passData, computeCtx) =>
        {
            var visibleUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visibleMeshletsPass1).AsResource(), BindlessAccess.UnorderedAccess);
            var occludedUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.occludedMeshlets).AsResource(), BindlessAccess.UnorderedAccess);
            var counterUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.counterBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            var props = new InternalMeshletCullGraphShaderProperties
            {
                hzbMipCount = passData.hzbMipCount,
                hzbWidth = passData.hzbWidth,
                hzbHeight = passData.hzbHeight,
                cullPassIndex = 0,
                visibleMeshletsUav = visibleUav,
                occludedMeshletsUav = occludedUav,
                counterBufferUav = counterUav,
                maxVisibleMeshlets = passData.maxVisibleMeshlets,
                lodErrorThreshold = 2.0f,
                instanceCount = passData.instanceCount,
                hzbMip0 = passData.hzbMipCount > 0 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[0].AsResource()) : uint.MaxValue,
                hzbMip1 = passData.hzbMipCount > 1 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[1].AsResource()) : uint.MaxValue,
                hzbMip2 = passData.hzbMipCount > 2 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[2].AsResource()) : uint.MaxValue,
                hzbMip3 = passData.hzbMipCount > 3 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[3].AsResource()) : uint.MaxValue,
                hzbMip4 = passData.hzbMipCount > 4 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[4].AsResource()) : uint.MaxValue,
                hzbMip5 = passData.hzbMipCount > 5 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[5].AsResource()) : uint.MaxValue,
                hzbMip6 = passData.hzbMipCount > 6 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[6].AsResource()) : uint.MaxValue,
                hzbMip7 = passData.hzbMipCount > 7 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[7].AsResource()) : uint.MaxValue,
                hzbMip8 = passData.hzbMipCount > 8 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[8].AsResource()) : uint.MaxValue,
                hzbMip9 = passData.hzbMipCount > 9 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[9].AsResource()) : uint.MaxValue,
                hzbMip10 = passData.hzbMipCount > 10 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[10].AsResource()) : uint.MaxValue,
                hzbMip11 = passData.hzbMipCount > 11 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[11].AsResource()) : uint.MaxValue,
                hzbMip12 = passData.hzbMipCount > 12 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[12].AsResource()) : uint.MaxValue,
                hzbMip13 = passData.hzbMipCount > 13 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[13].AsResource()) : uint.MaxValue,
                hzbMip14 = passData.hzbMipCount > 14 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[14].AsResource()) : uint.MaxValue,
                hzbMip15 = passData.hzbMipCount > 15 ? computeCtx.ResourceDatabase.GetBindlessIndex(passData.importedHzbTextures[15].AsResource()) : uint.MaxValue,
            };

            computeCtx.SetProperties(in props);

            var setProgramDesc = SetProgramDesc.ForWorkGraph(
                passData.programIdentifier,
                passData.backingMemoryAddress,
                passData.backingMemorySize,
                passData.flags);
            computeCtx.SetProgram(in setProgramDesc);

            var dispatchDesc = DispatchGraphDesc.ForEmptyCPUInput(0, passData.threadGroupCount);
            computeCtx.DispatchGraph(in dispatchDesc);
        });
    }

    private unsafe void AddPrepareIndirectArgsPass(
        RenderGraph rg,
        in CameraCullingBuffers buffers,
        uint cullPassIndex,
        uint maxVisibleMeshlets = 1_048_576)
    {
        if (!_cullingResource.prepareIndirectArgsShader.IsValid)
        {
            return;
        }

        using var builder = rg.AddComputeRenderPass<PrepareIndirectArgsPassData>($"PrepareIndirectArgs_Pass{cullPassIndex + 1}");
        builder.UseBuffer(buffers.counterBuffer, AccessFlags.Read);
        builder.UseBuffer(buffers.indirectArgsBuffer, AccessFlags.Write);

        builder.SetPassData(new PrepareIndirectArgsPassData
        {
            counterBuffer = buffers.counterBuffer,
            indirectArgsBuffer = buffers.indirectArgsBuffer,
            shader = _cullingResource.prepareIndirectArgsShader,
            cullPassIndex = cullPassIndex,
            maxCount = maxVisibleMeshlets
        });

        builder.SetRenderFunc<PrepareIndirectArgsPassData>(static (ref readonly passData, computeCtx) =>
        {
            var countOffset = passData.cullPassIndex == 0 ? 0u : 8u;
            var argsOffset = passData.cullPassIndex == 0 ? 0u : 16u;

            var counterUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.counterBuffer).AsResource(), BindlessAccess.UnorderedAccess);
            var indirectUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.indirectArgsBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            var props = new InternalPrepareMeshletIndirectArgsShaderProperties
            {
                counterBuffer = counterUav,
                indirectArgsBuffer = indirectUav,
                countOffset = countOffset,
                argsOffset = argsOffset,
                maxCount = passData.maxCount,
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetProperties(in props);
            computeCtx.DispatchCompute(1, 1, 1);
        });
    }

    private unsafe void AddBuildHZBPasses(
        RenderGraph rg,
        Identifier<RGTexture> depthBuffer,
        ref readonly RenderRequest request,
        uint renderWidth,
        uint renderHeight,
        out Identifier<RGTexture>[] hzbMips,
        out uint hzbMipCount)
    {
        var baseW = Math.Max(1u, renderWidth / 2);
        var baseH = Math.Max(1u, renderHeight / 2);
        var calculatedMipCount = (uint)Math.Floor(Math.Log2(Math.Max(baseW, baseH))) + 1;
        hzbMipCount = Math.Clamp(calculatedMipCount, 1u, 16u);

        hzbMips = new Identifier<RGTexture>[hzbMipCount];

        // Mip 0 downsamples from DepthBuffer (size: render resolution -> baseW x baseH)
        var mip0Desc = RGTextureDesc.Absolute(
            baseW,
            baseH,
            TextureFormat.R32_Float,
            usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);

        using (var builder = rg.AddComputeRenderPass<BuildHZBMipPassData>("BuildHZB_Mip0"))
        {
            hzbMips[0] = builder.CreateTexture(in mip0Desc, "HZBMip_0");
            builder.UseTexture(depthBuffer, AccessFlags.Read);
            builder.UseTexture(hzbMips[0], AccessFlags.Write);

            builder.QueueTextureExtraction(depthBuffer, request.depthTarget, ResourceExtractionFlags.ReleaseAfterExtract);
            builder.QueueTextureExtraction(hzbMips[0], request.hzbHistory[0], ResourceExtractionFlags.ReleaseAfterExtract);

            builder.SetPassData(new BuildHZBMipPassData
            {
                srcTex = depthBuffer,
                dstTex = hzbMips[0],
                shader = _cullingResource.buildHZBShader,
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

        // Mips 1..N downsample from previous HZB mip
        var prevW = baseW;
        var prevH = baseH;
        for (uint i = 1; i < hzbMipCount; ++i)
        {
            var curW = Math.Max(1u, prevW / 2);
            var curH = Math.Max(1u, prevH / 2);

            var mipDesc = RGTextureDesc.Absolute(
                curW,
                curH,
                TextureFormat.R32_Float,
                usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);

            using var builder = rg.AddComputeRenderPass<BuildHZBMipPassData>($"BuildHZB_Mip{i}");
            hzbMips[i] = builder.CreateTexture(in mipDesc, $"HZBMip_{i}");
            builder.UseTexture(hzbMips[i - 1], AccessFlags.Read);
            builder.UseTexture(hzbMips[i], AccessFlags.Write);

            builder.QueueTextureExtraction(hzbMips[i], request.hzbHistory[(int)i], ResourceExtractionFlags.ReleaseAfterExtract);

            builder.SetPassData(new BuildHZBMipPassData
            {
                srcTex = hzbMips[i - 1],
                dstTex = hzbMips[i],
                shader = _cullingResource.buildHZBShader,
                srcWidth = prevW,
                srcHeight = prevH,
                dstWidth = curW,
                dstHeight = curH
            });

            builder.SetRenderFunc<BuildHZBMipPassData>(static (ref readonly passData, computeCtx) =>
            {
                ExecuteHZBDownsamplePass(in passData, computeCtx);
            });

            prevW = curW;
            prevH = curH;
        }
    }

    private static unsafe void ExecuteHZBDownsamplePass(scoped in BuildHZBMipPassData passData, IComputeRenderContext computeCtx)
    {
        var srcActual = computeCtx.GetActualTexture(passData.srcTex);
        var dstActual = computeCtx.GetActualTexture(passData.dstTex);

        var srcIndex = computeCtx.ResourceDatabase.GetBindlessIndex(srcActual.AsResource(), BindlessAccess.ShaderResource);
        var dstIndex = computeCtx.ResourceDatabase.GetBindlessIndex(dstActual.AsResource(), BindlessAccess.UnorderedAccess);

        var props = new InternalBuildHZBShaderProperties
        {
            srcTexture = srcIndex,
            dstTexture = dstIndex,
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
        ReadOnlySpan<Identifier<RGTexture>> hzbMips,
        uint hzbMipCount,
        uint renderWidth,
        uint renderHeight,
        uint maxVisibleMeshlets = 1_048_576)
    {
        _cullingResource.EnsureWorkGraphProgram(_renderEngine);
        if (_cullingResource.CullWorkGraphProgram == null)
        {
            Logger.Warning("Meshlet work graph program is not ready. Skipping work graph culling pass 2.");
            return;
        }

        var cullProgram = _cullingResource.CullWorkGraphProgram;
        var flags = cullProgram.IsInitialized ? SetWorkGraphFlags.None : SetWorkGraphFlags.Initialize;
        cullProgram.MarkInitialized();

        using var builder = rg.AddComputeRenderPass<MeshletCullPass2Data>("MeshletCull_Pass2");
        builder.UseBuffer(buffers.occludedMeshlets, AccessFlags.Read);
        builder.UseBuffer(buffers.visibleMeshletsPass2, AccessFlags.Write);
        builder.UseBuffer(buffers.counterBuffer, AccessFlags.ReadWrite);

        var passData = new MeshletCullPass2Data
        {
            visibleMeshletsPass2 = buffers.visibleMeshletsPass2,
            occludedMeshlets = buffers.occludedMeshlets,
            counterBuffer = buffers.counterBuffer,
            hzbMipCount = hzbMipCount,
            hzbWidth = Math.Max(1u, renderWidth / 2),
            hzbHeight = Math.Max(1u, renderHeight / 2),
            maxVisibleMeshlets = maxVisibleMeshlets,
            threadGroupCount = Math.Max(1u, (maxVisibleMeshlets + 63) / 64),
            programIdentifier = cullProgram.ProgramIdentifier,
            backingMemoryAddress = cullProgram.BackingMemoryAddress,
            backingMemorySize = cullProgram.BackingMemorySize,
            flags = flags
        };

        for (int m = 0; m < (int)hzbMipCount && m < 16; m++)
        {
            builder.UseTexture(hzbMips[m], AccessFlags.Read);
            passData.hzbMips[m] = hzbMips[m];
        }

        builder.SetPassData(passData);
        builder.SetRenderFunc<MeshletCullPass2Data>(static (ref readonly passData, computeCtx) =>
        {
            var visibleUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visibleMeshletsPass2).AsResource(), BindlessAccess.UnorderedAccess);
            var occludedSrv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.occludedMeshlets).AsResource(), BindlessAccess.ShaderResource);
            var counterUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.counterBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            uint* hzbIndices = stackalloc uint[16];
            for (int i = 0; i < 16; i++)
            {
                if (i < (int)passData.hzbMipCount && passData.hzbMips[i].IsValid)
                {
                    var tex = computeCtx.GetActualTexture(passData.hzbMips[i]);
                    hzbIndices[i] = computeCtx.ResourceDatabase.GetBindlessIndex(tex.AsResource(), BindlessAccess.ShaderResource);
                }
                else
                {
                    hzbIndices[i] = uint.MaxValue;
                }
            }

            var props = new InternalMeshletCullGraphShaderProperties
            {
                hzbMipCount = passData.hzbMipCount,
                hzbWidth = passData.hzbWidth,
                hzbHeight = passData.hzbHeight,
                cullPassIndex = 1,
                visibleMeshletsUav = visibleUav,
                occludedMeshletsUav = occludedSrv,
                counterBufferUav = counterUav,
                maxVisibleMeshlets = passData.maxVisibleMeshlets,
                lodErrorThreshold = 2.0f,
                instanceCount = passData.maxVisibleMeshlets,
                hzbMip0 = hzbIndices[0],
                hzbMip1 = hzbIndices[1],
                hzbMip2 = hzbIndices[2],
                hzbMip3 = hzbIndices[3],
                hzbMip4 = hzbIndices[4],
                hzbMip5 = hzbIndices[5],
                hzbMip6 = hzbIndices[6],
                hzbMip7 = hzbIndices[7],
                hzbMip8 = hzbIndices[8],
                hzbMip9 = hzbIndices[9],
                hzbMip10 = hzbIndices[10],
                hzbMip11 = hzbIndices[11],
                hzbMip12 = hzbIndices[12],
                hzbMip13 = hzbIndices[13],
                hzbMip14 = hzbIndices[14],
                hzbMip15 = hzbIndices[15],
            };

            computeCtx.SetProperties(in props);

            var setProgramDesc = SetProgramDesc.ForWorkGraph(
                passData.programIdentifier,
                passData.backingMemoryAddress,
                passData.backingMemorySize,
                passData.flags);
            computeCtx.SetProgram(in setProgramDesc);

            // Program entry 1: OccludedMeshletCullNode
            var dispatchDesc = DispatchGraphDesc.ForEmptyCPUInput(1, passData.threadGroupCount);
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
