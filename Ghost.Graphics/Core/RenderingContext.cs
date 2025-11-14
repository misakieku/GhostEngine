using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;

namespace Ghost.Graphics.Core;

public unsafe readonly ref struct RenderingContext
{
    private readonly IGraphicsEngine _engine;
    private readonly ICommandBuffer _directCmd;
    private readonly ICommandBuffer _copyCmd;
    private readonly ICommandBuffer _computeCmd;

    public ICommandBuffer DirectCommandBuffer => _directCmd;
    public ICommandBuffer CopyCommandBuffer => _copyCmd;
    public ICommandBuffer ComputeCommandBuffer => _computeCmd;

    public IResourceAllocator ResourceAllocator => _engine.ResourceAllocator;
    public IResourceDatabase ResourceDatabase => _engine.ResourceDatabase;
    public IPipelineLibrary PipelineLibrary => _engine.PipelineLibrary;

    internal RenderingContext(
        IGraphicsEngine engine,
        ICommandBuffer directCmd,
        ICommandBuffer copyCmd,
        ICommandBuffer computeCmd)
    {
        _engine = engine;
        _directCmd = directCmd;
        _copyCmd = copyCmd;
        _computeCmd = computeCmd;
    }

    public ICommandBuffer CrearteCommandBuffer(CommandBufferType type)
    {
        return _engine.CreateCommandBuffer(type);
    }

    // TODO: ExecuteCommandBufferAsync with fencene.Device.GraphicsQueue.Submit(commandBuffer);
    public void ExecuteCommandBuffer(ICommandBuffer commandBuffer)
    {
        var queue = commandBuffer.Type switch
        {
            CommandBufferType.Graphics => _engine.Device.GraphicsQueue,
            CommandBufferType.Compute => _engine.Device.ComputeQueue,
            CommandBufferType.Copy => _engine.Device.CopyQueue,
            _ => throw new ArgumentOutOfRangeException(),
        };

        queue.Submit(commandBuffer);
        queue.WaitIdle();
    }

    public Handle<Mesh> CreateMesh(UnsafeList<Vertex> vertices, UnsafeList<uint> indices)
    {
        var mesh = ResourceAllocator.CreateMesh(vertices, indices);
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
        ref var meshData = ref ResourceDatabase.GetMeshReference(mesh);
        var vertexState = ResourceDatabase.GetResourceState(meshData.vertexBuffer.AsResource());
        var indexState = ResourceDatabase.GetResourceState(meshData.indexBuffer.AsResource());
        var needVertexTransition = vertexState != ResourceState.CopyDest;
        var needIndexTransition = indexState != ResourceState.CopyDest;

        if (needVertexTransition)
        {
            _directCmd.ResourceBarrier(meshData.vertexBuffer.AsResource(), vertexState, ResourceState.CopyDest);
        }

        if (needIndexTransition)
        {
            _directCmd.ResourceBarrier(meshData.indexBuffer.AsResource(), indexState, ResourceState.CopyDest);
        }

        _directCmd.UploadBuffer(meshData.vertexBuffer, meshData.vertices.AsSpan());
        _directCmd.UploadBuffer(meshData.indexBuffer, meshData.indices.AsSpan());

        if (needVertexTransition)
        {
            _directCmd.ResourceBarrier(meshData.vertexBuffer.AsResource(), ResourceState.CopyDest, vertexState);
        }

        if (needIndexTransition)
        {
            _directCmd.ResourceBarrier(meshData.indexBuffer.AsResource(), ResourceState.CopyDest, indexState);
        }

        if (markMeshStatic)
        {
            meshData.ReleaseCpuResources();
        }
    }

    public Handle<Texture> CreateTexture(ref readonly TextureDesc desc, bool tempResource = false)
    {
        return ResourceAllocator.CreateTexture(in desc, tempResource);
    }

    public void UploadTexture(Handle<Texture> texture, ReadOnlySpan<byte> data)
    {
        var desc = ResourceDatabase.GetResourceDescription(texture.AsResource());
        desc.textureDescription.Format.GetSurfaceInfo((int)desc.textureDescription.Width, (int)desc.textureDescription.Height, out var rowPitch, out var slicePitch, out _);

        var subresourceData = new SubResourceData
        {
            pData = data.GetUnsafePtr(),
            rowPitch = rowPitch,
            slicePitch = slicePitch
        };

        var sateBefore = ResourceDatabase.GetResourceState(texture.AsResource());
        var needTransition = sateBefore != ResourceState.CopyDest;

        if (needTransition)
        {
            _directCmd.ResourceBarrier(texture.AsResource(), sateBefore, ResourceState.CopyDest);
        }

        _directCmd.UploadTexture(texture, subresourceData);

        if (needTransition)
        {
            _directCmd.ResourceBarrier(texture.AsResource(), ResourceState.CopyDest, sateBefore);
        }
    }

    // TODO: Ideally we should queue the draw call to our rendering system, and render it in a full rendering pipeline.
    // This is just a place holder for now for testing purpose.
    public void DispatchMesh(Handle<Mesh> mesh, Handle<Material> material, string passName, uint numThreadsX)
    {
        ref var meshRef = ref ResourceDatabase.GetMeshReference(mesh);
        ref var materialRef = ref ResourceDatabase.GetMaterialReference(material);
        var shader = ResourceDatabase.GetShaderReference(materialRef.Shader);

        shader.TryGetPassKey(passName, out var passIndex, out var passKey);
        var hash = new GraphicsPipelineHash
        {
            id = passKey,
            rtvCount = 1,
            dsvFormat = TextureFormat.Unknown,
        };

        hash.rtvFormats[0] = TextureFormat.B8G8R8A8_UNorm;
        var pipelineKey = hash.GetKey();
        _directCmd.SetPipelineState(pipelineKey);

        // NOTE: We use fixed root signature layout for bindless rendering.
        ref var cache = ref materialRef.GetPassCache(passIndex);
        _directCmd.SetConstantBufferView(RootSignatureLayout.PER_MATERIAL_BUFFER_SLOT, cache.GpuResource);

        // NOTE: Since we are using true bindless resources, we only need to set the descriptor heaps, not individual tables.
        // TODO: Maybe handle the traditional bindless model?
#if false
        var samplerGpuHandle = _descriptorAllocator.GetSamplerHeap()->GetGPUDescriptorHandleForHeapStart();
        _commandList.Get()->SetGraphicsRootDescriptorTable(rootParamIndex, samplerGpuHandle);
#endif

        var threadGroupCountX = ((uint)meshRef.indices.Count + numThreadsX - 1) / numThreadsX;
        _directCmd.DispatchMesh(threadGroupCountX, 1, 1);
    }
}
