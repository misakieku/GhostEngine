using BenchmarkDotNet.Attributes;
using Ghost.Core;

namespace Ghost.RenderGraph.Concept.Benchmark;

[MemoryDiagnoser]
public class RenderGraphBenchmark
{
    private RenderGraph _renderGraph = null!;

    [GlobalSetup]
    public void Setup()
    {
        _renderGraph = new RenderGraph();
    }

    [Benchmark]
    public void Execute()
    {
        ExecuteGraph(_renderGraph);
    }

    public static void ExecuteGraph(RenderGraph renderGraph)
    {
        renderGraph.Reset(); // new RenderGraph()

        // Import external resources
        var backbuffer = renderGraph.ImportTexture(
            new TextureDescriptor(1920, 1080, TextureFormat.RGBA8, "Backbuffer"));

        // ===== GBuffer Pass =====
        GBufferData gbufferData;
        using (var builder = renderGraph.AddRasterRenderPass<GBufferData>("GBuffer Pass", out gbufferData))
        {
            // Create GBuffer textures
            gbufferData.Albedo = builder.CreateTexture(new TextureDescriptor(1920, 1080, TextureFormat.RGBA8, "GBuffer.Albedo"));
            gbufferData.Normal = builder.CreateTexture(new TextureDescriptor(1920, 1080, TextureFormat.RGBA16F, "GBuffer.Normal"));
            gbufferData.Depth = builder.CreateTexture(new TextureDescriptor(1920, 1080, TextureFormat.Depth32F, "GBuffer.Depth"));

            // Store in pass data and mark as written
            builder.SetColorAttachment(gbufferData.Albedo, 0);
            builder.SetColorAttachment(gbufferData.Normal, 1);
            builder.SetDepthAttachment(gbufferData.Depth);

            builder.SetRenderFunc<GBufferData>(static (data, cmd) =>
            {
                // New api will handle render target setup and clearing automatically

                //cmd.SetRenderTarget(data.Albedo.Name);
                //cmd.SetRenderTarget(data.Normal.Name);
                //cmd.SetDepthStencil(data.Depth.Name);
                //cmd.ClearRenderTarget(data.Albedo.Name, 0, 0, 0, 1);
                //cmd.ClearRenderTarget(data.Normal.Name, 0.5f, 0.5f, 1.0f, 1);
                //cmd.ClearDepth(data.Depth.Name, 1.0f);
                cmd.Draw(36000);
            });
        }

        // Store GBuffer data in blackboard for other passes
        renderGraph.Blackboard.Add(gbufferData);

        // ===== Lighting Pass =====
        Identifier<RGTexture> lightingOutput;
        using (var builder = renderGraph.AddRasterRenderPass<LightingPassData>("Lighting Pass", out var lightingData))
        {
            // Read GBuffer from blackboard
            var gbuffer = renderGraph.Blackboard.Get<GBufferData>();

            lightingData.GBufferAlbedo = builder.UseTexture(gbuffer.Albedo, AccessFlags.Read);
            lightingData.GBufferNormal = builder.UseTexture(gbuffer.Normal, AccessFlags.Read);
            lightingData.GBufferDepth = builder.UseTexture(gbuffer.Depth, AccessFlags.Read);

            // Create output texture
            lightingOutput = builder.CreateTexture(
                new TextureDescriptor(1920, 1080, TextureFormat.RGBA16F, "LightingResult"));
            builder.SetColorAttachment(lightingOutput, 0);

            lightingData.OutputLighting = lightingOutput;

            builder.SetRenderFunc<LightingPassData>(static (data, cmd) =>
            {
                cmd.BindShaderResource(data.GBufferAlbedo.AsResource(), 0);
                cmd.BindShaderResource(data.GBufferNormal.AsResource(), 1);
                cmd.BindShaderResource(data.GBufferDepth.AsResource(), 2);
                cmd.Draw(3);
            });
        }

        // ===== SSAO Pass (Async Compute) =====
        Identifier<RGTexture> ssaoOutput;
        using (var builder = renderGraph.AddComputeRenderPass<SSAOPassData>("SSAO Pass (Async)", out var ssaoData))
        {
            var gbuffer = renderGraph.Blackboard.Get<GBufferData>();

            ssaoData.GBufferDepth = builder.UseTexture(gbuffer.Depth, AccessFlags.Read);
            ssaoData.GBufferNormal = builder.UseTexture(gbuffer.Normal, AccessFlags.Read);

            // SSAO Output
            ssaoOutput = builder.CreateTexture(
                new TextureDescriptor(1920, 1080, TextureFormat.RGBA8, "SSAO"));
            ssaoData.OutputSSAO = builder.UseTexture(ssaoOutput, AccessFlags.Write);

            builder.EnableAsyncCompute(true);

            // Use SetComputeFunc with asyncCompute: true
            builder.SetRenderFunc<SSAOPassData>(static (data, cmd) =>
            {
                cmd.BindShaderResource(data.GBufferDepth.AsResource(), 0);
                cmd.BindShaderResource(data.GBufferNormal.AsResource(), 1);
                cmd.BindUnorderedAccess(data.OutputSSAO.AsResource(), 0);
                cmd.Dispatch(1920 / 8, 1080 / 8, 1);
            });
        }

        // ===== Bloom Downsample Pass (will alias with albedo) =====
        Identifier<RGTexture> bloomOutput;
        using (var builder = renderGraph.AddRasterRenderPass<BloomDownsampleData>("Bloom Downsample", out var bloomData))
        {
            bloomData.Input = builder.UseTexture(lightingOutput, AccessFlags.Read);

            // Create a texture that will alias with SSAO (same size, same format)
            bloomOutput = builder.CreateTexture(
                new TextureDescriptor(1920, 1080, TextureFormat.RGBA8, "BloomDownsample"));
            builder.SetColorAttachment(bloomOutput, 0);

            bloomData.Output = bloomOutput;

            builder.SetRenderFunc<BloomDownsampleData>(static (data, cmd) =>
            {
                cmd.BindShaderResource(data.Input.AsResource(), 0);
                cmd.Draw(3);
            });
        }

        // ===== Temporal AA Pass =====
        Identifier<RGTexture> taaOutput;
        using (var builder = renderGraph.AddRasterRenderPass<TAAPassData>("Temporal AA", out var taaData))
        {
            taaData.InputLighting = builder.UseTexture(lightingOutput, AccessFlags.Read);

            taaOutput = builder.CreateTexture(
                new TextureDescriptor(1920, 1080, TextureFormat.RGBA16F, "TAA.Result"));
            builder.SetColorAttachment(taaOutput, 0);

            taaData.OutputTAA = taaOutput;

            builder.SetRenderFunc<TAAPassData>(static (data, cmd) =>
            {
                cmd.BindShaderResource(data.InputLighting.AsResource(), 0);
                cmd.Draw(3);
            });
        }

        // ===== Post Processing Pass =====
        using (var builder = renderGraph.AddRasterRenderPass<PostProcessingPassDataV2>("Post Processing", out var postData))
        {
            postData.InputTAA = builder.UseTexture(taaOutput, AccessFlags.Read);
            postData.InputSSAO = builder.UseTexture(ssaoOutput, AccessFlags.Read);
            postData.InputBloom = builder.UseTexture(bloomOutput, AccessFlags.Read);

            builder.SetColorAttachment(backbuffer, 0);

            builder.SetRenderFunc<PostProcessingPassDataV2>(static (data, cmd) =>
            {
                cmd.BindShaderResource(data.InputTAA.AsResource(), 0);
                cmd.BindShaderResource(data.InputSSAO.AsResource(), 1);
                cmd.BindShaderResource(data.InputBloom.AsResource(), 2);
                cmd.Draw(3);
            });
        }

        // ===== GPU Profiler Marker Pass (non-cullable, textureless) =====
        using (var builder = renderGraph.AddRasterRenderPass<ProfilerMarkerData>("GPU Profiler Begin Frame", out var profilerData))
        {
            builder.AllowPassCulling(false); // Never cull this - it's for debugging/profiling
            builder.SetRenderFunc<ProfilerMarkerData>(static (data, cmd) =>
            {
                // Note: In a real implementation we would have specific profiler commands
                // For now, since RasterRenderContext doesn't expose generic console write, we skip the print
                // or we would add a specific Profiler method to the context
            });
        }

        // ===== Unused Debug Pass (will be culled) =====
        using (var builder = renderGraph.AddRasterRenderPass<DebugPassData>("Unused Debug Pass", out var debugData))
        {
            builder.SetColorAttachment(
                builder.CreateTexture(new TextureDescriptor(512, 512, TextureFormat.RGBA8, "DebugTexture")), 0);

            builder.SetRenderFunc<DebugPassData>(static (data, cmd) =>
            {
                cmd.Draw(100);
            });
        }

        // Compile and execute the render graph
        renderGraph.Compile();
        renderGraph.Execute();
    }
}