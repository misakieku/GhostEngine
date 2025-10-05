using Ghost.Core;
using Ghost.Graphics.Data;
using Ghost.Graphics.RenderGraphModule;
using System.Numerics;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.Examples;

/// <summary>
/// Example demonstrating render graph usage with history buffers and multiple cameras
/// </summary>
public static class RenderGraphExample
{
    // Example pass data structures
    public struct DepthPrePassData
    {
        public RGTextureHandle DepthBuffer;
        // Add other render targets, constants, etc.
    }

    public struct GBufferPassData
    {
        public RGTextureHandle DepthBuffer;
        public RGTextureHandle AlbedoBuffer;
        public RGTextureHandle NormalBuffer;
        public RGTextureHandle MaterialBuffer;
    }

    public struct LightingPassData
    {
        public RGTextureHandle DepthBuffer;
        public RGTextureHandle AlbedoBuffer;
        public RGTextureHandle NormalBuffer;
        public RGTextureHandle MaterialBuffer;
        public RGTextureHandle SceneColor;
        public RGTextureHandle PreviousFrameColor; // History buffer
    }

    public struct PostProcessPassData
    {
        public RGTextureHandle SceneColor;
        public RGTextureHandle FinalColor;
    }

    /// <summary>
    /// Example camera/view state class to store history buffers (like Unreal's FViewState)
    /// </summary>
    public class CameraViewState
    {
        public Handle<Texture>? PreviousFrameColorBuffer;
        public Handle<Texture>? PreviousFrameDepthBuffer;
        public Handle<Texture>? VelocityBuffer;

        public Matrix4x4 PreviousViewProjectionMatrix;
    }

    public static void ExampleRenderGraph(CameraViewState viewState, Handle<Texture> backBuffer)
    {
        // Create render graph with optional descriptor
        var renderGraphDesc = new RenderGraphDesc(initialResourceCapacity: 512, initialPassCapacity: 128);
        using var renderGraph = new RenderGraph("MainRenderGraph", renderGraphDesc);

        // Begin recording passes
        renderGraph.BeginRecord();

        // Import external resources (backbuffer, history buffers)
        var backBufferHandle = renderGraph.ImportTexture(
            "BackBuffer",
            backBuffer,
            new TextureDescription(1920, 1080, 1, Format.R8G8B8A8Unorm, ResourceFlags.AllowRenderTarget));

        // Import history buffer if available (for temporal effects like TAA)
        RGTextureHandle? previousFrameColorHandle = null;
        if (viewState.PreviousFrameColorBuffer.HasValue)
        {
            previousFrameColorHandle = renderGraph.ImportTexture(
                "PreviousFrameColor",
                viewState.PreviousFrameColorBuffer.Value,
                new TextureDescription(1920, 1080, 1, Format.R8G8B8A8Unorm));
        }

        // Depth Pre-pass
        renderGraph.CreatePass<DepthPrePassData>("DepthPrePass")
            .Setup((ref DepthPrePassData data, RenderPassBuilder builder) =>
            {
                // Create depth buffer as transient resource
                data.DepthBuffer = builder.CreateTexture("DepthBuffer",
                    new TextureDescription(1920, 1080, 1, Format.D32Float, ResourceFlags.AllowDepthStencil));

                // Declare write access to depth buffer
                data.DepthBuffer = builder.WriteTexture(data.DepthBuffer);
            })
            .SetRenderFunc((ref DepthPrePassData data, RenderPassContext ctx) =>
            {
                // Render depth only
                // Use data.DepthBuffer for rendering
            })
            .Compile();

        // G-Buffer Pass
        var gBufferOutputs = renderGraph.CreatePass<GBufferPassData>("GBufferPass")
            .Setup((ref GBufferPassData data, RenderPassBuilder builder) =>
            {
                // Read depth buffer from previous pass
                data.DepthBuffer = builder.ReadTexture(data.DepthBuffer); // This will be resolved during compilation

                // Create G-Buffer targets
                data.AlbedoBuffer = builder.CreateTexture("AlbedoBuffer",
                    new TextureDescription(1920, 1080, 1, Format.R8G8B8A8Unorm, ResourceFlags.AllowRenderTarget));
                data.NormalBuffer = builder.CreateTexture("NormalBuffer",
                    new TextureDescription(1920, 1080, 1, Format.R8G8B8A8Snorm, ResourceFlags.AllowRenderTarget));
                data.MaterialBuffer = builder.CreateTexture("MaterialBuffer",
                    new TextureDescription(1920, 1080, 1, Format.R8G8B8A8Unorm, ResourceFlags.AllowRenderTarget));

                // Declare write access
                data.AlbedoBuffer = builder.WriteTexture(data.AlbedoBuffer);
                data.NormalBuffer = builder.WriteTexture(data.NormalBuffer);
                data.MaterialBuffer = builder.WriteTexture(data.MaterialBuffer);
            })
            .SetRenderFunc((ref GBufferPassData data, RenderPassContext ctx) =>
            {
                // Render geometry to G-Buffer
                // Use data.DepthBuffer, data.AlbedoBuffer, etc.
            });

        gBufferOutputs.Compile();

        // Lighting Pass
        var lightingOutput = renderGraph.CreatePass<LightingPassData>("LightingPass")
            .Setup((ref LightingPassData data, RenderPassBuilder builder) =>
            {
                // Read G-Buffer outputs
                data.DepthBuffer = builder.ReadTexture(data.DepthBuffer);
                data.AlbedoBuffer = builder.ReadTexture(data.AlbedoBuffer);
                data.NormalBuffer = builder.ReadTexture(data.NormalBuffer);
                data.MaterialBuffer = builder.ReadTexture(data.MaterialBuffer);

                // Create scene color output
                data.SceneColor = builder.CreateTexture("SceneColor",
                    new TextureDescription(1920, 1080, 1, Format.R16G16B16A16Float, ResourceFlags.AllowRenderTarget));
                data.SceneColor = builder.WriteTexture(data.SceneColor);

                // Use previous frame color if available (for temporal effects)
                if (previousFrameColorHandle.HasValue)
                {
                    data.PreviousFrameColor = builder.ReadTexture(previousFrameColorHandle.Value);
                }
            })
            .SetRenderFunc((ref LightingPassData data, RenderPassContext ctx) =>
            {
                // Perform deferred lighting
                // Can access previous frame color for temporal anti-aliasing, etc.
            });

        lightingOutput.Compile();

        // Post-Processing Pass
        renderGraph.CreatePass<PostProcessPassData>("PostProcessPass")
            .Setup((ref PostProcessPassData data, RenderPassBuilder builder) =>
            {
                // Read scene color from lighting pass
                data.SceneColor = builder.ReadTexture(data.SceneColor);

                // Write to backbuffer
                data.FinalColor = builder.WriteTexture(backBufferHandle);
            })
            .SetRenderFunc((ref PostProcessPassData data, RenderPassContext ctx) =>
            {
                // Apply tone mapping, gamma correction, etc.
                // Copy to backbuffer
            });

        // End recording
        renderGraph.EndRecord();

        // Compile the render graph (dependency analysis, topological sort, resource lifetime analysis)
        renderGraph.Compile();

        // Export resources for next frame (history buffers)
        if (lightingOutput != null) // In real implementation, you'd track the scene color handle
        {
            // viewState.PreviousFrameColorBuffer = renderGraph.ExportTexture(sceneColorHandle);
        }

        // Execute the render graph
        renderGraph.Execute();
    }

    /// <summary>
    /// Example with multiple cameras rendering to different targets
    /// </summary>
    public static void MultiCameraExample(List<CameraViewState> cameras, List<Handle<Texture>> renderTargets)
    {
        using var renderGraph = new RenderGraph("MultiCameraRenderGraph");
        renderGraph.BeginRecord();

        for (var i = 0; i < cameras.Count; i++)
        {
            var camera = cameras[i];
            var renderTarget = renderTargets[i];
            var cameraName = $"Camera{i}";

            // Import render target
            var renderTargetHandle = renderGraph.ImportTexture(
                $"{cameraName}_RenderTarget",
                renderTarget,
                new TextureDescription(1920, 1080, 1, Format.R8G8B8A8Unorm, ResourceFlags.AllowRenderTarget));

            // Camera-specific rendering passes
            renderGraph.CreatePass<DepthPrePassData>($"{cameraName}_DepthPrePass")
                .Setup((ref DepthPrePassData data, RenderPassBuilder builder) =>
                {
                    data.DepthBuffer = builder.CreateTexture($"{cameraName}_DepthBuffer",
                        new TextureDescription(1920, 1080, 1, Format.D32Float, ResourceFlags.AllowDepthStencil));
                    data.DepthBuffer = builder.WriteTexture(data.DepthBuffer);
                })
                .SetRenderFunc((ref DepthPrePassData data, RenderPassContext ctx) =>
                {
                    // Render with camera[i] view/projection matrices
                })
                .Compile();

            // Additional passes for this camera...
        }

        renderGraph.EndRecord();
        renderGraph.Compile();
        renderGraph.Execute();
    }
}