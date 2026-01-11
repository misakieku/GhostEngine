# GhostEngine Render Graph

A high-performance, transient render graph implementation for GhostEngine, inspired by Unity, Unreal, and other AAA engines.

## Features

✅ **Transient Architecture** - Graph rebuilds every frame for maximum flexibility  
✅ **Minimal GC** - Only ~571 bytes allocated per frame (after warmup)  
✅ **Automatic Pass Culling** - Unused passes are automatically removed  
✅ **Resource Tracking** - Automatic resource lifetime management  
✅ **Blackboard Pattern** - Share data between passes easily  
✅ **Async Compute Support** - Mark passes for async execution  
✅ **Type-Safe API** - Strongly-typed pass data

## Performance

```
Per iteration time: 2,292 ns (~2.3 microseconds)
GC allocated: 571 bytes per iteration (after warmup)
```

Tested with a complex graph containing:
- 13 render passes (GBuffer, Lighting, SSAO, Bloom, TAA, Post-processing)
- 15+ transient textures
- Multiple read/write dependencies
- Blackboard data sharing
- Pass culling optimization

## Quick Start

```csharp
var renderGraph = new RenderGraph();

// Each frame:
renderGraph.Reset();

// Import external resources
var backbuffer = renderGraph.ImportTexture("Backbuffer",
    new TextureDescriptor(1920, 1080, TextureFormat.RGBA8, "Backbuffer"));

// Add a pass
GBufferData gbufferData;
using (var builder = renderGraph.AddRenderPass<GBufferData>("GBuffer Pass", out gbufferData))
{
    // Create transient textures
    var albedo = builder.CreateTexture(
        new TextureDescriptor(1920, 1080, TextureFormat.RGBA8, "GBuffer.Albedo"));
    
    // Mark resource usage
    gbufferData.Albedo = builder.WriteTexture(albedo);
    
    // Set the render function
    builder.SetRenderFunc<GBufferData>((data, cmd) =>
    {
        cmd.SetRenderTarget(data.Albedo.Name);
        cmd.Draw(36000);
    });
}

// Share data with other passes
renderGraph.Blackboard.Add(gbufferData);

// Read from blackboard in another pass
using (var builder = renderGraph.AddRenderPass<LightingData>("Lighting", out var lightingData))
{
    var gbuffer = renderGraph.Blackboard.Get<GBufferData>();
    lightingData.Albedo = builder.ReadTexture(gbuffer.Albedo);
    // ... rest of pass setup
}

// Compile and execute
renderGraph.Compile();
renderGraph.Execute();
```

## API Reference

### RenderGraph

**`void Reset()`**  
Clears the graph for a new frame. Reuses allocations to minimize GC.

**`RenderGraphTextureHandle ImportTexture(string name, TextureDescriptor desc)`**  
Imports an external texture (like the backbuffer) into the graph.

**`RenderGraphBuilder AddRenderPass<TPassData>(string name, out TPassData data)`**  
Adds a new render pass. Returns a builder for configuring the pass.

**`void Compile()`**  
Analyzes dependencies and culls unused passes.

**`void Execute()`**  
Executes all compiled (non-culled) passes.

**`RenderGraphBlackboard Blackboard { get; }`**  
Access the blackboard for sharing data between passes.

### RenderGraphBuilder

**`RenderGraphTextureHandle CreateTexture(TextureDescriptor desc)`**  
Creates a transient texture that only lives during this pass.

**`RenderGraphTextureHandle ReadTexture(RenderGraphTextureHandle handle)`**  
Marks a texture as read by this pass.

**`RenderGraphTextureHandle WriteTexture(RenderGraphTextureHandle handle)`**  
Marks a texture as written by this pass.

**`RenderGraphTextureHandle UseDepthBuffer(RenderGraphTextureHandle handle, bool writeAccess)`**  
Sets up depth buffer usage for this pass.

**`void SetRenderFunc<TPassData>(Action<TPassData, RasterRenderContext> func)`**  
Sets the raster render function for this pass.

**`void SetComputeFunc<TPassData>(Action<TPassData, ComputeRenderContext> func, bool asyncCompute = false)`**  
Sets the compute function for this pass. Optionally mark as async.

**`void SetAllowCulling(bool allow)`**  
Controls whether this pass can be culled if its outputs are unused.

### RenderGraphBlackboard

**`void Add<T>(T data) where T : class, IPassData`**  
Stores pass data in the blackboard.

**`T Get<T>() where T : class, IPassData`**  
Retrieves pass data from the blackboard.

**`bool TryGet<T>(out T data) where T : class, IPassData`**  
Attempts to retrieve pass data from the blackboard.

## Architecture

The render graph uses several key patterns to achieve minimal GC:

1. **Object Pooling** - All passes and data structures are pooled and reused
2. **Collection Reuse** - Lists are cleared instead of reallocated
3. **Pre-allocation** - Capacity is pre-allocated based on expected usage
4. **Avoid LINQ** - Explicit loops instead of LINQ for zero allocation
5. **Struct Handles** - Resource handles are lightweight value types

### Pass Culling

The graph automatically removes unused passes:

1. Passes that write to imported resources have side effects (never culled)
2. All other passes are initially marked as culled
3. Dependencies of non-culled passes are recursively un-culled
4. Only passes contributing to the final output remain

This means you can freely add debug/visualization passes - they'll be automatically removed if unused.

## Building

```bash
dotnet build Ghost.RenderGraph.Concept/Ghost.RenderGraph.Concept.csproj -c Release
```

## Running Tests

```bash
dotnet run --project Ghost.RenderGraph.Concept/Ghost.RenderGraph.Concept.csproj -c Release
```

This runs 500 warmup iterations, then measures 500 more to determine average performance.

## Implementation Notes

See [IMPLEMENTATION_NOTES.md](IMPLEMENTATION_NOTES.md) for detailed architecture documentation.

## Limitations

- **Single-threaded** - Not thread-safe, designed for render thread only
- **No GPU resource pooling** - Currently uses mock command buffers
- **No render pass merging** - Compatible passes could be merged for better performance on tile-based GPUs
- **No resource aliasing** - Could reuse memory for non-overlapping resource lifetimes

## Future Enhancements

- [ ] Resource aliasing for memory efficiency
- [ ] Native render pass merging (for tile-based GPUs)
- [ ] GPU resource pooling
- [ ] Async/await support in render functions
- [ ] Memory budgeting and OOM protection
- [ ] Debug visualization (like Unity's Render Graph Viewer)
- [ ] Multi-threaded pass recording
- [ ] Graph caching across similar frames

## License

Part of GhostEngine. See repository root for license information.

## References

- Unity Render Graph: https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest
- Unreal RDG: https://docs.unrealengine.com/5.0/en-US/render-dependency-graph-in-unreal-engine/
- Frostbite Frame Graph: https://www.gdcvault.com/play/1024612/FrameGraph-Extensible-Rendering-Architecture-in
