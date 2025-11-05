using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;

namespace Ghost.Graphics.Core;

public unsafe readonly ref struct RenderingContext
{
    private readonly IRenderDevice _device;
    private readonly ICommandBuffer _directCmb;
    private readonly ICommandBuffer _copyCmb;
    private readonly ICommandBuffer _computeCmb;
    private readonly IResourceAllocator _resourceAllocator;
    private readonly IResourceDatabase _resourceDatabase;
    private readonly IPipelineLibrary _stateController;

    internal RenderingContext(
        IRenderDevice device,
        ICommandBuffer directCmd,
        ICommandBuffer copyCmd,
        ICommandBuffer computeCmd,
        IResourceAllocator resourceAllocator,
        IResourceDatabase resourceDatabase,
        IPipelineLibrary stateController)
    {
        _device = device;
        _directCmb = directCmd;
        _copyCmb = copyCmd;
        _computeCmb = computeCmd;
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;
        _stateController = stateController;
    }

    public Handle<Mesh> CreateMesh(UnsafeList<Vertex> vertices, UnsafeList<uint> indices)
    {
        var mesh = _resourceAllocator.CreateMesh(vertices, indices);
        return mesh;
    }

    public Handle<Mesh> CreateMesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices)
    {
        var vertexList = new UnsafeList<Vertex>(vertices.Length, Allocator.Persistent);
        var indexList = new UnsafeList<uint>(indices.Length, Allocator.Persistent);

        vertexList.CopyFrom(vertices);
        indexList.CopyFrom(indices);

        return CreateMesh(vertexList, indexList);
    }

    // TODO: Make one memory pool for upload.

    /// <summary>
    /// Uploads the mesh data to the GPU.
    /// </summary>
    /// <param name="mesh">The handle point to the mesh buffer</param>
    /// <param name="markMeshStatic">Whether to mark the mesh as static. If it's true, the cpu buffer of the mesh will not be avaliable any more</param>
    public void UploadMesh(Handle<Mesh> mesh, bool markMeshStatic)
    {
        ref var meshData = ref _resourceDatabase.GetMeshReference(mesh);
        var vertexState = _resourceDatabase.GetResourceState(meshData.vertexBuffer.AsResource());
        var indexState = _resourceDatabase.GetResourceState(meshData.indexBuffer.AsResource());
        var needVertexTransition = vertexState != ResourceState.CopyDest;
        var needIndexTransition = indexState != ResourceState.CopyDest;

        if (needVertexTransition)
        {
            _copyCmb.ResourceBarrier(meshData.vertexBuffer.AsResource(), vertexState, ResourceState.CopyDest);
            _resourceDatabase.SetResourceState(meshData.vertexBuffer.AsResource(), ResourceState.CopyDest);
        }

        if (needIndexTransition)
        {
            _copyCmb.ResourceBarrier(meshData.indexBuffer.AsResource(), indexState, ResourceState.CopyDest);
            _resourceDatabase.SetResourceState(meshData.indexBuffer.AsResource(), ResourceState.CopyDest);
        }

        _copyCmb.Upload(meshData.vertexBuffer, meshData.vertices.AsSpan());
        _copyCmb.Upload(meshData.indexBuffer, meshData.indices.AsSpan());

        if (needVertexTransition)
        {
            _copyCmb.ResourceBarrier(meshData.vertexBuffer.AsResource(), ResourceState.CopyDest, vertexState);
            _resourceDatabase.SetResourceState(meshData.vertexBuffer.AsResource(), vertexState);
        }

        if (needIndexTransition)
        {
            _resourceDatabase.SetResourceState(meshData.indexBuffer.AsResource(), indexState);
            _copyCmb.ResourceBarrier(meshData.indexBuffer.AsResource(), ResourceState.CopyDest, indexState);
        }

        if (markMeshStatic)
        {
            meshData.ReleaseCpuResources();
        }
    }

    public Handle<Texture> CreateTexture(ref readonly TextureDesc desc, bool tempResource = false)
    {
        return _resourceAllocator.CreateTexture(in desc, tempResource);
    }

    public void UploadTexture(Handle<Texture> texture, ReadOnlySpan<byte> data)
    {
        var desc = _resourceDatabase.GetResourceDescription(texture.AsResource());
        desc.textureDescription.Format.GetSurfaceInfo((int)desc.textureDescription.Width, (int)desc.textureDescription.Height, out var rowPitch, out var slicePitch, out _);

        var subresourceData = new SubResourceData
        {
            pData = data.GetUnsafePtr(),
            rowPitch = rowPitch,
            slicePitch = slicePitch
        };

        var sateBefore = _resourceDatabase.GetResourceState(texture.AsResource());
        var needTransition = sateBefore != ResourceState.CopyDest;

        if (needTransition)
        {
            _copyCmb.ResourceBarrier(texture.AsResource(), sateBefore, ResourceState.CopyDest);
            _resourceDatabase.SetResourceState(texture.AsResource(), ResourceState.CopyDest);
        }

        _copyCmb.Upload(texture, subresourceData);

        if (needTransition)
        {
            _copyCmb.ResourceBarrier(texture.AsResource(), ResourceState.CopyDest, sateBefore);
            _resourceDatabase.SetResourceState(texture.AsResource(), sateBefore);
        }
    }
#if false
    // TODO: Ideally we should queue the draw call to our rendering system, and render it in the full rendering pipeline.
    // This is just a place holder for now for testing purpose.
    // TODO: Since we are using mesh shader, we should use dispatch mesh instead of draw calls.
    public void RenderMesh(Handle<Mesh> mesh, Handle<Material> material, string passName)
    {
        ref var meshRef = ref _resourceDatabase.GetMeshReference(mesh);
        ref var materialRef = ref _resourceDatabase.GetMaterialReference(material);
        var shader = _resourceDatabase.GetShaderReference(materialRef.Shader);

        shader.TryGetPassKey(passName, out var passIndex, out var passKey);
        var hash = new GraphicsPipelineHash
        {
            id = passKey,
            rtvCount = 1,
            dsvFormat = TextureFormat.Unknown,
        };

        hash.rtvFormats[0] = TextureFormat.B8G8R8A8_UNorm;
        var pipelineKey = hash.GetKey();
        _directCmb.SetPipelineState(pipelineKey);

        // FIX: Get valid root signature. In D3D12, we use fixed root signature layout for bindless rendering.
        // However, our code should not assume that blindly. Each pipeline should have contained root signature info even if there are fixed.
        // This ensures that future changes to root signature layout can be accommodated.

        // for (int i = 0; i < 4; i++)
        {
            ref var cache = ref materialRef.GetPassCache(passIndex);
            _directCmb.SetConstantBufferView(RootSignatureLayout.PER_MATERIAL_BUFFER_SLOT, cache.GpuResource);
        }

        // NOTE: Since we are using true bindless resources, we only need to set the descriptor heaps, not individual tables.
        // TODO: Matbe handle the transitional bindless model?
#if false
        var samplerGpuHandle = _descriptorAllocator.GetSamplerHeap()->GetGPUDescriptorHandleForHeapStart();
        _commandList.Get()->SetGraphicsRootDescriptorTable(rootParamIndex, samplerGpuHandle);
#endif
        _directCmb.SetPrimitiveTopology(PrimitiveTopology.Triangle);

        // Draw without vertex/index buffers - use instanced drawing
        // Each instance represents a triangle (3 vertices)
        var triangleCount = (uint)meshRef.indices.Count / 3;
        _directCmb.Draw(3, triangleCount, 0, 0);
    }
#endif

    // TODO: Ideally we should queue the draw call to our rendering system, and render it in the full rendering pipeline.
    // This is just a place holder for now for testing purpose.
    // TODO: Since we are using mesh shader, we should use dispatch mesh instead of draw calls.
    public void DispatchMesh(Handle<Mesh> mesh, Handle<Material> material, string passName, uint numThreadsX)
    {
        ref var meshRef = ref _resourceDatabase.GetMeshReference(mesh);
        ref var materialRef = ref _resourceDatabase.GetMaterialReference(material);
        var shader = _resourceDatabase.GetShaderReference(materialRef.Shader);

        shader.TryGetPassKey(passName, out var passIndex, out var passKey);
        var hash = new GraphicsPipelineHash
        {
            id = passKey,
            rtvCount = 1,
            dsvFormat = TextureFormat.Unknown,
        };

        hash.rtvFormats[0] = TextureFormat.B8G8R8A8_UNorm;
        var pipelineKey = hash.GetKey();
        _directCmb.SetPipelineState(pipelineKey);

        // FIX: Get valid root signature. In D3D12, we use fixed root signature layout for bindless rendering.
        // However, our code should not assume that blindly. Each pipeline should have contained root signature info even if there are fixed.
        // This ensures that future changes to root signature layout can be accommodated.

        // for (int i = 0; i < 4; i++)
        {
            ref var cache = ref materialRef.GetPassCache(passIndex);
            _directCmb.SetConstantBufferView(RootSignatureLayout.PER_MATERIAL_BUFFER_SLOT, cache.GpuResource);
        }

        // NOTE: Since we are using true bindless resources, we only need to set the descriptor heaps, not individual tables.
        // TODO: Matbe handle the transitional bindless model?
#if false
        var samplerGpuHandle = _descriptorAllocator.GetSamplerHeap()->GetGPUDescriptorHandleForHeapStart();
        _commandList.Get()->SetGraphicsRootDescriptorTable(rootParamIndex, samplerGpuHandle);
#endif

        var threadGroupCountX = ((uint)meshRef.indices.Count + numThreadsX - 1) / numThreadsX;
        _directCmb.DispatchMesh(threadGroupCountX, 1, 1);
    }

    public void ExecuteCopyCommands()
    {
        _device.CopyQueue.Submit(_copyCmb);
        _device.CopyQueue.WaitIdle();
    }
}
