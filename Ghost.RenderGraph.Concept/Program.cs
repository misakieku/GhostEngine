using Ghost.RenderGraph.Concept;


//ConsoleAPI.WriteLine("==================================================");
//ConsoleAPI.WriteLine("     Transient Render Graph - Proof of Concept");
//ConsoleAPI.WriteLine("     Using Typed Pass Data and Blackboard Pattern");
//ConsoleAPI.WriteLine("==================================================\n");

var renderGraph = new RenderGraph();

for (int i = 0; i < 500; i++)
{
    BuildGraph(renderGraph);
}

var sw = new System.Diagnostics.Stopwatch();
var gcBefore = GC.GetAllocatedBytesForCurrentThread();
sw.Start();

for (int i = 0; i < 500; i++)
{
    BuildGraph(renderGraph);
}

//BuildGraph(renderGraph);

sw.Stop();
var gcAfter = GC.GetAllocatedBytesForCurrentThread();

Console.WriteLine($"{sw.Elapsed.TotalNanoseconds / 500} ns");
Console.WriteLine($"GC Allocated Bytes: {(gcAfter - gcBefore) / 500} bytes");

//Console.WriteLine("\nPress any key to exit...");
//Console.ReadKey();

static void BuildGraph(RenderGraph renderGraph)
{
    renderGraph.Reset();

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

        builder.SetRenderFunc((data, cmd) =>
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

        builder.SetRenderFunc((data, cmd) =>
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
        builder.SetComputeFunc((data, cmd) =>
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

        builder.SetRenderFunc((data, cmd) =>
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

        builder.SetRenderFunc((data, cmd) =>
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

        builder.SetRenderFunc((data, cmd) =>
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
        builder.SetRenderFunc((data, cmd) =>
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

        builder.SetRenderFunc((data, cmd) =>
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