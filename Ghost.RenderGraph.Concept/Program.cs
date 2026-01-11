using Ghost.RenderGraph.Concept;

var renderGraph = new RenderGraph();

#if !DEBUG
const int _ITERATION = 500;
for (var i = 0; i < _ITERATION; i++)
{
    ExecuteGraph(renderGraph);
}

GC.Collect();
GC.WaitForPendingFinalizers();
// Thread.Sleep(1000); // Leave a gap in visual studio allocations timeline
var sw = new System.Diagnostics.Stopwatch();
var gcBefore = GC.GetAllocatedBytesForCurrentThread();
sw.Start();

for (var i = 0; i < _ITERATION; i++)
{
    ExecuteGraph(renderGraph);
}

sw.Stop();
var gcAfter = GC.GetAllocatedBytesForCurrentThread();

Console.WriteLine($"{sw.Elapsed.TotalNanoseconds / _ITERATION} ns (per iteration)");
Console.WriteLine($"GC Allocated Bytes: {(gcAfter - gcBefore) / _ITERATION} bytes (per iteration)");
#else
// Run twice to demonstrate cache hit
Console.WriteLine("=== FRAME 1 (Cache Miss Expected) ===");
ExecuteGraph(renderGraph);

Console.WriteLine("\n\n=== FRAME 2 (Cache Hit Expected) ===");
ExecuteGraph(renderGraph);
#endif

static void ExecuteGraph(RenderGraph renderGraph)
{
    renderGraph.Reset(); // new RenderGraph()

    // Import external resources
    var backbuffer = renderGraph.ImportTexture(
        "Backbuffer",
        new TextureDescriptor(1920, 1080, TextureFormat.RGBA8, "Backbuffer"));

    // ===== GBuffer Pass =====
    GBufferData gbufferData;
    using (var builder = renderGraph.AddRenderPass<GBufferData>("GBuffer Pass", out gbufferData))
    {
        // Create GBuffer textures
        var albedo = builder.CreateTexture(new TextureDescriptor(1920, 1080, TextureFormat.RGBA8, "GBuffer.Albedo"));
        var normal = builder.CreateTexture(new TextureDescriptor(1920, 1080, TextureFormat.RGBA16F, "GBuffer.Normal"));
        var depth = builder.CreateTexture(new TextureDescriptor(1920, 1080, TextureFormat.Depth32F, "GBuffer.Depth"));

        // Store in pass data and mark as written
        gbufferData.Albedo = builder.WriteTexture(albedo);
        gbufferData.Normal = builder.WriteTexture(normal);
        gbufferData.Depth = builder.UseDepthBuffer(depth, writeAccess: true);

        builder.SetRenderFunc<GBufferData>((data, cmd) =>
        {
            cmd.SetRenderTarget(data.Albedo.Name);
            cmd.SetRenderTarget(data.Normal.Name);
            cmd.SetDepthStencil(data.Depth.Name);
            cmd.ClearRenderTarget(data.Albedo.Name, 0, 0, 0, 1);
            cmd.ClearRenderTarget(data.Normal.Name, 0.5f, 0.5f, 1.0f, 1);
            cmd.ClearDepth(data.Depth.Name, 1.0f);
            cmd.Draw(36000);
        });
    }

    // Store GBuffer data in blackboard for other passes
    renderGraph.Blackboard.Add(gbufferData);

    // ===== Lighting Pass =====
    RenderGraphTextureHandle lightingOutput;
    using (var builder = renderGraph.AddRenderPass<LightingPassData>("Lighting Pass", out var lightingData))
    {
        // Read GBuffer from blackboard
        var gbuffer = renderGraph.Blackboard.Get<GBufferData>();

        lightingData.GBufferAlbedo = builder.ReadTexture(gbuffer.Albedo);
        lightingData.GBufferNormal = builder.ReadTexture(gbuffer.Normal);
        lightingData.GBufferDepth = builder.ReadTexture(gbuffer.Depth);

        // Create output texture
        lightingOutput = builder.CreateTexture(
            new TextureDescriptor(1920, 1080, TextureFormat.RGBA16F, "LightingResult"));
        lightingData.OutputLighting = builder.WriteTexture(lightingOutput);

        builder.SetRenderFunc<LightingPassData>((data, cmd) =>
        {
            cmd.BindShaderResource(data.GBufferAlbedo.Name, 0);
            cmd.BindShaderResource(data.GBufferNormal.Name, 1);
            cmd.BindShaderResource(data.GBufferDepth.Name, 2);
            cmd.SetRenderTarget(data.OutputLighting.Name);
            cmd.Draw(3);
        });
    }

    // ===== SSAO Pass (Async Compute) =====
    RenderGraphTextureHandle ssaoOutput;
    using (var builder = renderGraph.AddRenderPass<SSAOPassData>("SSAO Pass (Async)", out var ssaoData))
    {
        var gbuffer = renderGraph.Blackboard.Get<GBufferData>();

        ssaoData.GBufferDepth = builder.ReadTexture(gbuffer.Depth);
        ssaoData.GBufferNormal = builder.ReadTexture(gbuffer.Normal);

        // SSAO Output
        ssaoOutput = builder.CreateTexture(
            new TextureDescriptor(1920, 1080, TextureFormat.RGBA8, "SSAO"));
        ssaoData.OutputSSAO = builder.WriteTexture(ssaoOutput);

        // Use SetComputeFunc with asyncCompute: true
        builder.SetComputeFunc<SSAOPassData>((data, cmd) =>
        {
            cmd.BindShaderResource(data.GBufferDepth.Name, 0);
            cmd.BindShaderResource(data.GBufferNormal.Name, 1);
            cmd.BindUnorderedAccess(data.OutputSSAO.Name, 0);
            cmd.Dispatch(1920 / 8, 1080 / 8, 1);
        }, asyncCompute: true);
    }

    // ===== Bloom Downsample Pass (will alias with albedo) =====
    RenderGraphTextureHandle bloomOutput;
    using (var builder = renderGraph.AddRenderPass<BloomDownsampleData>("Bloom Downsample", out var bloomData))
    {
        bloomData.Input = builder.ReadTexture(lightingOutput);

        // Create a texture that will alias with SSAO (same size, same format)
        bloomOutput = builder.CreateTexture(
            new TextureDescriptor(1920, 1080, TextureFormat.RGBA8, "BloomDownsample"));
        bloomData.Output = builder.WriteTexture(bloomOutput);

        builder.SetRenderFunc<BloomDownsampleData>((data, cmd) =>
        {
            cmd.BindShaderResource(data.Input.Name, 0);
            cmd.SetRenderTarget(data.Output.Name);
            cmd.Draw(3);
        });
    }

    // ===== Temporal AA Pass =====
    RenderGraphTextureHandle taaOutput;
    using (var builder = renderGraph.AddRenderPass<TAAPassData>("Temporal AA", out var taaData))
    {
        taaData.InputLighting = builder.ReadTexture(lightingOutput);

        taaOutput = builder.CreateTexture(
            new TextureDescriptor(1920, 1080, TextureFormat.RGBA16F, "TAA.Result"));
        taaData.OutputTAA = builder.WriteTexture(taaOutput);

        builder.SetRenderFunc<TAAPassData>((data, cmd) =>
        {
            cmd.BindShaderResource(data.InputLighting.Name, 0);
            cmd.SetRenderTarget(data.OutputTAA.Name);
            cmd.Draw(3);
        });
    }

    // ===== Post Processing Pass =====
    using (var builder = renderGraph.AddRenderPass<PostProcessingPassDataV2>("Post Processing", out var postData))
    {
        postData.InputTAA = builder.ReadTexture(taaOutput);
        postData.InputSSAO = builder.ReadTexture(ssaoOutput);
        postData.InputBloom = builder.ReadTexture(bloomOutput);
        postData.OutputBackbuffer = builder.WriteTexture(backbuffer);

        builder.SetRenderFunc<PostProcessingPassDataV2>((data, cmd) =>
        {
            cmd.BindShaderResource(data.InputTAA.Name, 0);
            cmd.BindShaderResource(data.InputSSAO.Name, 1);
            cmd.BindShaderResource(data.InputBloom.Name, 2);
            cmd.SetRenderTarget(data.OutputBackbuffer.Name);
            cmd.Draw(3);
        });
    }

    // ===== GPU Profiler Marker Pass (non-cullable, textureless) =====
    using (var builder = renderGraph.AddRenderPass<ProfilerMarkerData>("GPU Profiler Begin Frame", out var profilerData))
    {
        builder.SetAllowCulling(false); // Never cull this - it's for debugging/profiling
        builder.SetRenderFunc<ProfilerMarkerData>((data, cmd) =>
        {
            // Note: In a real implementation we would have specific profiler commands
            // For now, since RasterRenderContext doesn't expose generic console write, we skip the print
            // or we would add a specific Profiler method to the context
        });
    }

    // ===== Unused Debug Pass (will be culled) =====
    using (var builder = renderGraph.AddRenderPass<DebugPassData>("Unused Debug Pass", out var debugData))
    {
        debugData.DebugTexture = builder.WriteTexture(
            builder.CreateTexture(new TextureDescriptor(512, 512, TextureFormat.RGBA8, "DebugTexture")));

        builder.SetRenderFunc<DebugPassData>((data, cmd) =>
        {
            cmd.SetRenderTarget(data.DebugTexture.Name);
            cmd.ClearRenderTarget(data.DebugTexture.Name, 1, 0, 1, 1);
            cmd.Draw(100);
        });
    }

    // Compile and execute the render graph
    renderGraph.Compile();
    renderGraph.Execute();
}