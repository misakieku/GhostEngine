using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline : IRenderPipeline
{
    private const string PRODUCER_RASTER_HLSL = @"
struct PSInput
{
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
};

[numthreads(4, 1, 1)]
[outputtopology(""triangle"")]
void MSMain(
    uint gtid : SV_GroupThreadID,
    out vertices PSInput verts[4],
    out indices uint3 tris[2]
)
{
    SetMeshOutputCounts(4, 2);

    float2 uv = float2(gtid & 1, (gtid >> 1) & 1);
    verts[gtid].position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    verts[gtid].uv = uv;

    if (gtid == 0)
    {
        tris[0] = uint3(0, 1, 2);
        tris[1] = uint3(1, 3, 2);
    }
}

float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.uv;
    float3 col = float3(0.1f + uv.x * 0.4f, 0.2f + uv.y * 0.3f, 0.5f);
    return float4(col, 1.0f);
}
";

    private const string SHADERTOY_COMPUTE_HLSL = @"
struct PushConstants
{
    uint outputTextureUav;
    uint inputTextureSrv;
    uint frameNumber;
};
ConstantBuffer<PushConstants> g_Constants : register(b0, space0);

[numthreads(8, 8, 1)]
void CSMain(uint3 dispatchThreadID : SV_DispatchThreadID)
{
    RWTexture2D<float4> outTex = ResourceDescriptorHeap[g_Constants.outputTextureUav];
    Texture2D<float4> inTex = ResourceDescriptorHeap[g_Constants.inputTextureSrv];

    uint width, height;
    outTex.GetDimensions(width, height);
    if (dispatchThreadID.x >= width || dispatchThreadID.y >= height)
        return;

    float2 uv = (float2(dispatchThreadID.xy) + 0.5f) / float2(width, height);
    float4 bg = inTex.Load(int3(dispatchThreadID.xy, 0));

    float time = g_Constants.frameNumber * 0.025f;
    float2 c = (uv - 0.5f) * 2.6f;
    float2 z = c;
    float iter = 0.0f;
    const float maxIter = 48.0f;

    for (float i = 0.0f; i < maxIter; i += 1.0f)
    {
        if (dot(z, z) > 4.0f) { iter = i; break; }
        z = float2(z.x * z.x - z.y * z.y, 2.0f * z.x * z.y) + c + float2(sin(time) * 0.12f, cos(time) * 0.12f);
    }

    float t = iter / maxIter;
    float3 col = 0.5f + 0.5f * cos(3.0f + t * 6.28318f + float3(0.0f, 0.6f, 1.0f));
    if (iter == 0.0f && dot(z, z) <= 4.0f) col = bg.rgb * 0.3f;
    else col = lerp(col, bg.rgb, 0.25f);

    outTex[dispatchThreadID.xy] = float4(col, 1.0f);
}
";

    private const string DEPTH_RASTER_HLSL = @"
struct DepthPSInput
{
    float4 position : SV_POSITION;
};

[numthreads(4, 1, 1)]
[outputtopology(""triangle"")]
void DepthMSMain(
    uint gtid : SV_GroupThreadID,
    out vertices DepthPSInput verts[4],
    out indices uint3 tris[2]
)
{
    SetMeshOutputCounts(4, 2);

    float2 uv = float2(gtid & 1, (gtid >> 1) & 1);
    verts[gtid].position = float4(uv * 1.6 - 0.8, 0.5, 1.0);

    if (gtid == 0)
    {
        tris[0] = uint3(0, 1, 2);
        tris[1] = uint3(1, 3, 2);
    }
}

void DepthPSMain(DepthPSInput input)
{
}
";

    private const string COMPOSITE_RASTER_HLSL = @"
struct PushConstants
{
    uint inputTextureSrv;
    uint depthTextureSrv;
    uint frameNumber;
};
ConstantBuffer<PushConstants> g_Constants : register(b0, space0);

struct PSInput
{
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
};

[numthreads(4, 1, 1)]
[outputtopology(""triangle"")]
void CompositeMSMain(
    uint gtid : SV_GroupThreadID,
    out vertices PSInput verts[4],
    out indices uint3 tris[2]
)
{
    SetMeshOutputCounts(4, 2);

    float2 uv = float2(gtid & 1, (gtid >> 1) & 1);
    verts[gtid].position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    verts[gtid].uv = uv;

    if (gtid == 0)
    {
        tris[0] = uint3(0, 1, 2);
        tris[1] = uint3(1, 3, 2);
    }
}

float4 CompositePSMain(PSInput input) : SV_TARGET
{
    Texture2D<float4> computeTex = ResourceDescriptorHeap[g_Constants.inputTextureSrv];
    uint2 pixelCoord = uint2(input.position.xy);
    return computeTex.Load(int3(pixelCoord, 0));
}
";

    private struct ProducerPassData
    {
        public Identifier<RGTexture> targetA;
        public Key128<PipelineState> pso;
        public uint width;
        public uint height;
    }

    private struct AsyncComputePassData
    {
        public Identifier<RGTexture> targetA;
        public Identifier<RGTexture> targetB;
        public Key128<PipelineState> pso;
        public uint frameIndex;
        public uint width;
        public uint height;
    }

    private struct IndependentGraphicsPassData
    {
        public Identifier<RGTexture> targetC;
        public Key128<PipelineState> pso;
        public uint width;
        public uint height;
    }

    private struct GraphicsJoinPassData
    {
        public Identifier<RGTexture> targetA;
        public Identifier<RGTexture> targetB;
        public Identifier<RGTexture> targetC;
        public Identifier<RGTexture> backBuffer;
        public Key128<PipelineState> pso;
        public uint frameIndex;
        public uint width;
        public uint height;
    }

    private readonly RenderEngine _renderEngine;

    private readonly RenderGraph _renderGraph;
    private readonly GPUScene _gpuScene;
    private readonly Key128<PipelineState> _producerPso;
    private readonly Key128<PipelineState> _fractalComputePso;
    private readonly Key128<PipelineState> _depthPso;
    private readonly Key128<PipelineState> _compositePso;

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

        var producerMs = RuntimeShaderCompiler.CompileShader(PRODUCER_RASTER_HLSL, "MSMain", "ms_6_6");
        var producerPs = RuntimeShaderCompiler.CompileShader(PRODUCER_RASTER_HLSL, "PSMain", "ps_6_6");
        var producerPsoDesc = new GraphicsPSODesc
        {
            CompiledHash = 0x50726F6455434552UL,
            MsCode = producerMs,
            PsCode = producerPs,
            RtvFormats = [TextureFormat.R8G8B8A8_UNorm],
            DsvFormat = TextureFormat.Unknown,
            PipelineOption = new PipelineState { ZTest = ZTest.Disabled, ZWrite = ZWrite.Off, Cull = Cull.Off, Blend = Blend.Opaque, ColorMask = ColorWriteMask.All }
        };
        _producerPso = renderEngine.GraphicsEngine.PipelineLibrary.CreateGraphicsPipeline(in producerPsoDesc).GetValueOrThrow();

        var computeCs = RuntimeShaderCompiler.CompileComputeShader(SHADERTOY_COMPUTE_HLSL, "CSMain");
        var computePsoDesc = new ComputePSODesc
        {
            CompiledHash = 0x546F79435350534FUL, // "ToyCSPSO"
            CsCode = computeCs,
        };
        _fractalComputePso = renderEngine.GraphicsEngine.PipelineLibrary.CreateComputePipeline(in computePsoDesc).GetValueOrThrow();

        var depthMs = RuntimeShaderCompiler.CompileShader(DEPTH_RASTER_HLSL, "DepthMSMain", "ms_6_6");
        var depthPs = RuntimeShaderCompiler.CompileShader(DEPTH_RASTER_HLSL, "DepthPSMain", "ps_6_6");
        var depthPsoDesc = new GraphicsPSODesc
        {
            CompiledHash = 0x446570746850534FUL,
            MsCode = depthMs,
            PsCode = depthPs,
            RtvFormats = [],
            DsvFormat = TextureFormat.D32_Float,
            PipelineOption = new PipelineState { ZTest = ZTest.LessEqual, ZWrite = ZWrite.On, Cull = Cull.Off, Blend = Blend.Opaque, ColorMask = ColorWriteMask.None }
        };
        _depthPso = renderEngine.GraphicsEngine.PipelineLibrary.CreateGraphicsPipeline(in depthPsoDesc).GetValueOrThrow();

        var compositeMs = RuntimeShaderCompiler.CompileShader(COMPOSITE_RASTER_HLSL, "CompositeMSMain", "ms_6_6");
        var compositePs = RuntimeShaderCompiler.CompileShader(COMPOSITE_RASTER_HLSL, "CompositePSMain", "ps_6_6");
        var compositePsoDesc = new GraphicsPSODesc
        {
            CompiledHash = 0x436F6D706F534954UL,
            MsCode = compositeMs,
            PsCode = compositePs,
            RtvFormats = [TextureFormat.B8G8R8A8_UNorm],
            DsvFormat = TextureFormat.Unknown,
            PipelineOption = new PipelineState { ZTest = ZTest.Disabled, ZWrite = ZWrite.Off, Cull = Cull.Off, Blend = Blend.Opaque, ColorMask = ColorWriteMask.All }
        };
        _compositePso = renderEngine.GraphicsEngine.PipelineLibrary.CreateGraphicsPipeline(in compositePsoDesc).GetValueOrThrow();
    }

    public void RecordPrelude(RenderContext ctx, int frameIndex, IRenderPayload payload)
    {
        var ghostPayload = (GhostRenderPayload)payload;

        foreach (ref readonly var request in ghostPayload.RenderRequests)
        {
            try
            {
                using var viewData = new RenderViewData(_renderEngine.SwapChainManager, ctx.ResourceDatabase, in request);
                RenderPipelineUtility.GetVPMatrices(in request, viewData.ScreenSize, out var view, out var projection);

                UpdateGPUScene(ctx, ghostPayload);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }

    public RGExecution ExecuteGraph(RenderContext ctx, int frameIndex, IRenderPayload payload,
        in RenderGraphExecutionContext executionContext)
    {
        var ghostPayload = (GhostRenderPayload)payload;
        _renderGraph.Reset();

        if (ghostPayload.RenderRequests.Length == 0)
        {
            return default;
        }

        ref readonly var request = ref ghostPayload.RenderRequests[0];
        using var viewData = new RenderViewData(_renderEngine.SwapChainManager, ctx.ResourceDatabase, in request);
        var viewState = new ViewState(viewData.ScreenSize.x, viewData.ScreenSize.y, viewData.ScreenSize.x, viewData.ScreenSize.y);

        BuildRepresentativePipeline(
            _renderGraph,
            viewData.ColorTexture,
            _producerPso,
            _fractalComputePso,
            _depthPso,
            _compositePso,
            (uint)frameIndex,
            viewData.ScreenSize.x,
            viewData.ScreenSize.y);

        var result = _renderGraph.CompileAndExecute(executionContext, viewState);
        if (result.IsFailure)
        {
            Logger.Error($"Render graph execution failed: {result.Error}");
            return default;
        }

        return result.Value;
    }

    private static void BuildRepresentativePipeline(
        RenderGraph rg,
        Handle<GPUTexture> backBufferHandle,
        Key128<PipelineState> producerPso,
        Key128<PipelineState> computePso,
        Key128<PipelineState> depthPso,
        Key128<PipelineState> compositePso,
        uint frameIndex,
        uint width,
        uint height)
    {
        var backBuffer = rg.ImportTexture(
            backBufferHandle,
            initialState: new ResourceBarrierData(BarrierLayout.Present, BarrierAccess.NoAccess, BarrierSync.None),
            finalState: new ResourceBarrierData(BarrierLayout.Present, BarrierAccess.NoAccess, BarrierSync.None));

        // 1. Graphics Producer (Raster)
        Identifier<RGTexture> targetA;
        using (var builder = rg.AddRasterRenderPass<ProducerPassData>("Graphics_Producer"))
        {
            targetA = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm, clearAtFirstUse: true, clearColor: new Color128(0.1f, 0.2f, 0.5f, 1.0f)), "Transient_TargetA");
            builder.SetColorAttachment(targetA, 0, AccessFlags.WriteAll);
            builder.SetPassData(new ProducerPassData { targetA = targetA, pso = producerPso, width = width, height = height });
            builder.SetRenderFunc<ProducerPassData>(static (ref readonly data, ctx) =>
            {
                var cmd = ((IUnsafeRenderContext)ctx).GetCommandBufferUnsafe();
                cmd.SetPipelineState(data.pso);
                cmd.SetViewport(new ViewportDesc { X = 0, Y = 0, Width = data.width, Height = data.height, MinDepth = 0.0f, MaxDepth = 1.0f });
                cmd.SetScissorRect(new ScissorRectDesc { Left = 0, Top = 0, Right = data.width, Bottom = data.height });
                cmd.DispatchMesh(1, 1, 1);
            });
        }

        // 2. Async Compute Pass (Compute)
        Identifier<RGTexture> targetB;
        using (var builder = rg.AddComputeRenderPass<AsyncComputePassData>("Async_Compute_Work"))
        {
            builder.EnableAsyncCompute(true);
            targetB = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm, usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource), "Transient_TargetB");
            builder.UseTexture(targetA, AccessFlags.Read);
            builder.UseTexture(targetB, AccessFlags.Write);
            builder.SetPassData(new AsyncComputePassData { targetA = targetA, targetB = targetB, pso = computePso, frameIndex = frameIndex, width = width, height = height });
            builder.SetRenderFunc<AsyncComputePassData>(static (ref readonly data, ctx) =>
            {
                var cmd = ((IUnsafeRenderContext)ctx).GetCommandBufferUnsafe();
                var targetAHandle = ctx.GetActualTexture(data.targetA);
                var targetBHandle = ctx.GetActualTexture(data.targetB);
                var srvIndex = ctx.ResourceDatabase.GetBindlessIndex(targetAHandle.AsResource(), BindlessAccess.ShaderResource);
                var uavIndex = ctx.ResourceDatabase.GetBindlessIndex(targetBHandle.AsResource(), BindlessAccess.UnorderedAccess);

                var constants = new PushConstantsData
                {
                    frameBuffer = uavIndex,
                    viewBuffer = srvIndex,
                    instanceIndex = data.frameIndex,
                };

                cmd.SetPipelineState(data.pso);
                cmd.SetComputeRoot32Constants(RootSignatureLayout.PUSH_CONSTANT_SLOT, constants.AsUInts());
                cmd.DispatchCompute((data.width + 7) / 8, (data.height + 7) / 8, 1);
            });
        }

        // 3. Independent Graphics Pass (Raster, Overlap Window)
        Identifier<RGTexture> targetC;
        using (var builder = rg.AddRasterRenderPass<IndependentGraphicsPassData>("Independent_Graphics_Work"))
        {
            targetC = builder.CreateTexture(RGTextureDesc.RelativeDepth(1.0f, clearAtFirstUse: true, clearDepth: 1.0f, usage: TextureUsage.DepthStencil | TextureUsage.ShaderResource), "Transient_TargetC");
            builder.SetDepthAttachment(targetC, AccessFlags.WriteAll);
            builder.SetPassData(new IndependentGraphicsPassData { targetC = targetC, pso = depthPso, width = width, height = height });
            builder.SetRenderFunc<IndependentGraphicsPassData>(static (ref readonly data, ctx) =>
            {
                var cmd = ((IUnsafeRenderContext)ctx).GetCommandBufferUnsafe();
                cmd.SetPipelineState(data.pso);
                cmd.SetViewport(new ViewportDesc { X = 0, Y = 0, Width = data.width, Height = data.height, MinDepth = 0.0f, MaxDepth = 1.0f });
                cmd.SetScissorRect(new ScissorRectDesc { Left = 0, Top = 0, Right = data.width, Bottom = data.height });
                cmd.DispatchMesh(1, 1, 1);
            });
        }

        // 4. Graphics Join / Consumer Pass (Raster)
        using (var builder = rg.AddRasterRenderPass<GraphicsJoinPassData>("Graphics_Join_Consumer"))
        {
            builder.UseTexture(targetA, AccessFlags.Read);
            builder.UseTexture(targetB, AccessFlags.Read);
            builder.UseTexture(targetC, AccessFlags.Read);
            builder.SetColorAttachment(backBuffer, 0, AccessFlags.WriteAll);
            builder.SetPassData(new GraphicsJoinPassData { targetA = targetA, targetB = targetB, targetC = targetC, backBuffer = backBuffer, pso = compositePso, frameIndex = frameIndex, width = width, height = height });
            builder.SetRenderFunc<GraphicsJoinPassData>(static (ref readonly data, ctx) =>
            {
                var cmd = ((IUnsafeRenderContext)ctx).GetCommandBufferUnsafe();
                var targetBHandle = ctx.GetActualTexture(data.targetB);
                var srvIndex = ctx.ResourceDatabase.GetBindlessIndex(targetBHandle.AsResource(), BindlessAccess.ShaderResource);

                var constants = new PushConstantsData
                {
                    frameBuffer = srvIndex,
                    viewBuffer = 0,
                    instanceIndex = data.frameIndex,
                };

                cmd.SetPipelineState(data.pso);
                cmd.SetGraphicsRoot32Constants(RootSignatureLayout.PUSH_CONSTANT_SLOT, constants.AsUInts());
                cmd.SetViewport(new ViewportDesc { X = 0, Y = 0, Width = data.width, Height = data.height, MinDepth = 0.0f, MaxDepth = 1.0f });
                cmd.SetScissorRect(new ScissorRectDesc { Left = 0, Top = 0, Right = data.width, Bottom = data.height });
                cmd.DispatchMesh(1, 1, 1);
            });
        }
    }

    public void Dispose()
    {
        _renderGraph.Dispose();
        _gpuScene.Dispose();
    }
}
