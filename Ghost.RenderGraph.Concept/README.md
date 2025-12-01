# Transient Render Graph - Proof of Concept

This is a high-level proof of concept implementation of a modern transient render graph system, inspired by:
- **Unreal Engine 5 RDG** (Render Dependency Graph)
- **Frostbite Frame Graph**
- **Unity HDRP Render Graph**

## Key Features

### 1. **Resource Virtualization**
Resources are declared during setup but not physically created until execution. This allows the graph to analyze the entire frame before committing to resource allocation.

### 2. **Automatic Resource Lifetime Management**
- Resources are created only when first needed (at their `FirstUse` pass)
- Resources are destroyed immediately after their last use (at their `LastUse` pass)
- Imported resources (like backbuffer) are never destroyed by the graph

### 3. **Automatic Barrier Insertion**
The graph automatically inserts resource state transitions based on how resources are used:
- Write operations: `RenderTarget`, `DepthWrite`, `UnorderedAccess`, `CopyDest`
- Read operations: `ShaderResource`, `DepthRead`, `CopySource`

### 4. **Automatic Pass Dependencies**
Dependencies are automatically inferred from resource usage patterns:
- **Read-After-Write (RAW)**: Pass B reads what Pass A wrote
- **Write-After-Read (WAR)**: Pass B writes to what Pass A read
- **Write-After-Write (WAW)**: Pass B writes to what Pass A wrote

### 5. **Pass Culling**
Passes that don't contribute to the final output are automatically culled:
- Starts from imported resources (outputs)
- Recursively marks all dependent passes as needed
- Unused passes are not executed

## Architecture

### Core Classes

#### `RenderGraph`
The main orchestrator that manages the entire frame graph:
- **Setup Phase**: Declare resources and passes
- **Compile Phase**: Build dependencies, cull passes, analyze lifetimes
- **Execute Phase**: Create resources, insert barriers, execute passes, destroy resources

#### `RenderGraphResourceHandle`
Handle representing a virtual resource:
- `RenderGraphTextureHandle`: Textures with format, size
- `RenderGraphBufferHandle`: Buffers with size

#### `IRenderGraphBuilder`
Builder interface used during pass setup:
- `ReadTexture()` / `ReadBuffer()`: Declare read access
- `WriteTexture()` / `WriteBuffer()`: Declare write access
- `CreateTransientTexture()` / `CreateTransientBuffer()`: Create temporary resources

#### `ICommandBuffer`
Abstraction for executing graphics commands (simulated with `Console.WriteLine`)

### Resource Lifetime Example

```
GBuffer.Albedo:  [0..1]  Created in pass 0, destroyed after pass 1
GBuffer.Normal:  [0..2]  Created in pass 0, destroyed after pass 2
GBuffer.Depth:   [0..2]  Created in pass 0, destroyed after pass 2
LightingResult:  [1..3]  Created in pass 1, destroyed after pass 3
SSAO:            [2..4]  Created in pass 2, destroyed after pass 4
TAA.Result:      [3..4]  Created in pass 3, destroyed after pass 4
Backbuffer:      [4..4]  Imported (never created/destroyed)
```

## Usage Example

### Pattern 1: Using Typed Pass Data with Blackboard (Recommended)

```csharp
var renderGraph = new RenderGraph();

// Import external resources (e.g., backbuffer)
var backbuffer = renderGraph.ImportTexture(
    "Backbuffer", 
    new TextureDescriptor(1920, 1080, TextureFormat.RGBA8));

// Define pass data structure
class GBufferData
{
    public RenderGraphTextureHandle Albedo = null!;
    public RenderGraphTextureHandle Normal = null!;
    public RenderGraphTextureHandle Depth = null!;
}

// Create GBuffer pass with typed data
GBufferData gbufferData;
using (var builder = renderGraph.AddRenderPass<GBufferData>("GBuffer Pass", out gbufferData))
{
    // Create transient resources
    var albedo = builder.CreateTexture(
        new TextureDescriptor(1920, 1080, TextureFormat.RGBA8, "GBuffer.Albedo"));
    var normal = builder.CreateTexture(
        new TextureDescriptor(1920, 1080, TextureFormat.RGBA16F, "GBuffer.Normal"));
    var depth = builder.CreateTexture(
        new TextureDescriptor(1920, 1080, TextureFormat.Depth32F, "GBuffer.Depth"));
    
    // Store in pass data and declare access
    gbufferData.Albedo = builder.WriteTexture(albedo);
    gbufferData.Normal = builder.WriteTexture(normal);
    gbufferData.Depth = builder.UseDepthBuffer(depth, writeAccess: true);
    
    // Set render function with typed data
    builder.SetRenderFunc((data, cmd) =>
    {
        cmd.SetRenderTarget(data.Albedo.Name);
        cmd.SetRenderTarget(data.Normal.Name);
        cmd.SetDepthStencil(data.Depth.Name);
        cmd.Draw(36000);
    });
}

// Store in blackboard for other passes
renderGraph.Blackboard.Add(gbufferData);

// ===== Lighting Pass =====
class LightingPassData
{
    public RenderGraphTextureHandle GBufferAlbedo = null!;
    public RenderGraphTextureHandle GBufferNormal = null!;
    public RenderGraphTextureHandle OutputLighting = null!;
}

RenderGraphTextureHandle lightingOutput;
using (var builder = renderGraph.AddRenderPass<LightingPassData>("Lighting Pass", out var lightingData))
{
    // Read GBuffer from blackboard
    var gbuffer = renderGraph.Blackboard.Get<GBufferData>();
    
    lightingData.GBufferAlbedo = builder.ReadTexture(gbuffer.Albedo);
    lightingData.GBufferNormal = builder.ReadTexture(gbuffer.Normal);
    
    // Create and return output handle
    lightingOutput = builder.CreateTexture(
        new TextureDescriptor(1920, 1080, TextureFormat.RGBA16F, "Lighting"));
    lightingData.OutputLighting = builder.WriteTexture(lightingOutput);
    
    builder.SetRenderFunc((data, cmd) =>
    {
        cmd.BindShaderResource(data.GBufferAlbedo.Name, 0);
        cmd.BindShaderResource(data.GBufferNormal.Name, 1);
        cmd.SetRenderTarget(data.OutputLighting.Name);
        cmd.Draw(3);
    });
}

// Compile and execute
renderGraph.Compile();
renderGraph.Execute();
```

### Pattern 2: Simple Handle Passing

For simple cases where you just need to pass one or two textures between passes, you can skip the blackboard:

```csharp
// Create and return a handle
RenderGraphTextureHandle myTexture;
using (var builder = renderGraph.AddRenderPass<MyPassData>("Pass 1", out var data))
{
    myTexture = builder.CreateTexture(new TextureDescriptor(...));
    data.Output = builder.WriteTexture(myTexture);
    builder.SetRenderFunc((d, cmd) => { /* ... */ });
}

// Use the handle in next pass
using (var builder = renderGraph.AddRenderPass<NextPassData>("Pass 2", out var data))
{
    data.Input = builder.ReadTexture(myTexture);
    builder.SetRenderFunc((d, cmd) => { /* ... */ });
}
```

## Key API Design Features

### 1. **Typed Pass Data**
Each pass has a strongly-typed data structure that holds all its resource handles. This:
- Makes resource dependencies explicit and compile-time safe
- Avoids string-based lookups during execution
- Enables better IDE support and refactoring

### 2. **Blackboard Pattern**
For sharing data structures between multiple passes:
- Store complex data (like GBuffer with multiple textures) in the blackboard
- Other passes retrieve it type-safely
- Useful for resources used by many passes

### 3. **Direct Handle Passing**
For simple cases:
- Return a `RenderGraphTextureHandle` from a pass setup
- Pass it directly to the next pass
- No need for blackboard overhead

### 4. **Using Block Pattern**
The `using` statement automatically commits the pass when the block ends:
- Builder is disposed → pass is committed to the graph
- Ensures all passes are properly registered
- Mimics Unity HDRP's render graph API

## Benefits of Transient Resources

1. **Memory Efficiency**: Resources only exist when needed, allowing memory reuse
2. **Automatic Synchronization**: Barriers inserted automatically based on usage
3. **Self-Documenting**: Clear declaration of what each pass reads/writes
4. **Type Safety**: Compile-time checking of pass data structures
5. **Performance**: No string lookups or dictionary access during execution
6. **Optimization Opportunities**: Graph can reorder passes (future work)
7. **Resource Aliasing**: Multiple transient resources can share memory (future work)

## What's NOT Implemented (Intentionally)

This is a proof of concept focusing on core graph mechanics. Some features are fully implemented, others are intentionally omitted:

### ✅ Fully Implemented
- ✅ **Resource aliasing/memory pooling** - Automatic memory reuse for non-overlapping lifetimes
- ✅ **Typed pass data** - Zero-cost abstraction with compile-time safety
- ✅ **Blackboard pattern** - Type-safe data sharing between passes
- ✅ **Automatic barriers** - State transitions inferred from usage

### ❌ Not Implemented
- ❌ Async compute queues
- ❌ Pass reordering optimization
- ❌ Subresource tracking (mip levels, array slices)
- ❌ Multi-queue synchronization
- ❌ GPU timeline profiling
- ❌ Resource versioning
- ❌ Graph visualization/debugging tools

## Resource Aliasing (Implemented!)

Transient resources with non-overlapping lifetimes automatically share the same physical memory:
```
GBuffer.Albedo [0..1]  ━━━━━━━━━━━
                                   ╰──> Reuse memory (34% savings!)
SSAO           [2..4]              ━━━━━━━━━━━━━
```

Example output:
```
[RG] Memory Statistics:
     Total memory without aliasing: 72.19 MB
     Total memory with aliasing: 47.46 MB
     Memory saved: 24.73 MB (34.3%)
     Allocations: 4 physical allocations for 7 resources
```

See [ALIASING.md](ALIASING.md) for detailed documentation.

## Future Enhancements

### Async Compute
Some passes can run on compute queue while graphics queue continues:
```
Graphics: ━━[GBuffer]━━[Lighting]━━━━━━━━[PostFX]━━
                          ║
Compute:                  ╚═[SSAO]════╗
                                       ║
                          [Wait]───────╝
```

### Pass Reordering
Independent passes can be reordered for better GPU utilization or to enable more aliasing.

## Output Example

The demo program creates a deferred rendering pipeline and produces output like:

```
[RG] Building pass dependencies...
     Pass 'Lighting Pass' depends on 'GBuffer Pass'
     Pass 'SSAO Pass' depends on 'GBuffer Pass'
     Pass 'Temporal AA' depends on 'Lighting Pass'
     Pass 'Post Processing' depends on 'TAA' and 'SSAO'

[RG] Culling unused passes...
     Culled unused pass: 'Unused Debug Pass'

[RG] Resource lifetimes:
     'GBuffer.Albedo': [0..1] (GBuffer Pass, Lighting Pass)
     'GBuffer.Normal': [0..2] (GBuffer Pass, Lighting Pass, SSAO Pass)
     ...

[PASS 0] Executing: 'GBuffer Pass'
  [CREATE] Texture 'GBuffer.Albedo' (1920x1080, RGBA8)
  [BARRIER] Transition 'GBuffer.Albedo' from Undefined to RenderTarget
  [BEGIN] RenderPass 'GBuffer Pass'
    [RT] Set RenderTarget: 'GBuffer.Albedo'
    [DRAW] Drawing 36000 vertices
  [END] RenderPass

[PASS 1] Executing: 'Lighting Pass'
  [BARRIER] Transition 'GBuffer.Albedo' from RenderTarget to ShaderResource
  ...
  [DESTROY] Resource 'GBuffer.Albedo'
```

## Conclusion

This proof of concept demonstrates the core principles of modern transient render graphs. The system automatically manages resource lifetimes, inserts synchronization barriers, builds dependency DAGs, and culls unused work—all from high-level declarative pass descriptions.

The architecture is designed to be extended with real graphics API integration (D3D12, Vulkan) while maintaining the same high-level interface.
