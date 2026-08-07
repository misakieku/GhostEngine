using BenchmarkDotNet.Attributes;
using Ghost.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.UnitTest.MockingEnvironment;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.UnitTest.Benchmarks.Graphics;

[MemoryDiagnoser]
public class RenderGraphBenchmark
{
    private struct CullingPassData
    {
        public Identifier<RGBuffer> sceneBuffer;
        public Identifier<RGBuffer> visibleBuffer;
        public Identifier<RGTexture> hizPyramid;
    }

    private struct ShadowPassData
    {
        public Identifier<RGBuffer> visibleBuffer;
        public Identifier<RGTexture> shadowMap;
    }

    private struct GBufferPassData
    {
        public Identifier<RGBuffer> visibleBuffer;
        public Identifier<RGTexture> albedo;
        public Identifier<RGTexture> normal;
        public Identifier<RGTexture> material;
        public Identifier<RGTexture> velocity;
        public Identifier<RGTexture> depth;
    }

    private struct ComputePostPassData
    {
        public Identifier<RGTexture> input;
        public Identifier<RGTexture> output;
    }

    private struct FinalCompositePassData
    {
        public Identifier<RGTexture> source;
        public Identifier<RGTexture> backBuffer;
    }

    private MockingRenderDevice _renderDevice = null!;
    private MockingResourceDatabase _resourceDatabase = null!;
    private MockingResourceAllocator _resourceAllocator = null!;
    private MockingPipelineLibrary _pipelineLibrary = null!;
    private MockingGraphicsEngine _graphicsEngine = null!;
    private ICommandAllocator _graphicsCommandAllocator = null!;
    private ICommandAllocator _computeCommandAllocator = null!;
    private FrameScheduler _frameScheduler = null!;
    private ResourceManager _resourceManager = null!;
    private ShaderLibrary _shaderLibrary = null!;

    private RenderGraph _renderGraph = null!;
    private RenderGraphExecutionContext _executionContext;
    private ViewState _viewState;

    private Handle<GPUTexture> _importedBackBufferHandle;
    private Handle<GPUBuffer> _importedSceneBufferHandle;

    [GlobalSetup]
    public void Setup()
    {
        AllocationManager.Initialize();

        _renderDevice = new MockingRenderDevice();
        _resourceDatabase = new MockingResourceDatabase();
        _resourceAllocator = new MockingResourceAllocator(_resourceDatabase);
        _pipelineLibrary = new MockingPipelineLibrary();
        _graphicsEngine = new MockingGraphicsEngine(_renderDevice, _resourceDatabase, _resourceAllocator);
        _graphicsCommandAllocator = _graphicsEngine.CreateCommandAllocator(CommandBufferType.Graphics);
        _computeCommandAllocator = _graphicsEngine.CreateCommandAllocator(CommandBufferType.Compute);
        _frameScheduler = new FrameScheduler(_graphicsEngine);

        _resourceManager = new ResourceManager(_renderDevice, _resourceAllocator, _resourceDatabase);
        _shaderLibrary = new ShaderLibrary(null, _pipelineLibrary, string.Empty);

        _renderGraph = new RenderGraph(_resourceDatabase, _resourceAllocator, _pipelineLibrary, _resourceManager, _shaderLibrary);
        _executionContext = new RenderGraphExecutionContext(
            _graphicsEngine,
            _frameScheduler,
            _graphicsCommandAllocator,
            _computeCommandAllocator);

        _viewState = new ViewState
        {
            actualWidth = 3840,
            actualHeight = 2160,
            viewportWidth = 3840,
            viewportHeight = 2160
        };

        var backBufferDesc = new TextureDesc
        {
            Width = 3840,
            Height = 2160,
            Format = TextureFormat.R8G8B8A8_UNorm,
            Usage = TextureUsage.RenderTarget
        };

        _importedBackBufferHandle = _resourceAllocator.CreateTexture(in backBufferDesc);

        var sceneDataDesc = new BufferDesc { Size = 10 * 1024 * 1024 }; // 10MB
        _importedSceneBufferHandle = _resourceAllocator.CreateBuffer(in sceneDataDesc);

        // Pre-warm once so cache and execution scratch are initialized.
        BuildAAAPipeline(_renderGraph, _importedBackBufferHandle, _importedSceneBufferHandle);
        _ = _renderGraph.CompileAndExecute(_executionContext, _viewState).GetValueOrThrow();
        _frameScheduler.Flush();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _frameScheduler.Dispose();
        _renderGraph.Dispose();
        _shaderLibrary.Dispose();
        _resourceManager.Dispose();
        _graphicsCommandAllocator.Dispose();
        _computeCommandAllocator.Dispose();
        _graphicsEngine.Dispose();
        _pipelineLibrary.Dispose();
        _resourceAllocator.Dispose();
        _resourceDatabase.Dispose();
        _renderDevice.Dispose();

        AllocationManager.Dispose();
    }

    /// <summary>
    /// Builds a complex ~35-pass AAA rendering pipeline matching Frostbite / UE5 engine architectures.
    /// </summary>
    private void BuildAAAPipeline(RenderGraph rg, Handle<GPUTexture> backBufferHandle, Handle<GPUBuffer> sceneBufferHandle)
    {
        var backBuffer = rg.ImportTexture(backBufferHandle);
        var sceneBuffer = rg.ImportBuffer(sceneBufferHandle);

        // ---------------------------------------------------------------------------------------
        // 1. GPU Culling & Hi-Z (3 Async Compute Passes)
        // ---------------------------------------------------------------------------------------
        Identifier<RGBuffer> visibleBuffer;
        Identifier<RGTexture> hizPyramid;

        using (var builder = rg.AddComputeRenderPass<CullingPassData>("GPU_Culling_HiZ"))
        {
            builder.EnableAsyncCompute(true);
            hizPyramid = builder.CreateTexture(RGTextureDesc.Relative(0.5f, TextureFormat.R32_UInt), "HiZPyramid");
            visibleBuffer = builder.CreateBuffer(new BufferDesc { Size = 2 * 1024 * 1024 }, "VisibleBuffer");
            builder.UseBuffer(sceneBuffer, AccessFlags.Read);
            builder.UseBuffer(visibleBuffer, AccessFlags.Write);
            builder.UseTexture(hizPyramid, AccessFlags.Write);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        using (var builder = rg.AddComputeRenderPass<CullingPassData>("Light_Binning"))
        {
            builder.EnableAsyncCompute(true);
            builder.UseBuffer(sceneBuffer, AccessFlags.Read);
            builder.UseBuffer(visibleBuffer, AccessFlags.ReadWrite);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        // ---------------------------------------------------------------------------------------
        // 2. Depth Prepass (1 Raster Pass)
        // ---------------------------------------------------------------------------------------
        Identifier<RGTexture> mainDepth;
        using (var builder = rg.AddRasterRenderPass<GBufferPassData>("DepthPrepass"))
        {
            mainDepth = builder.CreateTexture(RGTextureDesc.RelativeDepth(1.0f), "MainDepth");
            builder.UseBuffer(visibleBuffer, AccessFlags.Read);
            builder.SetDepthAttachment(mainDepth, AccessFlags.WriteAll);
            builder.SetPassData(new GBufferPassData());
            builder.SetRenderFunc<GBufferPassData>(static (ref readonly data, ctx) => { });
        }

        // ---------------------------------------------------------------------------------------
        // 3. Shadow Map Passes (6 Raster Passes)
        // ---------------------------------------------------------------------------------------
        for (var c = 0; c < 4; c++)
        {
            using var builder = rg.AddRasterRenderPass<ShadowPassData>($"SunShadow_Cascade");
            var shadowTex = builder.CreateTexture(RGTextureDesc.Absolute(2048, 2048, TextureFormat.D32_Float), $"SunShadow");
            builder.UseBuffer(visibleBuffer, AccessFlags.Read);
            builder.SetDepthAttachment(shadowTex, AccessFlags.WriteAll);
            builder.SetPassData(new ShadowPassData());
            builder.SetRenderFunc<ShadowPassData>(static (ref readonly data, ctx) => { });
        }

        for (var s = 0; s < 2; s++)
        {
            using var builder = rg.AddRasterRenderPass<ShadowPassData>($"SpotShadow");
            var shadowTex = builder.CreateTexture(RGTextureDesc.Absolute(1024, 1024, TextureFormat.D32_Float), $"SpotShadow");
            builder.UseBuffer(visibleBuffer, AccessFlags.Read);
            builder.SetDepthAttachment(shadowTex, AccessFlags.WriteAll);
            builder.SetPassData(new ShadowPassData());
            builder.SetRenderFunc<ShadowPassData>(static (ref readonly data, ctx) => { });
        }

        // ---------------------------------------------------------------------------------------
        // 4. Base Pass / G-Buffer (1 Raster Pass with 4 RTVs + Depth)
        // ---------------------------------------------------------------------------------------
        Identifier<RGTexture> gAlbedo, gNormal, gMaterial, gVelocity;
        using (var builder = rg.AddRasterRenderPass<GBufferPassData>("GBuffer_BasePass"))
        {
            gAlbedo = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm), "GBuffer_Albedo");
            gNormal = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R16G16B16A16_Float), "GBuffer_Normal");
            gMaterial = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm), "GBuffer_Material");
            gVelocity = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R16G16_Float), "GBuffer_Velocity");

            builder.UseBuffer(visibleBuffer, AccessFlags.Read);
            builder.SetColorAttachment(gAlbedo, 0, AccessFlags.WriteAll);
            builder.SetColorAttachment(gNormal, 1, AccessFlags.WriteAll);
            builder.SetColorAttachment(gMaterial, 2, AccessFlags.WriteAll);
            builder.SetColorAttachment(gVelocity, 3, AccessFlags.WriteAll);
            builder.SetDepthAttachment(mainDepth, AccessFlags.Read);

            builder.SetPassData(new GBufferPassData());
            builder.SetRenderFunc<GBufferPassData>(static (ref readonly data, ctx) => { });
        }

        // ---------------------------------------------------------------------------------------
        // 5. GTAO Ambient Occlusion (2 Compute Passes)
        // ---------------------------------------------------------------------------------------
        Identifier<RGTexture> gtaoRaw, gtaoDenoised;
        using (var builder = rg.AddComputeRenderPass<ComputePostPassData>("GTAO_Compute"))
        {
            builder.EnableAsyncCompute(true);
            gtaoRaw = builder.CreateTexture(RGTextureDesc.Relative(0.5f, TextureFormat.R8_UNorm), "GTAO_Raw");
            builder.UseTexture(mainDepth, AccessFlags.Read);
            builder.UseTexture(gNormal, AccessFlags.Read);
            builder.UseTexture(gtaoRaw, AccessFlags.Write);
            builder.SetPassData(new ComputePostPassData());
            builder.SetRenderFunc<ComputePostPassData>(static (ref readonly data, ctx) => { });
        }

        using (var builder = rg.AddComputeRenderPass<ComputePostPassData>("GTAO_SpatialDenoise"))
        {
            gtaoDenoised = builder.CreateTexture(RGTextureDesc.Relative(0.5f, TextureFormat.R8_UNorm), "GTAO_Denoised");
            builder.UseTexture(gtaoRaw, AccessFlags.Read);
            builder.UseTexture(gtaoDenoised, AccessFlags.Write);
            builder.SetPassData(new ComputePostPassData());
            builder.SetRenderFunc<ComputePostPassData>(static (ref readonly data, ctx) => { });
        }

        // ---------------------------------------------------------------------------------------
        // 6. Deferred Tiled Lighting & SSR (3 Compute Passes)
        // ---------------------------------------------------------------------------------------
        Identifier<RGTexture> hdrLighting, ssrRaw;
        using (var builder = rg.AddComputeRenderPass<ComputePostPassData>("Deferred_Tiled_Lighting"))
        {
            hdrLighting = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R16G16B16A16_Float), "HDR_Lighting");
            builder.UseTexture(gAlbedo, AccessFlags.Read);
            builder.UseTexture(gNormal, AccessFlags.Read);
            builder.UseTexture(gMaterial, AccessFlags.Read);
            builder.UseTexture(mainDepth, AccessFlags.Read);
            builder.UseTexture(gtaoDenoised, AccessFlags.Read);
            builder.UseTexture(hdrLighting, AccessFlags.Write);
            builder.SetPassData(new ComputePostPassData());
            builder.SetRenderFunc<ComputePostPassData>(static (ref readonly data, ctx) => { });
        }

        using (var builder = rg.AddComputeRenderPass<ComputePostPassData>("SSR_RayMarch"))
        {
            ssrRaw = builder.CreateTexture(RGTextureDesc.Relative(0.5f, TextureFormat.R16G16B16A16_Float), "SSR_Raw");
            builder.UseTexture(hdrLighting, AccessFlags.Read);
            builder.UseTexture(gNormal, AccessFlags.Read);
            builder.UseTexture(mainDepth, AccessFlags.Read);
            builder.UseTexture(ssrRaw, AccessFlags.Write);
            builder.SetPassData(new ComputePostPassData());
            builder.SetRenderFunc<ComputePostPassData>(static (ref readonly data, ctx) => { });
        }

        using (var builder = rg.AddComputeRenderPass<ComputePostPassData>("SSR_TemporalDenoise"))
        {
            builder.UseTexture(ssrRaw, AccessFlags.Read);
            builder.UseTexture(hdrLighting, AccessFlags.ReadWrite);
            builder.SetPassData(new ComputePostPassData());
            builder.SetRenderFunc<ComputePostPassData>(static (ref readonly data, ctx) => { });
        }

        // ---------------------------------------------------------------------------------------
        // 7. Volumetric Fog (1 Compute Pass)
        // ---------------------------------------------------------------------------------------
        using (var builder = rg.AddComputeRenderPass<ComputePostPassData>("VolumetricFog_Inject"))
        {
            builder.EnableAsyncCompute(true);
            builder.UseTexture(hdrLighting, AccessFlags.ReadWrite);
            builder.SetPassData(new ComputePostPassData());
            builder.SetRenderFunc<ComputePostPassData>(static (ref readonly data, ctx) => { });
        }

        // ---------------------------------------------------------------------------------------
        // 8. Post-Processing Pipeline (TAA, DoF, Bloom Pyramid 5 down + 4 up, Tonemap, FSR) (~15 Passes)
        // ---------------------------------------------------------------------------------------
        Identifier<RGTexture> taaOutput;
        using (var builder = rg.AddComputeRenderPass<ComputePostPassData>("TAA_Resolve"))
        {
            taaOutput = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R16G16B16A16_Float), "TAA_Output");
            builder.UseTexture(hdrLighting, AccessFlags.Read);
            builder.UseTexture(gVelocity, AccessFlags.Read);
            builder.UseTexture(mainDepth, AccessFlags.Read);
            builder.UseTexture(taaOutput, AccessFlags.Write);
            builder.SetPassData(new ComputePostPassData());
            builder.SetRenderFunc<ComputePostPassData>(static (ref readonly data, ctx) => { });
        }

        // Bloom Downsample Chain (5 Passes)
        var bloomChain = new Identifier<RGTexture>[5];
        var prevTex = taaOutput;
        for (var i = 0; i < 5; i++)
        {
            var scale = 0.5f / (1 << i);
            using var builder = rg.AddComputeRenderPass<ComputePostPassData>($"Bloom_Downsample");
            var bTex = builder.CreateTexture(RGTextureDesc.Relative(scale, TextureFormat.R16G16B16A16_Float), $"Bloom_Down");
            builder.UseTexture(prevTex, AccessFlags.Read);
            builder.UseTexture(bTex, AccessFlags.Write);
            bloomChain[i] = bTex;
            prevTex = bTex;
            builder.SetPassData(new ComputePostPassData());
            builder.SetRenderFunc<ComputePostPassData>(static (ref readonly data, ctx) => { });
        }

        // Bloom Upsample Chain (4 Passes)
        for (var i = 3; i >= 0; i--)
        {
            using var builder = rg.AddComputeRenderPass<ComputePostPassData>($"Bloom_Upsample");
            builder.UseTexture(bloomChain[i + 1], AccessFlags.Read);
            builder.UseTexture(bloomChain[i], AccessFlags.ReadWrite);
            builder.SetPassData(new ComputePostPassData());
            builder.SetRenderFunc<ComputePostPassData>(static (ref readonly data, ctx) => { });
        }

        Identifier<RGTexture> tonemapped;
        using (var builder = rg.AddComputeRenderPass<ComputePostPassData>("Tonemap_ColorGrading"))
        {
            tonemapped = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm), "Tonemapped");
            builder.UseTexture(taaOutput, AccessFlags.Read);
            builder.UseTexture(bloomChain[0], AccessFlags.Read);
            builder.UseTexture(tonemapped, AccessFlags.Write);
            builder.SetPassData(new ComputePostPassData());
            builder.SetRenderFunc<ComputePostPassData>(static (ref readonly data, ctx) => { });
        }

        Identifier<RGTexture> finalLDR;
        using (var builder = rg.AddComputeRenderPass<ComputePostPassData>("FSR3_Upscale"))
        {
            finalLDR = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm), "FinalLDR");
            builder.UseTexture(tonemapped, AccessFlags.Read);
            builder.UseTexture(gVelocity, AccessFlags.Read);
            builder.UseTexture(finalLDR, AccessFlags.Write);
            builder.SetPassData(new ComputePostPassData());
            builder.SetRenderFunc<ComputePostPassData>(static (ref readonly data, ctx) => { });
        }

        // ---------------------------------------------------------------------------------------
        // 9. Final UI & Swapchain Blit (1 Unsafe Pass, Writes BackBuffer)
        // ---------------------------------------------------------------------------------------
        using (var builder = rg.AddUnsafeRenderPass<FinalCompositePassData>("UI_Swapchain_Blit"))
        {
            builder.SetPassData(new FinalCompositePassData
            {
                source = builder.UseTexture(finalLDR, AccessFlags.Read),
                backBuffer = builder.UseRenderTargetTexture(backBuffer, AccessFlags.WriteAll)
            });
            builder.SetRenderFunc<FinalCompositePassData>(static (ref readonly data, ctx) => { });
        }
    }

    /// <summary>
    /// Benchmark 1: Graph Declaration Only.
    /// Measures pooled pass reset, resource declaration, deduplication, and completed-pass validation.
    /// </summary>
    [Benchmark]
    public void Declare_PipelineOnly()
    {
        _renderGraph.Reset();
        BuildAAAPipeline(_renderGraph, _importedBackBufferHandle, _importedSceneBufferHandle);
        AllocationManager.ResetTempAllocator();
    }

    /// <summary>
    /// Benchmark 2: Cold Compile (Cache Miss).
    /// Measures pass creation, DAG sorting, memory aliasing allocation plan, native pass merging, and binary stream compilation.
    /// </summary>
    [Benchmark]
    public void Compile_Cold_CacheMiss()
    {
        _renderGraph.Reset();
        _renderGraph.InvalidateCache();

        BuildAAAPipeline(_renderGraph, _importedBackBufferHandle, _importedSceneBufferHandle);
        var result = _renderGraph.CompileAndExecute(
            _executionContext,
            _viewState);

        if (result.IsFailure)
        {
            throw new InvalidOperationException("Cold compile failed: " + result.Error);
        }

        _frameScheduler.Flush();
        AllocationManager.ResetTempAllocator();
    }

    /// <summary>
    /// Benchmark 3: Warm Compile (Cache Hit).
    /// Measures frame setup + graph hash calculation + compilation cache restoration.
    /// </summary>
    [Benchmark]
    public void Compile_Warm_CacheHit()
    {
        _renderGraph.Reset();

        BuildAAAPipeline(_renderGraph, _importedBackBufferHandle, _importedSceneBufferHandle);
        var result = _renderGraph.CompileAndExecute(
            _executionContext,
            _viewState);

        if (result.IsFailure)
        {
            throw new InvalidOperationException("Warm compile failed: " + result.Error);
        }

        _frameScheduler.Flush();
        AllocationManager.ResetTempAllocator();
    }
}
