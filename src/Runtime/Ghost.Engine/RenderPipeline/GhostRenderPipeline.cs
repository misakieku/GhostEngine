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

    private struct BlitPassData
    {
        public Identifier<RGTexture> srcBuffer;
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
    private readonly GPUViewManager _gpuViewManager;

    private CPUInstanceInfo[] _instanceInfos;
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

        _renderGraph = new RenderGraph(
            renderEngine.GraphicsEngine.ResourceDatabase,
            renderEngine.GraphicsEngine.ResourceAllocator,
            renderEngine.GraphicsEngine.PipelineLibrary,
            renderEngine.ResourceManager,
            renderEngine.ShaderLibrary);
        _gpuScene = new GPUScene(renderEngine.GraphicsEngine.ResourceAllocator, renderEngine.GraphicsEngine.ResourceDatabase, 102_400u); // 102.4k objects should be enough for now
        _gpuViewManager = new GPUViewManager(renderEngine.GraphicsEngine.ResourceDatabase);
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

    public RGExecution ExecuteGraph(RenderContext ctx, int frameIndex, IRenderPayload payload, in RenderGraphExecutionContext executionContext)
    {
        var ghostPayload = (GhostRenderPayload)payload;
        _renderGraph.Reset();
        if (_lastRenderRequestCount != ghostPayload.RenderRequests.Length || _lastInstanceCount != _gpuScene.InstanceCount)
        {
            _lastRenderRequestCount = ghostPayload.RenderRequests.Length;
            _lastInstanceCount = _gpuScene.InstanceCount;
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

            var viewContext = _gpuViewManager.GetView(request.viewId);
            viewContext.EnsureResources(
                ctx.ResourceAllocator,
                ctx.ResourceDatabase,
                viewData.ScreenSize.x,
                viewData.ScreenSize.y);

            var isStatic = AreEqualBits(in viewProjMatrix, in viewContext.prevViewProjMatrix);
            viewContext.prevViewProjMatrix = viewProjMatrix;

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
                clearAtFirstUse: true);

            var depthDesc = RGTextureDesc.Absolute(
                viewData.ScreenSize.x,
                viewData.ScreenSize.y,
                TextureFormat.D32_Float,
                usage: TextureUsage.DepthStencil | TextureUsage.ShaderResource,
                clearAtFirstUse: true);

            var hzbAtlas = _renderGraph.ImportTexture(viewContext.hzbAtlas);

            // Initialize transient culling & indirect argument buffers for this camera view
            AddInitializeCullingBuffersPass(_renderGraph, out var cullingBuffers);

            // Pass 1: Early-Z Hierarchical Meshlet Culling (Work Graph)
            AddMeshletCullPass1(
                _renderGraph,
                in cullingBuffers,
                hzbAtlas,
                viewContext.hzbMipCount,
                isStatic,
                viewData.ScreenSize.x,
                viewData.ScreenSize.y,
                in viewContext.hzbOffsets0,
                in viewContext.hzbOffsets1,
                in viewContext.hzbOffsets2,
                in viewContext.hzbOffsets3,
                _gpuScene.InstanceCount);

            // Pass 1: Prepare Indirect Dispatch Arguments
            AddPrepareIndirectArgsPass(_renderGraph, in cullingBuffers, 0);

            // Pass 1: Visibility Buffer Rasterization (Early-Z / Previously Visible)
            var (currentVisBuffer, currentDepth) = AddVisibilityBufferPass(
                _renderGraph,
                in vbufferDesc,
                in depthDesc,
                in cullingBuffers,
                cullPassIndex: 0);

            // Build HZB Mip Pyramid (Compute passes downsampling current depth directly into hzbAtlas)
            AddBuildHZBPasses(
                _renderGraph,
                currentDepth,
                hzbAtlas,
                viewContext.hzbMipCount,
                viewData.ScreenSize.x,
                viewData.ScreenSize.y);

            // Pass 2: Late-Z Meshlet Culling (Work Graph testing occluded meshlets against current HZB)
            AddMeshletCullPass2(
                _renderGraph,
                in cullingBuffers,
                hzbAtlas,
                viewContext.hzbMipCount,
                viewData.ScreenSize.x,
                viewData.ScreenSize.y,
                in viewContext.hzbOffsets0,
                in viewContext.hzbOffsets1,
                in viewContext.hzbOffsets2,
                in viewContext.hzbOffsets3,
                _gpuScene.InstanceCount);

            // Pass 2: Prepare Indirect Dispatch Arguments
            AddPrepareIndirectArgsPass(_renderGraph, in cullingBuffers, 1);

            // Pass 2: Visibility Buffer Rasterization (Late-Z / Newly Visible)
            AddVisibilityBufferPass(
                _renderGraph,
                in vbufferDesc,
                in depthDesc,
                in cullingBuffers,
                cullPassIndex: 1,
                existingVisBuffer: currentVisBuffer,
                existingDepth: currentDepth);

            // Blit Visibility Buffer to screen / backbuffer
            AddBlitPass(_renderGraph, currentVisBuffer, colorTarget);
        }

        var result = _renderGraph.CompileAndExecute(executionContext, primaryViewState);
        if (result.IsFailure)
        {
            Logger.Error($"Render graph execution failed: {result.Error}");
            return default;
        }

        return result.Value;
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

        _renderGraph.Dispose();
        _gpuScene.Dispose();
        _gpuViewManager.Dispose();
    }

    private static bool AreEqualBits(in float4x4 a, in float4x4 b)
    {
        var sa = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in a), 1));
        var sb = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in b), 1));
        return sa.SequenceEqual(sb);
    }
}
