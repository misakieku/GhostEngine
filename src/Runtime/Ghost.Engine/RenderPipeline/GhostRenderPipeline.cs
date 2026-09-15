using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.ShaderProperties;
using Ghost.Engine.Streaming;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

internal unsafe partial class GhostRenderPipeline : IRenderPipeline
{
    private struct VisibilityPassData
    {
        public Identifier<RGBuffer> visibleMeshlets;
        public Identifier<RGBuffer> visibleMaskedMeshlets;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public ulong opaqueArgsOffset;
        public ulong maskedArgsOffset;
        public Handle<Shader> visibilityShader;
        public Handle<Shader> visibilityMaskedShader;
        public uint passIndex;
    }

    private struct MeshletDebugPassData
    {
        public Identifier<RGBuffer> visibleMeshlets;
        public Identifier<RGBuffer> visibleMaskedMeshlets;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public ulong opaqueArgsOffset;
        public ulong maskedArgsOffset;
        public Handle<Shader> debugShader;
        public uint passIndex;
    }

    private struct BlitPassData
    {
        public Identifier<RGTexture> srcBuffer;
        public Handle<Shader> blitShader;
    }

    private readonly RenderEngine _renderEngine;
    private readonly AssetManager _assetManager;
    private readonly GhostRenderPipelineSettings _settings;
    private readonly GPUSceneResource _gpuSceneResource;

    private readonly GPUScene _gpuScene;
    private readonly GPUViewManager _gpuViewManager;

    private bool _disposed;
    private int _lastRenderRequestCount = -1;
    private uint _lastInstanceCount = uint.MaxValue;

    public GPUScene GPUScene => _gpuScene;
    public GPUViewManager GPUViewManager => _gpuViewManager;
    public GhostRenderPipelineSettings Settings => _settings;

    public GhostRenderPipeline(RenderEngine renderEngine, AssetManager assetManager, GhostRenderPipelineSettings? settings = null)
    {
        _renderEngine = renderEngine;
        _assetManager = assetManager;
        _settings = settings ?? new GhostRenderPipelineSettings();

        _gpuSceneResource = new GPUSceneResource(assetManager);
        _gpuSceneResource.Resolve();

        _gpuScene = new GPUScene(renderEngine.GraphicsEngine.ResourceAllocator, renderEngine.GraphicsEngine.ResourceDatabase, 102_400u); // 102.4k objects should be enough for now
        _gpuViewManager = new GPUViewManager(renderEngine.GraphicsEngine.ResourceDatabase);

        InitializeCulling(renderEngine, assetManager);
    }

    public IRenderPayload CreatePayload()
    {
        return new GhostRenderPayload(this);
    }

    public void RecordPrelude(RenderContext ctx, int frameIndex, IRenderPayload payload)
    {
        var ghostPayload = (GhostRenderPayload)payload;

        // Upload dirty material palette tables to the GPU before any rendering or shading
        ctx.ResourceManager.UploadMaterialPaletteData(ctx);

        // Update GPU scene instance buffer once per frame prelude
        UpdateGPUScene(ctx, ghostPayload);
    }

    public Result ExecuteGraph(RenderContext ctx, int frameIndex, IRenderPayload payload, in RenderGraphExecutionContext executionContext)
    {
        var ghostPayload = (GhostRenderPayload)payload;
        if (_lastRenderRequestCount != ghostPayload.RenderRequests.Length || _lastInstanceCount != ghostPayload.InstanceCount)
        {
            _lastRenderRequestCount = ghostPayload.RenderRequests.Length;
            _lastInstanceCount = ghostPayload.InstanceCount;
        }

        if (ghostPayload.RenderRequests.Length == 0)
        {
            return Result.Success();
        }

        // Upload FrameData once per frame
        var frameBufferIndex = RenderPipelineUtility.CreateFrameBuffer(ctx, _gpuScene.SceneBufferSrvIndex);

        for (var requestIndex = 0; requestIndex < ghostPayload.RenderRequests.Length; requestIndex++)
        {
            ref readonly var request = ref ghostPayload.RenderRequests[requestIndex];
            using var viewData = new RenderViewData(_renderEngine.SwapChainManager, ctx.ResourceDatabase, in request);
            var viewState = new ViewState(viewData.ScreenSize.x, viewData.ScreenSize.y, viewData.ScreenSize.x, viewData.ScreenSize.y);

            // Upload ViewData (Reversed-Z: near=1.0, far=0.0)
            RenderPipelineUtility.GetVPMatricesReversedZ(in request, viewData.ScreenSize, out var viewMatrix, out var projMatrix);
            var viewProjMatrix = math.mul(projMatrix, viewMatrix);

            var viewContext = _gpuViewManager.GetView(request.viewId);
            viewContext.EnsureResources(
                ctx.ResourceAllocator,
                ctx.ResourceDatabase,
                ctx.PipelineLibrary,
                ctx.ResourceManager,
                ctx.ShaderLibrary,
                viewData.ScreenSize.x,
                viewData.ScreenSize.y,
                _settings.HzbMaxMegapixels);

            Logger.DebugAssert(viewContext.renderGraph != null);

            viewContext.renderGraph.Reset();
            viewContext.renderGraph.SetFrameData(frameBufferIndex);

            var isFirstFrame = viewContext.prevViewProjMatrix.Equals(float4x4.zero);
            var prevVP = isFirstFrame ? viewProjMatrix : viewContext.prevViewProjMatrix;

            var viewDataGpu = new ViewData
            {
                viewMatrix = viewMatrix,
                projectionMatrix = projMatrix,
                viewProjectionMatrix = viewProjMatrix,
                preVPMatrix = prevVP,
                cameraPosition = request.view.localToWorld.c3.xyz,
                nearClip = request.view.nearClipPlane,
                cameraDirection = request.view.localToWorld.c2.xyz,
                farClip = request.view.farClipPlane,
                screenSize = new float4(viewData.ScreenSize.x, viewData.ScreenSize.y, 1.0f / viewData.ScreenSize.x, 1.0f / viewData.ScreenSize.y),
            };

            viewContext.prevViewProjMatrix = viewProjMatrix;

            var viewDesc = new BufferDesc
            {
                Size = (uint)sizeof(ViewData),
                Stride = (uint)sizeof(ViewData),
                Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
                HeapType = HeapType.Upload,
            };
            var viewGpuBuffer = ctx.ResourceManager.CreateTransientBuffer(in viewDesc, "ViewDataBuffer");
            var pViewData = (ViewData*)ctx.ResourceDatabase.MapResource(viewGpuBuffer.AsResource(), 0, null);
            *pViewData = viewDataGpu;
            ctx.ResourceDatabase.UnmapResource(viewGpuBuffer.AsResource(), 0, null);
            var viewBufferIndex = ctx.ResourceDatabase.GetBindlessIndex(viewGpuBuffer.AsResource());

            viewContext.renderGraph.SetViewData(viewBufferIndex);

            var presentBarrier = new ResourceBarrierData(
                BarrierLayout.Present,
                BarrierAccess.NoAccess,
                BarrierSync.None);

            var colorTarget = viewContext.renderGraph.ImportTexture(
                viewData.ColorTexture,
                initialState: presentBarrier,
                finalState: presentBarrier,
                clearColor: new Color128(0.05f, 0.05f, 0.05f, 1.0f),
                clearAtFirstUse: true);

            var vbufferDesc = RGTextureDesc.Absolute(
                viewData.ScreenSize.x,
                viewData.ScreenSize.y,
                TextureFormat.R32G32_UInt,
                usage: TextureUsage.RenderTarget | TextureUsage.ShaderResource,
                clearAtFirstUse: true);

            var depthDesc = RGTextureDesc.Absolute(
                viewData.ScreenSize.x,
                viewData.ScreenSize.y,
                TextureFormat.D32_Float,
                usage: TextureUsage.DepthStencil | TextureUsage.ShaderResource,
                clearAtFirstUse: true);

            var hzb = viewContext.renderGraph.ImportTexture(viewContext.hzbTexture);

            // Initialize transient culling & indirect argument buffers for this camera view
            AddInitializeCullingBuffersPass(viewContext.renderGraph, out var cullingBuffers);

            // Pass 1: Early-Z Hierarchical Meshlet Culling (Work Graph)
            AddMeshletCullPass1(
                viewContext.renderGraph,
                in cullingBuffers,
                hzb,
                viewContext.hzbMipCount,
                viewData.ScreenSize.x,
                viewData.ScreenSize.y,
                viewContext.baseWidth,
                viewContext.baseHeight,
                ghostPayload.InstanceCount);

            // Pass 1: Prepare Indirect Dispatch Arguments
            AddPrepareIndirectArgsPass(viewContext.renderGraph, in cullingBuffers, 0);

            Identifier<RGTexture> currentDepth;
            Identifier<RGTexture> currentVisBuffer = default;

            switch (_settings.DebugMode)
            {
                case RenderPipelineDebugMode.Meshlet:
                    // Pass 1: Meshlet Debug Rasterization (Early-Z) directly to backbuffer
                    currentDepth = AddMeshletDebugPass(
                        viewContext.renderGraph,
                        colorTarget,
                        in depthDesc,
                        in cullingBuffers,
                        cullPassIndex: 0);

                    // Build HZB Mip Pyramid from current depth
                    AddBuildHZBPasses(
                        viewContext.renderGraph,
                        currentDepth,
                        hzb,
                        viewContext.hzbMipCount,
                        viewContext.baseWidth,
                        viewContext.baseHeight,
                        viewData.ScreenSize.x,
                        viewData.ScreenSize.y);

                    // Pass 2: Late-Z Meshlet Culling (Work Graph testing occluded meshlets against current HZB)
                    AddMeshletCullPass2(
                        viewContext.renderGraph,
                        in cullingBuffers,
                        hzb,
                        viewContext.hzbMipCount,
                        viewData.ScreenSize.x,
                        viewData.ScreenSize.y,
                        viewContext.baseWidth,
                        viewContext.baseHeight,
                        ghostPayload.InstanceCount);

                    // Pass 2: Prepare Indirect Dispatch Arguments
                    AddPrepareIndirectArgsPass(viewContext.renderGraph, in cullingBuffers, 1);

                    // Pass 2: Meshlet Debug Rasterization (Late-Z) directly to backbuffer
                    AddMeshletDebugPass(
                        viewContext.renderGraph,
                        colorTarget,
                        in depthDesc,
                        in cullingBuffers,
                        cullPassIndex: 1,
                        existingDepth: currentDepth);

                    // Skip BlitPass: colorTarget (backbuffer) has already received rendered meshlet debug output directly
                    break;
                default:
                    // Pass 1: Visibility Buffer Rasterization (Early-Z / Previously Visible)
                    (currentVisBuffer, currentDepth) = AddVisibilityBufferPass(
                        viewContext.renderGraph,
                        in vbufferDesc,
                        in depthDesc,
                        in cullingBuffers,
                        cullPassIndex: 0);

                    // Build HZB Mip Pyramid (Compute passes downsampling current depth directly into hzb)
                    AddBuildHZBPasses(
                        viewContext.renderGraph,
                        currentDepth,
                        hzb,
                        viewContext.hzbMipCount,
                        viewContext.baseWidth,
                        viewContext.baseHeight,
                        viewData.ScreenSize.x,
                        viewData.ScreenSize.y);

                    // Pass 2: Late-Z Meshlet Culling (Work Graph testing occluded meshlets against current HZB)
                    AddMeshletCullPass2(
                        viewContext.renderGraph,
                        in cullingBuffers,
                        hzb,
                        viewContext.hzbMipCount,
                        viewData.ScreenSize.x,
                        viewData.ScreenSize.y,
                        viewContext.baseWidth,
                        viewContext.baseHeight,
                        ghostPayload.InstanceCount);

                    // Pass 2: Prepare Indirect Dispatch Arguments
                    AddPrepareIndirectArgsPass(viewContext.renderGraph, in cullingBuffers, 1);

                    // Pass 2: Visibility Buffer Rasterization (Late-Z / Newly Visible)
                    AddVisibilityBufferPass(
                        viewContext.renderGraph,
                        in vbufferDesc,
                        in depthDesc,
                        in cullingBuffers,
                        cullPassIndex: 1,
                        existingVisBuffer: currentVisBuffer,
                        existingDepth: currentDepth);

                    // Blit Visibility Buffer to screen / backbuffer
                    AddBlitPass(viewContext.renderGraph, currentVisBuffer, colorTarget);
                    break;
            }

            var result = viewContext.renderGraph.CompileAndExecute(executionContext, viewState);
            if (result.IsFailure)
            {
                return Result.Failure($"Render graph execution failed: {result.Error}");
            }
        }

        return Result.Success();
    }

    private Identifier<RGTexture> AddMeshletDebugPass(
        RenderGraph rg,
        Identifier<RGTexture> colorTarget,
        scoped in RGTextureDesc depthDesc,
        in CameraCullingBuffers buffers,
        uint cullPassIndex,
        Identifier<RGTexture> existingDepth = default)
    {
        if (!_cullingResource.meshletDebugShader.IsValid || s_dispatchMeshCommandSignature == null)
        {
            return existingDepth;
        }

        var isPass1 = cullPassIndex == 0;
        var passName = isPass1 ? "MeshletDebug_Pass1_EarlyZ" : "MeshletDebug_Pass2_LateZ";
        using var builder = rg.AddRasterRenderPass<MeshletDebugPassData>(passName);

        var depthTexture = existingDepth.IsValid
            ? existingDepth
            : builder.CreateTexture(in depthDesc, "SceneDepthBuffer");

        builder.SetColorAttachment(colorTarget, 0);
        builder.SetDepthAttachment(depthTexture);

        var visibleBuffer = isPass1 ? buffers.visibleMeshletsPass1 : buffers.visibleMeshletsPass2;
        var visibleMaskedBuffer = isPass1 ? buffers.visibleMaskedMeshletsPass1 : buffers.visibleMaskedMeshletsPass2;
        builder.UseBuffer(visibleBuffer, AccessFlags.Read);
        builder.UseBuffer(visibleMaskedBuffer, AccessFlags.Read);
        builder.UseBuffer(buffers.indirectArgsBuffer, AccessFlags.Read);

        builder.SetPassData(new MeshletDebugPassData
        {
            visibleMeshlets = visibleBuffer,
            visibleMaskedMeshlets = visibleMaskedBuffer,
            indirectArgsBuffer = buffers.indirectArgsBuffer,
            opaqueArgsOffset = isPass1 ? 0UL : 32UL,
            maskedArgsOffset = isPass1 ? 16UL : 48UL,
            debugShader = _cullingResource.meshletDebugShader,
            passIndex = cullPassIndex
        });

        builder.SetRenderFunc<MeshletDebugPassData>(static (ref readonly passData, renderCtx) =>
        {
            var actualIndirectBuf = renderCtx.GetActualBuffer(passData.indirectArgsBuffer);
            var passBit = passData.passIndex << 31;

            if (passData.debugShader.IsValid &&
                renderCtx.TrySetActiveShaderPass(passData.debugShader, PassSemantic.Forward))
            {
                // 1. Draw Opaque Meshlets with debug shader
                var visibleBufferIndex = renderCtx.GetActualBindlessIndex(passData.visibleMeshlets);
                renderCtx.SetInstanceIndex(visibleBufferIndex | passBit);
                renderCtx.ExecuteIndirect(s_dispatchMeshCommandSignature, 1, actualIndirectBuf, passData.opaqueArgsOffset);

                // 2. Draw Masked Meshlets with debug shader
                var visibleMaskedIndex = renderCtx.GetActualBindlessIndex(passData.visibleMaskedMeshlets);
                renderCtx.SetInstanceIndex(visibleMaskedIndex | passBit);
                renderCtx.ExecuteIndirect(s_dispatchMeshCommandSignature, 1, actualIndirectBuf, passData.maskedArgsOffset);
            }
        });

        return depthTexture;
    }

    private (Identifier<RGTexture> visibilityBuffer, Identifier<RGTexture> depthBuffer) AddVisibilityBufferPass(
        RenderGraph rg,
        scoped in RGTextureDesc vbufferDesc,
        scoped in RGTextureDesc depthDesc,
        in CameraCullingBuffers buffers,
        uint cullPassIndex,
        Identifier<RGTexture> existingVisBuffer = default,
        Identifier<RGTexture> existingDepth = default)
    {
        if (!_cullingResource.visibilityShader.IsValid || s_dispatchMeshCommandSignature == null)
        {
            return (existingVisBuffer, existingDepth);
        }

        var isPass1 = cullPassIndex == 0;
        var passName = isPass1 ? "Visibility_Pass1_EarlyZ" : "Visibility_Pass2_LateZ";
        using var builder = rg.AddRasterRenderPass<VisibilityPassData>(passName);

        var visBufferTexture = existingVisBuffer.IsValid
            ? existingVisBuffer
            : builder.CreateTexture(in vbufferDesc, "VisibilityBuffer");

        var depthTexture = existingDepth.IsValid
            ? existingDepth
            : builder.CreateTexture(in depthDesc, "SceneDepthBuffer");

        builder.SetColorAttachment(visBufferTexture, 0);
        builder.SetDepthAttachment(depthTexture);

        var visibleBuffer = isPass1 ? buffers.visibleMeshletsPass1 : buffers.visibleMeshletsPass2;
        var visibleMaskedBuffer = isPass1 ? buffers.visibleMaskedMeshletsPass1 : buffers.visibleMaskedMeshletsPass2;
        builder.UseBuffer(visibleBuffer, AccessFlags.Read);
        builder.UseBuffer(visibleMaskedBuffer, AccessFlags.Read);
        builder.UseBuffer(buffers.indirectArgsBuffer, AccessFlags.Read);

        builder.SetPassData(new VisibilityPassData
        {
            visibleMeshlets = visibleBuffer,
            visibleMaskedMeshlets = visibleMaskedBuffer,
            indirectArgsBuffer = buffers.indirectArgsBuffer,
            opaqueArgsOffset = isPass1 ? 0UL : 32UL,
            maskedArgsOffset = isPass1 ? 16UL : 48UL,
            visibilityShader = _cullingResource.visibilityShader,
            visibilityMaskedShader = _cullingResource.visibilityMaskedShader,
            passIndex = cullPassIndex
        });

        builder.SetRenderFunc<VisibilityPassData>(static (ref readonly passData, renderCtx) =>
        {
            var actualIndirectBuf = renderCtx.GetActualBuffer(passData.indirectArgsBuffer);
            var passBit = passData.passIndex << 31;

            // 1. Draw Opaque Meshlets
            if (passData.visibilityShader.IsValid &&
                renderCtx.TrySetActiveShaderPass(passData.visibilityShader, PassSemantic.Visibility))
            {
                var visibleBufferIndex = renderCtx.GetActualBindlessIndex(passData.visibleMeshlets);
                renderCtx.SetInstanceIndex(visibleBufferIndex | passBit);
                renderCtx.ExecuteIndirect(s_dispatchMeshCommandSignature, 1, actualIndirectBuf, passData.opaqueArgsOffset);
            }

            // 2. Draw Masked (Alpha-Clipped) Meshlets
            if (passData.visibilityMaskedShader.IsValid &&
                renderCtx.TrySetActiveShaderPass(passData.visibilityMaskedShader, PassSemantic.Visibility))
            {
                var visibleMaskedIndex = renderCtx.GetActualBindlessIndex(passData.visibleMaskedMeshlets);
                renderCtx.SetInstanceIndex(visibleMaskedIndex | passBit);
                renderCtx.ExecuteIndirect(s_dispatchMeshCommandSignature, 1, actualIndirectBuf, passData.maskedArgsOffset);
            }
        });

        return (visBufferTexture, depthTexture);
    }

    private void AddBlitPass(
        RenderGraph rg,
        Identifier<RGTexture> srcBuffer,
        Identifier<RGTexture> dstColorTarget)
    {
        if (!_cullingResource.blitShader.IsValid)
        {
            return;
        }

        using var builder = rg.AddRasterRenderPass<BlitPassData>("BlitPass");
        builder.SetColorAttachment(dstColorTarget, 0, AccessFlags.WriteAll);
        builder.UseTexture(srcBuffer, AccessFlags.Read);

        builder.SetPassData(new BlitPassData
        {
            srcBuffer = srcBuffer,
            blitShader = _cullingResource.blitShader
        });

        builder.SetRenderFunc<BlitPassData>(static (ref readonly passData, renderCtx) =>
        {
            if (!renderCtx.TrySetActiveShaderPass(passData.blitShader, PassSemantic.Forward))
            {
                return;
            }

            var property = new HiddenBlitShaderProperties
            {
                mainTex = renderCtx.GetActualBindlessIndex(passData.srcBuffer),
                sampler_mainTex = (uint)renderCtx.ResourceManager.StaticSampler.LinearClamp.Value,
            };

            renderCtx.SetProperties(property);
            renderCtx.DispatchMesh(1, 1, 1);
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        DisposeCulling();
        _gpuSceneResource.Dispose();

        _gpuScene.Dispose();
        _gpuViewManager.Dispose();
    }
}
