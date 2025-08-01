# Bindless Rendering in Ghost Engine: A Technical Deep-Dive

This document provides a detailed explanation of the bindless rendering architecture implemented in the Ghost Engine, leveraging modern Direct3D 12 features.

## 1. Introduction to Bindless Rendering

Traditional rendering approaches require the CPU to explicitly bind resources (textures, buffers, etc.) to specific "slots" in the shader pipeline before a draw call. This process, known as "binding," involves creating descriptor tables, setting them on the command list, and managing resource state transitions. While functional, this can lead to significant CPU overhead, complex state management, and a high number of draw calls.

**Bindless rendering** revolutionizes this by moving resource selection from the CPU to the GPU. Instead of binding individual resources, we bind a single, massive descriptor heap containing descriptors for (potentially) all resources. Shaders can then access any resource in this heap using a simple index, which can be passed via a constant buffer or calculated dynamically.

**Key Advantages:**
- **Reduced CPU Overhead:** Eliminates the need for constant re-binding of resources and management of numerous descriptor tables.
- **Simplified Rendering Code:** Drastically simplifies the logic for drawing objects with different materials.
- **Increased GPU Autonomy:** Allows the GPU to fetch required data on its own, leading to more efficient execution.
- **Enables Advanced Techniques:** Opens the door for techniques like "fully bindless" rendering, where even vertex and index data are fetched manually in the vertex shader.

The Ghost Engine implements a state-of-the-art bindless system using DirectX 12's Shader Model 6.6 capabilities.

## 2. Core D3D12 Implementation

The foundation of the engine's bindless architecture rests on a few key D3D12 features.

### 2.1. The Bindless Root Signature

The root signature defines how shaders access resources. For bindless, we use a special flag: `D3D12_ROOT_SIGNATURE_FLAG_CBV_SRV_UAV_HEAP_DIRECTLY_INDEXED`. This tells the GPU that shaders will be able to directly index the entire CBV/SRV/UAV descriptor heap.

Here's how the root signature is created in `Ghost.Graphics\Shading\Shader.cs`:

```csharp
// From: Ghost.Graphics\Shading\Shader.cs

private void CreateBindlessRootSignature()
{
    // ... (parameter setup for constant buffers and samplers)

    // Create root signature with the modern flag
    fixed (RootParameter1* rootParamsPtr = rootParameters)
    {
        var rootSignatureDesc = new RootSignatureDescription1
        {
            // ... (parameters)

            // Key difference: Use the modern flag for direct heap indexing
            Flags = RootSignatureFlags.AllowInputAssemblerInputLayout |
                   RootSignatureFlags.CbvSrvUavHeapDirectlyIndexed
        };

        var versionedDesc = new VersionedRootSignatureDescription
        {
            Version = RootSignatureVersion.V1_1,
            Desc_1_1 = rootSignatureDesc
        };

        // ... (serialization and creation)
    }
}
```

With this flag, the HLSL global `ResourceDescriptorHeap` becomes accessible, representing the entire heap of shader-visible resources.

### 2.2. Resource Descriptors

For a resource to be accessible in a bindless fashion, it needs a Shader Resource View (SRV) created in the global, shader-visible descriptor heap. This is handled by the `Mesh` and `Texture2D` classes.

#### Texture2D Descriptors

When a `Texture2D` is created, it allocates a "bindless descriptor" and creates an SRV for itself at that descriptor's location.

```csharp
// From: Ghost.Graphics\Data\Texture2D.cs

private BindlessDescriptor CreateBindlessDescriptors()
{
    var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;

    // Allocate bindless descriptor from the descriptor allocator
    var bindlessDescriptor = GraphicsPipeline.DescriptorAllocator.AllocateBindless();

    // Create the SRV description
    var srvDesc = new ShaderResourceViewDescription
    {
        Format = Format,
        ViewDimension = SrvDimension.Texture2D,
        Texture2D = new Texture2DSrv { MipLevels = 1 },
        Shader4ComponentMapping = 0x1688 // D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
    };

    // Create the SRV in the bindless heap
    device->CreateShaderResourceView(_resource.NativeResource.Ptr, &srvDesc, bindlessDescriptor.CpuHandle);

    return bindlessDescriptor;
}
```
The `bindlessDescriptor.Index` now holds the unique integer ID that shaders will use to access this texture.

#### Mesh Buffer Descriptors (Fully Bindless)

The engine takes bindless a step further by also making vertex and index buffers available as bindless resources. This enables a "fully bindless" or "meshlet-style" rendering approach where the Vertex Shader manually fetches its own data, bypassing the Input Assembler stage.

The `Mesh` class creates SRVs for its vertex and index buffers, treating them as raw byte-addressable buffers.

```csharp
// From: Ghost.Graphics\Data\Mesh.cs

private void CreateBindlessDescriptors()
{
    // ... (null checks)

    _vertexBufferDescriptor = GraphicsPipeline.DescriptorAllocator.AllocateBindless();
    _indexBufferDescriptor = GraphicsPipeline.DescriptorAllocator.AllocateBindless();

    var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;

    // Create SRV for vertex buffer (as ByteAddressBuffer)
    var vertexSrvDesc = new ShaderResourceViewDescription
    {
        Format = Format.R32Typeless,
        ViewDimension = SrvDimension.Buffer,
        // ...
        Buffer = new()
        {
            // ...
            Flags = BufferSrvFlags.Raw // ByteAddressBuffer
        }
    };
    device->CreateShaderResourceView(_vertexBuffer.NativeResource.Ptr, &vertexSrvDesc, _vertexBufferDescriptor.CpuHandle);

    // Create SRV for index buffer (as ByteAddressBuffer)
    var indexSrvDesc = new ShaderResourceViewDescription
    {
        Format = Format.R32Typeless,
        ViewDimension = SrvDimension.Buffer,
        // ...
        Buffer = new()
        {
            // ...
            Flags = BufferSrvFlags.Raw // ByteAddressBuffer
        }
    };
    device->CreateShaderResourceView(_indexBuffer.NativeResource.Ptr, &indexSrvDesc, _indexBufferDescriptor.CpuHandle);
}
```

## 3. HLSL Shader Implementation

With the D3D12 backend in place, accessing resources in HLSL becomes remarkably simple. The `MeshRenderPass.cs` file contains a perfect example of a fully bindless shader.

### 3.1. The Constant Buffer

First, we define a constant buffer to pass the resource indices from the CPU to the GPU.

```hlsl
// From: Ghost.Graphics\RenderPasses\MeshRenderPass.cs

cbuffer ConstantBuffer : register(b0)
{
    float4 _Color;
    uint _TextureIndex1;
    uint _TextureIndex2;
    uint _TextureIndex3;
    uint _TextureIndex4;
    uint _VertexBufferIndex;
    uint _IndexBufferIndex;
};
```

### 3.2. Vertex Shader: Manual Vertex Fetching

The vertex shader (`VSMain`) demonstrates the power of fully bindless rendering. Instead of receiving pre-fetched vertex attributes, it receives only a `vertexId` and `instanceId`. It uses the `_VertexBufferIndex` and `_IndexBufferIndex` to access the correct buffers from the global `ResourceDescriptorHeap` and manually loads the vertex data.

```hlsl
// From: Ghost.Graphics\RenderPasses\MeshRenderPass.cs

PixelInput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    // Get bindless buffers from the global heap
    ByteAddressBuffer vertexBuffer = ResourceDescriptorHeap[_VertexBufferIndex];
    ByteAddressBuffer indexBuffer = ResourceDescriptorHeap[_IndexBufferIndex];
    
    // Each instance represents one triangle
    // vertexId is 0, 1, or 2
    
    // Calculate index into the index buffer to find the vertex index
    uint indexOffset = (instanceId * 3 + vertexId) * 4; // 4 bytes per index
    uint vertexIndex = indexBuffer.Load(indexOffset);
    
    // Calculate offset into the vertex buffer using the vertex index
    uint vertexOffset = vertexIndex * 80; // 80 bytes per vertex (5 * float4)
    
    // Load vertex data from the bindless vertex buffer
    Vertex vertex;
    vertex.position = asfloat(vertexBuffer.Load4(vertexOffset + 0));
    vertex.normal   = asfloat(vertexBuffer.Load4(vertexOffset + 16));
    // ... etc.
    
    // ...
    return output;
}
```

### 3.3. Pixel Shader: Bindless Texture Sampling

The pixel shader (`PSMain`) uses the same principle to access textures. It uses the `_TextureIndexN` values to grab the correct `Texture2D` from the `ResourceDescriptorHeap` and samples them.

```hlsl
// From: Ghost.Graphics\RenderPasses\MeshRenderPass.cs

float4 PSMain(PixelInput input) : SV_TARGET
{
    // Access textures directly from the heap using their indices
    Texture2D tex1 = ResourceDescriptorHeap[_TextureIndex1];
    Texture2D tex2 = ResourceDescriptorHeap[_TextureIndex2];
    Texture2D tex3 = ResourceDescriptorHeap[_TextureIndex3];
    Texture2D tex4 = ResourceDescriptorHeap[_TextureIndex4];
    
    // Sample the textures
    float4 color1 = tex1.Sample(_MainSampler, input.uv.xy);
    // ... etc.
    
    // Blend all textures together
    float4 blendedColor = (color1 + color2 + color3 + color4) * 0.25f;
    
    return blendedColor * _Color;
}
```

## 4. C# Usage Example

The C# side of the implementation is elegant and straightforward, abstracting the low-level details. The `MeshRenderPass` class demonstrates a typical setup.

### 4.1. Initialization

During initialization, we create the mesh and textures. The `UploadMeshData` and `UploadTextureData` calls ensure the data is on the GPU, and the underlying `Mesh` and `Texture2D` constructors have already created the necessary bindless SRVs.

We then create a `Material` and use it to bridge the gap between the C# objects and the shader's constant buffer properties.

```csharp
// From: Ghost.Graphics\RenderPasses\MeshRenderPass.cs

public void Initialize(CommandList cmd)
{
    _mesh = MeshBuilder.CreateCube(0.75f);
    _mesh.UploadMeshData();

    _shader = new Shader(_HLSL_SOURCE);
    _material = new Material(_shader);

    // Load textures
    _textures = new Texture2D[_textureFiles.Length];
    for (var i = 0; i < _textureFiles.Length; i++)
    {
        _textures[i] = Texture2D.FromFile(_textureFiles[i]);
        _textures[i].UploadTextureData();
    }

    // Set material properties, passing the descriptor indices to the shader
    _material.SetVector("_Color", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
    for (var i = 0; i < _textures.Length; i++)
    {
        var texture = _textures[i];
        // This sets the uint property in the CBuffer
        _material.SetTextureIndex($"_TextureIndex{i + 1}", texture);
    }

    // This is a helper on Material that sets _VertexBufferIndex and _IndexBufferIndex
    // It is not used in the provided code, but it's the intended way.
    // For this example, the indices are set implicitly by the DrawMeshBindless call.
    // material.SetMeshBufferIndices(mesh);

    // Uploads the constant buffer data to the GPU
    _material.UploadMaterialData();
}
```
*Note: The `Material.SetTextureIndex` method is a convenient wrapper that calls `SetUInt` internally, passing the `texture.DescriptorIndex`.*

### 4.2. Execution

Executing the render pass is now incredibly simple. The `DrawMeshBindless` command takes the mesh and material, binds the necessary state (Root Signature, PSO, and the material's constant buffer), and issues a single `DrawInstanced` call.

```csharp
// From: Ghost.Graphics\RenderPasses\MeshRenderPass.cs

public void Execute(CommandList cmd)
{
    // This single call handles everything!
    cmd.DrawMeshBindless(_mesh!, _material!);
}
```

The `DrawMeshBindless` method (in `CommandList.cs`) is where the magic happens. It doesn't need to set vertex or index buffers on the Input Assembler. It just needs to know the number of triangles to draw.

```csharp
// From: Ghost.Graphics\D3D12\CommandList.cs (Conceptual)

public void DrawMeshBindless(Mesh mesh, Material material)
{
    // 1. Bind the material (sets PSO, Root Signature, CBuffers, Samplers)
    material.Bind(this);

    // 2. Set the mesh buffer indices on the material
    // This is the crucial step that connects the mesh to the shader
    material.SetMeshBufferIndices(mesh);
    material.UploadMaterialData(); // Re-upload CBuffer with mesh indices

    // 3. Draw instanced.
    // - VertexCountPerInstance = 3 (for a triangle)
    // - InstanceCount = total number of triangles in the mesh
    // - StartVertexLocation = 0
    // - StartInstanceLocation = 0
    uint triangleCount = mesh.IndexCount / 3;
    _commandList.Get()->DrawInstanced(3, triangleCount, 0, 0);
}
```

## 5. Conclusion

The bindless architecture in the Ghost Engine is a powerful, modern, and efficient way to handle rendering. By leveraging Shader Model 6.6, it significantly reduces CPU overhead and simplifies rendering logic, paving the way for more complex and dynamic scenes. The "fully bindless" approach for mesh data further enhances this paradigm, offering maximum flexibility and performance on the GPU.
