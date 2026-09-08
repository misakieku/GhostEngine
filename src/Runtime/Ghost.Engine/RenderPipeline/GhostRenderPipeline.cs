using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Engine.Streaming;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline : IRenderPipeline
{
    private struct TestPassData
    {
        public Identifier<RGTexture> depth;
        public uint frameBufferIndex;
        public uint viewBufferIndex;
        public uint instanceCount;
        public uint width;
        public uint height;
    }

    private struct CPUInstanceInfo
    {
        public Handle<Mesh> mesh;
        public Handle<Material> material;
    }

    private readonly RenderEngine _renderEngine;

    private readonly RenderGraph _renderGraph;
    private readonly GPUScene _gpuScene;
    private readonly PassRenderFunc<TestPassData, IRasterRenderContext> _renderPassFunc;

    private CPUInstanceInfo[] _instanceInfos;
    private AssetManager? _assetManager;
    private bool _disposed;
    private int _lastRenderRequestCount = -1;
    private uint _lastInstanceCount = uint.MaxValue;
    private int _lastMaterialBindProbe = -1;

    public GPUScene GPUScene => _gpuScene;

    public GhostRenderPipeline(RenderEngine renderEngine)
    {
        _renderEngine = renderEngine;

        _renderGraph = new RenderGraph(
            renderEngine.GraphicsEngine.ResourceDatabase,
            renderEngine.GraphicsEngine.ResourceAllocator,
            renderEngine.GraphicsEngine.PipelineLibrary,
            renderEngine.ResourceManager,
            renderEngine.ShaderLibrary);
        _gpuScene = new GPUScene(renderEngine.GraphicsEngine.ResourceAllocator, renderEngine.GraphicsEngine.ResourceDatabase, 102_400u); // 102.4k objects should be enough for now
        _instanceInfos = new CPUInstanceInfo[64];
        _renderPassFunc = RenderPassCallback;
    }

    public void RecordPrelude(RenderContext ctx, int frameIndex, IRenderPayload payload)
    {
        var ghostPayload = (GhostRenderPayload)payload;

        // Upload dirty material palette tables to the GPU before any rendering or shading
        ctx.ResourceManager.UploadMaterialPaletteData(ctx);

        // Update GPU scene instance buffer once per frame prelude
        UpdateGPUScene(ctx, ghostPayload);
    }

    public unsafe RGExecution ExecuteGraph(RenderContext ctx, int frameIndex, IRenderPayload payload,
        in RenderGraphExecutionContext executionContext)
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

        ref readonly var request = ref ghostPayload.RenderRequests[0];
        using var viewData = new RenderViewData(_renderEngine.SwapChainManager, ctx.ResourceDatabase, in request);
        var viewState = new ViewState(viewData.ScreenSize.x, viewData.ScreenSize.y, viewData.ScreenSize.x, viewData.ScreenSize.y);

        // Upload FrameData
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

        // Upload ViewData
        RenderPipelineUtility.GetVPMatrices(in request, viewData.ScreenSize, out var viewMatrix, out var projMatrix);
        var viewDataGpu = new ViewData
        {
            viewMatrix = viewMatrix,
            projectionMatrix = projMatrix,
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

        BuildRepresentativePipeline(
            _renderGraph,
            viewData.ColorTexture,
            (uint)frameIndex,
            viewData.ScreenSize.x,
            viewData.ScreenSize.y,
            frameBufferIndex,
            viewBufferIndex,
            _gpuScene.InstanceCount);

        var result = _renderGraph.CompileAndExecute(executionContext, viewState);
        if (result.IsFailure)
        {
            Logger.Error($"Render graph execution failed: {result.Error}");
            return default;
        }

        return result.Value;
    }

    private void BuildRepresentativePipeline(
        RenderGraph rg,
        Handle<GPUTexture> backBufferHandle,
        uint frameIndex,
        uint width,
        uint height,
        uint frameBufferIndex,
        uint viewBufferIndex,
        uint instanceCount)
    {
        var backBuffer = rg.ImportTexture(
            backBufferHandle,
            initialState: new ResourceBarrierData(BarrierLayout.Present, BarrierAccess.NoAccess, BarrierSync.None),
            finalState: new ResourceBarrierData(BarrierLayout.Present, BarrierAccess.NoAccess, BarrierSync.None),
            clearColor: new Color128(0.1f, 0.1f, 0.15f, 1.0f),
            clearAtFirstUse: true);

        using (var builder = rg.AddRasterRenderPass<TestPassData>("MeshletTestPass"))
        {
            var depth = builder.CreateTexture(RGTextureDesc.RelativeDepth(1.0f));

            builder.SetColorAttachment(backBuffer, 0, AccessFlags.WriteAll);
            builder.SetDepthAttachment(depth, AccessFlags.WriteAll);

            builder.SetPassData(new TestPassData
            {
                depth = depth,
                frameBufferIndex = frameBufferIndex,
                viewBufferIndex = viewBufferIndex,
                instanceCount = instanceCount,
                width = width,
                height = height
            });

            builder.SetRenderFunc(_renderPassFunc);
        }
    }

    private void RenderPassCallback(ref readonly TestPassData passData, IRasterRenderContext renderCtx)
    {
        for (uint inst = 0; inst < passData.instanceCount; inst++)
        {
            if (inst >= _instanceInfos.Length)
            {
                break;
            }

            var info = _instanceInfos[inst];
            if (info.mesh.IsInvalid)
            {
                continue;
            }

            var meshResult = renderCtx.ResourceManager.GetMeshReference(info.mesh);
            if (meshResult.IsFailure)
            {
                continue;
            }

            ref readonly var mesh = ref meshResult.Value;
            var meshletCount = mesh.MeshletCount > 0 ? (uint)mesh.MeshletCount : 1u;

            var instancePsoSet = renderCtx.TrySetActiveMaterialPass(info.material, PassSemantic.Forward);
            var bindState = instancePsoSet ? 1 : 0;
            if (_lastMaterialBindProbe != bindState)
            {
                _lastMaterialBindProbe = bindState;
                Logger.Info($"Render probe: materialBound={instancePsoSet}, meshlets={meshletCount}, materialValid={info.material.IsValid}.");
            }

            if (!instancePsoSet)
            {
                continue;
            }

            renderCtx.SetActiveMesh(in mesh);
            renderCtx.SetGlobalData(passData.frameBufferIndex, passData.viewBufferIndex);
            renderCtx.SetInstanceIndex(inst);
            renderCtx.DispatchMesh(meshletCount, 1, 1);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _renderGraph.Dispose();
        _gpuScene.Dispose();
    }
}
