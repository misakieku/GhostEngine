# Render Graph API Design Summary

## Overview

This render graph implementation uses a **production-grade API design** inspired by Unity HDRP's render graph, focusing on performance and usability.

## Core Design Principles

### 1. **Typed Pass Data > String Lookups**

❌ **Anti-pattern** (slow, error-prone):
```csharp
builder.SetRenderFunc(cmd => {
    cmd.SetRenderTarget("GBuffer.Albedo");  // String lookup!
});
```

✅ **Best practice** (fast, type-safe):
```csharp
builder.SetRenderFunc((data, cmd) => {
    cmd.SetRenderTarget(data.Albedo.Name);  // Direct field access!
});
```

### 2. **Blackboard for Complex Data**

When multiple passes need the same resources (like GBuffer with albedo, normal, depth):

```csharp
class GBufferData {
    public RenderGraphTextureHandle Albedo = null!;
    public RenderGraphTextureHandle Normal = null!;
    public RenderGraphTextureHandle Depth = null!;
}

// Producer pass
using (var builder = renderGraph.AddRenderPass<GBufferData>("GBuffer", out var data)) {
    data.Albedo = builder.WriteTexture(builder.CreateTexture(...));
    data.Normal = builder.WriteTexture(builder.CreateTexture(...));
    data.Depth = builder.UseDepthBuffer(builder.CreateTexture(...), true);
    builder.SetRenderFunc((d, cmd) => { /* use d.Albedo, d.Normal, d.Depth */ });
}
renderGraph.Blackboard.Add(gbufferData);

// Consumer passes
using (var builder = renderGraph.AddRenderPass<LightingData>("Lighting", out var data)) {
    var gbuffer = renderGraph.Blackboard.Get<GBufferData>();
    data.Albedo = builder.ReadTexture(gbuffer.Albedo);
    data.Normal = builder.ReadTexture(gbuffer.Normal);
    // ...
}
```

### 3. **Direct Handle Passing for Simple Cases**

When passing a single texture between two passes:

```csharp
// Pass 1: Return handle
RenderGraphTextureHandle lightingOutput;
using (var builder = renderGraph.AddRenderPass<LightingData>("Lighting", out var data)) {
    lightingOutput = builder.CreateTexture(...);
    data.Output = builder.WriteTexture(lightingOutput);
    builder.SetRenderFunc((d, cmd) => { /* ... */ });
}

// Pass 2: Use handle directly
using (var builder = renderGraph.AddRenderPass<TAAData>("TAA", out var data)) {
    data.Input = builder.ReadTexture(lightingOutput);  // Direct pass!
    builder.SetRenderFunc((d, cmd) => { /* ... */ });
}
```

## Performance Benefits

| Aspect | Traditional String-Based | Typed Pass Data |
|--------|-------------------------|-----------------|
| **Resource Access** | Dictionary lookup | Direct field access |
| **Type Safety** | Runtime errors | Compile-time checks |
| **Refactoring** | Find & Replace | Compiler-assisted |
| **IDE Support** | Limited | Full IntelliSense |
| **Performance** | Hash lookup overhead | Zero overhead |

## Real-World Example

Here's how a complete deferred rendering pipeline looks:

```csharp
// 1. GBuffer Pass - produce multiple outputs
GBufferData gbufferData;
using (var builder = renderGraph.AddRenderPass<GBufferData>("GBuffer", out gbufferData)) {
    gbufferData.Albedo = builder.WriteTexture(builder.CreateTexture(...));
    gbufferData.Normal = builder.WriteTexture(builder.CreateTexture(...));
    gbufferData.Depth = builder.UseDepthBuffer(builder.CreateTexture(...), true);
    builder.SetRenderFunc((data, cmd) => { /* render geometry */ });
}
renderGraph.Blackboard.Add(gbufferData);

// 2. Lighting Pass - consume GBuffer, produce lighting
RenderGraphTextureHandle lightingOutput;
using (var builder = renderGraph.AddRenderPass<LightingData>("Lighting", out var data)) {
    var gbuffer = renderGraph.Blackboard.Get<GBufferData>();
    data.GBufferAlbedo = builder.ReadTexture(gbuffer.Albedo);
    data.GBufferNormal = builder.ReadTexture(gbuffer.Normal);
    data.GBufferDepth = builder.ReadTexture(gbuffer.Depth);
    
    lightingOutput = builder.CreateTexture(...);
    data.Output = builder.WriteTexture(lightingOutput);
    builder.SetRenderFunc((data, cmd) => { /* deferred lighting */ });
}

// 3. SSAO Pass - also consume GBuffer
RenderGraphTextureHandle ssaoOutput;
using (var builder = renderGraph.AddRenderPass<SSAOData>("SSAO", out var data)) {
    var gbuffer = renderGraph.Blackboard.Get<GBufferData>();
    data.Depth = builder.ReadTexture(gbuffer.Depth);
    data.Normal = builder.ReadTexture(gbuffer.Normal);
    
    ssaoOutput = builder.CreateTexture(...);
    data.Output = builder.WriteTexture(ssaoOutput);
    builder.SetRenderFunc((data, cmd) => { /* SSAO */ });
}

// 4. Post Processing - combine lighting + SSAO
using (var builder = renderGraph.AddRenderPass<PostData>("Post", out var data)) {
    data.Lighting = builder.ReadTexture(lightingOutput);  // Direct handle
    data.SSAO = builder.ReadTexture(ssaoOutput);          // Direct handle
    data.Output = builder.WriteTexture(backbuffer);
    builder.SetRenderFunc((data, cmd) => { /* combine */ });
}

renderGraph.Compile();
renderGraph.Execute();
```

## When to Use What?

| Scenario | Use | Example |
|----------|-----|---------|
| Multiple outputs used by many passes | **Blackboard** | GBuffer (albedo, normal, depth) |
| Single texture passed to next pass | **Direct Handle** | Lighting → TAA |
| Temporary working data | **Pass Data** | Intermediate blur textures |
| Persistent frame data | **Import** | Backbuffer, history textures |

## Comparison with Other Systems

### Unity HDRP
```csharp
// Unity's API (very similar!)
using (var builder = renderGraph.AddRenderPass<PassData>("MyPass", out var passData))
{
    passData.input = builder.ReadTexture(inputHandle);
    passData.output = builder.WriteTexture(outputHandle);
    builder.SetRenderFunc((data, ctx) => { /* ... */ });
}
```

### Unreal Engine 5 RDG
```cpp
// Unreal's API (similar concepts, different syntax)
FRDGTextureRef Output = GraphBuilder.CreateTexture(Desc, TEXT("Output"));
AddPass(GraphBuilder, "MyPass", Parameters,
    [Parameters](FRHICommandList& RHICmdList) {
        // Execute
    });
```

### Frostbite
```cpp
// Frostbite (original frame graph paper)
FrameGraphTextureHandle output = frameGraph.create("Output", desc);
frameGraph.addPass("MyPass",
    [&](FrameGraphBuilder& builder) {
        builder.write(output);
    },
    [=](const Resources& resources, CommandBuffer& cmd) {
        // Execute
    });
```

## Conclusion

This API design prioritizes:
1. **Performance**: Zero-cost abstractions with direct field access
2. **Safety**: Compile-time type checking
3. **Ergonomics**: Natural C# patterns (using blocks, typed data)
4. **Flexibility**: Blackboard for complex data, handles for simple cases

It matches industry-standard patterns from Unity, Unreal, and Frostbite while leveraging C#'s type system for maximum safety and performance.
