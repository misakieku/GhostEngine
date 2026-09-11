using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.Streaming;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline : IRenderPipeline
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
    }

    private struct BlitPassData
    {
        public Identifier<RGTexture> visBuffer;
        public Handle<Shader> blitShader;
    }

    private struct CPUInstanceInfo
    {
        public Handle<Mesh> mesh;
        public Handle<Material> material;
    }

    private readonly RenderEngine _renderEngine;
    private readonly AssetManager _assetManager;
    private readonly GhostRenderPipelineSettings _settings;
    private readonly GPUSceneResource _gpuSceneResource;

    private readonly RenderGraph _renderGraph;
    private readonly GPUScene _gpuScene;

    private CPUInstanceInfo[] _instanceInfos;
    private bool _disposed;
    private int _lastRenderRequestCount = -1;
    private uint _lastInstanceCount = uint.MaxValue;

    public GPUScene GPUScene => _gpuScene;
    public GhostRenderPipelineSettings Settings => _settings;

    public GhostRenderPipeline(RenderEngine renderEngine, AssetManager assetManager, GhostRenderPipelineSettings? settings = null)
    {
        _renderEngine = renderEngine;
        _assetManager = assetManager;
        _settings = settings ?? new GhostRenderPipelineSettings();

        _gpuSceneResource = new GPUSceneResource(assetManager);
        _gpuSceneResource.Resolve();

        _renderGraph = new RenderGraph(
            renderEngine.GraphicsEngine.ResourceDatabase,
            renderEngine.GraphicsEngine.ResourceAllocator,
            renderEngine.GraphicsEngine.PipelineLibrary,
            renderEngine.ResourceManager,
            renderEngine.ShaderLibrary);
        _gpuScene = new GPUScene(renderEngine.GraphicsEngine.ResourceAllocator, renderEngine.GraphicsEngine.ResourceDatabase, 102_400u); // 102.4k objects should be enough for now
        _instanceInfos = new CPUInstanceInfo[64];

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

    public unsafe RGExecution ExecuteGraph(RenderContext ctx, int frameIndex, IRenderPayload payload, in RenderGraphExecutionContext executionContext)
    {
        var ghostPayload = (GhostRenderPayload)payload;
        _renderGraph.Reset();
        if (_lastRenderRequestCount != ghostPayload.RenderRequests.Length || _lastInstanceCount != _gpuScene.InstanceCount)
        {
            _lastRenderRequestCount = ghostPayload.RenderRequests.Length;
            _lastInstanceCount = _gpuScene.InstanceCount;
            Logger.Info($"Render probe: requests={_lastRenderRequestCount}, gpuInstances={_lastInstanceCount}.");
        }

        if (ghostPayload.RenderRequests.Length == 0)
        {
            return default;
        }

        // Upload FrameData once per frame
        var frameData = new FrameData
        {
            instanceBuffer = ctx.ResourceDatabase.GetBindlessIndex(_gpuScene.SceneBuffer.AsResource()),
            userBuffer = 0,
            paletteOffsetBuffer = ctx.ResourceManager.PaletteOffsetBufferBindlessIndex,
            materialIndexBuffer = ctx.ResourceManager.MaterialIndexBufferBindlessIndex,
        };

        var frameDesc = new BufferDesc
        {
            Size = (uint)sizeof(FrameData),
            Stride = (uint)sizeof(FrameData),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Upload,
        };
        var frameGpuBuffer = ctx.ResourceManager.CreateTransientBuffer(in frameDesc, "FrameDataBuffer");
        var pFrameData = (FrameData*)ctx.ResourceDatabase.MapResource(frameGpuBuffer.AsResource(), 0, null);
        *pFrameData = frameData;
        ctx.ResourceDatabase.UnmapResource(frameGpuBuffer.AsResource(), 0, null);
        var frameBufferIndex = ctx.ResourceDatabase.GetBindlessIndex(frameGpuBuffer.AsResource());

        _renderGraph.SetFrameData(frameBufferIndex);

        var primaryViewState = default(ViewState);

        // NOTE: Architectural Rule: Do not hide multiple passes inside an opaque aggregate method (e.g. DoCullingPass).
        // Keep individual pass registration visible at the pipeline orchestration level in ExecuteGraph so that developers
        // and tooling can immediately see all scheduled passes and their dependency order.
        for (var requestIndex = 0; requestIndex < ghostPayload.RenderRequests.Length; requestIndex++)
        {
            ref readonly var request = ref ghostPayload.RenderRequests[requestIndex];
            using var viewData = new RenderViewData(_renderEngine.SwapChainManager, ctx.ResourceDatabase, in request);
            var viewState = new ViewState(viewData.ScreenSize.x, viewData.ScreenSize.y, viewData.ScreenSize.x, viewData.ScreenSize.y);
            if (requestIndex == 0)
            {
                primaryViewState = viewState;
            }

            // Upload ViewData (Reversed-Z: near=1.0, far=0.0)
            RenderPipelineUtility.GetVPMatricesReversedZ(in request, viewData.ScreenSize, out var viewMatrix, out var projMatrix);
            var viewProjMatrix = math.mul(projMatrix, viewMatrix);
            var viewDataGpu = new ViewData
            {
                viewMatrix = viewMatrix,
                projectionMatrix = projMatrix,
                viewProjectionMatrix = viewProjMatrix,
                cameraPosition = request.view.localToWorld.c3.xyz,
                nearClip = request.view.nearClipPlane,
                cameraDirection = request.view.localToWorld.c2.xyz,
                farClip = request.view.farClipPlane,
                screenSize = new float4(viewData.ScreenSize.x, viewData.ScreenSize.y, 1.0f / viewData.ScreenSize.x, 1.0f / viewData.ScreenSize.y),
            };

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
            _renderGraph.SetViewData(viewBufferIndex);

            var presentBarrier = new ResourceBarrierData(
                BarrierLayout.Present,
                BarrierAccess.NoAccess,
                BarrierSync.None);

            var colorTarget = _renderGraph.ImportTexture(
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
                clearAtFirstUse: true) with { clearColor = default };

            var depthDesc = RGTextureDesc.Absolute(
                viewData.ScreenSize.x,
                viewData.ScreenSize.y,
                TextureFormat.D32_Float,
                usage: TextureUsage.DepthStencil | TextureUsage.ShaderResource,
                clearAtFirstUse: true) with { clearDepth = 0.0f };

            // 1. Initialize transient culling & indirect argument buffers for this camera view
            AddInitializeCullingBuffersPass(_renderGraph, out var cullingBuffers);

            // 2. Pass 1: Early-Z Hierarchical Meshlet Culling (Work Graph)
            AddMeshletCullPass1(
                _renderGraph,
                in cullingBuffers,
                in request,
                _gpuScene.InstanceCount);

            // 3. Pass 1: Prepare Indirect Dispatch Arguments
            AddPrepareIndirectArgsPass(
                _renderGraph,
                in cullingBuffers,
                cullPassIndex: 0);

            // 4. Pass 1: Visibility Buffer Rasterization (Early-Z / Previously Visible)
            var (currentVisBuffer, currentDepth) = AddVisibilityBufferPass(
                _renderGraph,
                in vbufferDesc,
                in depthDesc,
                in cullingBuffers,
                cullPassIndex: 0);

            // 5. Build HZB Mip Pyramid (Compute passes downsampling current depth) & Queue Extractions to camera history
            AddBuildHZBPasses(
                _renderGraph,
                currentDepth,
                in request,
                viewData.ScreenSize.x,
                viewData.ScreenSize.y,
                out var hzbMips,
                out var hzbMipCount);

            // 6. Pass 2: Late-Z Meshlet Culling (Work Graph testing occluded meshlets against current HZB)
            AddMeshletCullPass2(
                _renderGraph,
                in cullingBuffers,
                hzbMips,
                hzbMipCount,
                viewData.ScreenSize.x,
                viewData.ScreenSize.y,
                _gpuScene.InstanceCount);

            // 7. Pass 2: Prepare Indirect Dispatch Arguments
            AddPrepareIndirectArgsPass(
                _renderGraph,
                in cullingBuffers,
                cullPassIndex: 1);

            // 8. Pass 2: Visibility Buffer Rasterization (Late-Z / Newly Visible)
            AddVisibilityBufferPass(
                _renderGraph,
                in vbufferDesc,
                in depthDesc,
                in cullingBuffers,
                cullPassIndex: 1,
                existingVisBuffer: currentVisBuffer,
                existingDepth: currentDepth);

            // 9. Blit Visibility Buffer to screen / backbuffer
            AddBlitPass(
                _renderGraph,
                currentVisBuffer,
                colorTarget);
        }

        var result = _renderGraph.CompileAndExecute(executionContext, primaryViewState);
        if (result.IsFailure)
        {
            Logger.Error($"Render graph execution failed: {result.Error}");
            return default;
        }

        return result.Value;
    }

    private unsafe (Identifier<RGTexture> visibilityBuffer, Identifier<RGTexture> depthBuffer) AddVisibilityBufferPass(
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
            visibilityMaskedShader = _cullingResource.visibilityMaskedShader
        });

        builder.SetRenderFunc<VisibilityPassData>(static (ref readonly passData, renderCtx) =>
        {
            var actualIndirectBuf = renderCtx.GetActualBuffer(passData.indirectArgsBuffer);

            // 1. Draw Opaque Meshlets
            if (passData.visibilityShader.IsValid &&
                renderCtx.TrySetActiveShaderPass(passData.visibilityShader, PassSemantic.Visibility))
            {
                var visibleBufferIndex = renderCtx.GetActualBindlessIndex(passData.visibleMeshlets);
                renderCtx.SetInstanceIndex(visibleBufferIndex);
                renderCtx.SetGlobalData();
                renderCtx.ExecuteIndirect(s_dispatchMeshCommandSignature, 1, actualIndirectBuf, passData.opaqueArgsOffset);
            }

            // 2. Draw Masked (Alpha-Clipped) Meshlets
            if (passData.visibilityMaskedShader.IsValid &&
                renderCtx.TrySetActiveShaderPass(passData.visibilityMaskedShader, PassSemantic.Visibility))
            {
                var visibleMaskedIndex = renderCtx.GetActualBindlessIndex(passData.visibleMaskedMeshlets);
                renderCtx.SetInstanceIndex(visibleMaskedIndex);
                renderCtx.SetGlobalData();
                renderCtx.ExecuteIndirect(s_dispatchMeshCommandSignature, 1, actualIndirectBuf, passData.maskedArgsOffset);
            }
        });

        return (visBufferTexture, depthTexture);
    }

    private unsafe void AddBlitPass(
        RenderGraph rg,
        Identifier<RGTexture> srcVisibilityBuffer,
        Identifier<RGTexture> dstColorTarget)
    {
        if (!_cullingResource.blitShader.IsValid)
        {
            return;
        }

        using var builder = rg.AddRasterRenderPass<BlitPassData>("BlitVisibilityBuffer");
        builder.SetColorAttachment(dstColorTarget, 0, AccessFlags.WriteAll);
        builder.UseTexture(srcVisibilityBuffer, AccessFlags.Read);

        builder.SetPassData(new BlitPassData
        {
            visBuffer = srcVisibilityBuffer,
            blitShader = _cullingResource.blitShader
        });

        builder.SetRenderFunc<BlitPassData>(static (ref readonly passData, renderCtx) =>
        {
            if (!renderCtx.TrySetActiveShaderPass(passData.blitShader, PassSemantic.Forward))
            {
                return;
            }

            var visBufferIndex = renderCtx.GetActualBindlessIndex(passData.visBuffer);

            renderCtx.SetInstanceIndex(visBufferIndex);
            renderCtx.SetGlobalData();
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

        _renderGraph.Dispose();
        _gpuScene.Dispose();
    }
}
