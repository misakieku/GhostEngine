using Ghost.Core;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;

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

    public IShaderCompiler ShaderCompiler => _engine.ShaderCompiler;
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

    public Handle<Mesh> CreateMesh(UnsafeList<Vertex> vertices, UnsafeList<uint> indices, bool staticMesh)
    {
        var mesh = ResourceAllocator.CreateMesh(vertices, indices);
        ref var meshData = ref ResourceDatabase.GetMeshReference(mesh);

        var vertexHandle = meshData.VertexBuffer.AsResource();
        var indexHandle = meshData.IndexBuffer.AsResource();

        _directCmd.ResourceBarrier(vertexHandle, ResourceState.Common, ResourceState.CopyDest);
        _directCmd.ResourceBarrier(indexHandle, ResourceState.Common, ResourceState.CopyDest);

        _directCmd.UploadBuffer(meshData.VertexBuffer, meshData.Vertices.AsSpan());
        _directCmd.UploadBuffer(meshData.IndexBuffer, meshData.Indices.AsSpan());

        _directCmd.ResourceBarrier(vertexHandle, ResourceState.CopyDest, ResourceState.VertexAndConstantBuffer);
        _directCmd.ResourceBarrier(indexHandle, ResourceState.CopyDest, ResourceState.IndexBuffer);

        if (staticMesh)
        {
            meshData.ReleaseCpuResources();
        }

        return mesh;
    }

    public Handle<Mesh> CreateMesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices, bool staticMesh)
    {
        var vertexList = new UnsafeList<Vertex>(vertices.Length, Allocator.Persistent);
        var indexList = new UnsafeList<uint>(indices.Length, Allocator.Persistent);

        vertexList.CopyFrom(vertices);
        indexList.CopyFrom(indices);

        return CreateMesh(vertexList, indexList, staticMesh);
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
        var vertexState = ResourceDatabase.GetResourceState(meshData.VertexBuffer.AsResource());
        var indexState = ResourceDatabase.GetResourceState(meshData.IndexBuffer.AsResource());
        var needVertexTransition = vertexState != ResourceState.CopyDest;
        var needIndexTransition = indexState != ResourceState.CopyDest;

        if (needVertexTransition)
        {
            _directCmd.ResourceBarrier(meshData.VertexBuffer.AsResource(), vertexState, ResourceState.CopyDest);
        }

        if (needIndexTransition)
        {
            _directCmd.ResourceBarrier(meshData.IndexBuffer.AsResource(), indexState, ResourceState.CopyDest);
        }

        _directCmd.UploadBuffer(meshData.VertexBuffer, meshData.Vertices.AsSpan());
        _directCmd.UploadBuffer(meshData.IndexBuffer, meshData.Indices.AsSpan());

        if (needVertexTransition)
        {
            _directCmd.ResourceBarrier(meshData.VertexBuffer.AsResource(), ResourceState.CopyDest, vertexState);
        }

        if (needIndexTransition)
        {
            _directCmd.ResourceBarrier(meshData.IndexBuffer.AsResource(), ResourceState.CopyDest, indexState);
        }

        if (markMeshStatic)
        {
            meshData.ReleaseCpuResources();
        }
    }

    public void UpdateObjectData(Handle<Mesh> mesh, float4x4 localToWorld)
    {
        ref var meshData = ref ResourceDatabase.GetMeshReference(mesh);
        var data = new PerObjectData
        {
            localToWorld = localToWorld,
            worldBoundsMin = meshData.BoundingBox.Min,
            worldBoundsMax = meshData.BoundingBox.Max,
            vertexBuffer = (uint)_engine.ResourceDatabase.GetBindlessIndex(meshData.VertexBuffer.AsResource()),
            indexBuffer = (uint)_engine.ResourceDatabase.GetBindlessIndex(meshData.IndexBuffer.AsResource()),
        };

        var bufferHandle = meshData.ObjectDataBuffer.AsResource();
        var state = ResourceDatabase.GetResourceState(bufferHandle);
        var needTransition = state != ResourceState.CopyDest;
        if (needTransition)
        {
            _directCmd.ResourceBarrier(bufferHandle, state, ResourceState.CopyDest);
        }

        _directCmd.UploadBuffer(meshData.ObjectDataBuffer, [data]);

        if (needTransition)
        {
            _directCmd.ResourceBarrier(bufferHandle, ResourceState.CopyDest, ResourceState.VertexAndConstantBuffer);
        }
    }

    public Handle<Texture> CreateTexture(ref readonly TextureDesc desc, bool tempResource = false)
    {
        return ResourceAllocator.CreateTexture(in desc, tempResource);
    }

    public void UploadTexture(Handle<Texture> texture, ReadOnlySpan<byte> data)
    {
        var desc = ResourceDatabase.GetResourceDescription(texture.AsResource());
        desc.TextureDescription.Format.GetSurfaceInfo((int)desc.TextureDescription.Width, (int)desc.TextureDescription.Height, out var rowPitch, out var slicePitch, out _);

        var sateBefore = ResourceDatabase.GetResourceState(texture.AsResource());
        var needTransition = sateBefore != ResourceState.CopyDest;

        if (needTransition)
        {
            _directCmd.ResourceBarrier(texture.AsResource(), sateBefore, ResourceState.CopyDest);
        }

        fixed (byte* pData = data)
        {
            var subresourceData = new SubResourceData
            {
                pData = pData,
                rowPitch = rowPitch,
                slicePitch = slicePitch
            };

            _directCmd.UploadTexture(texture, [subresourceData]);
        }

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

        var keyResult = shader.TryGetPassKey(passName, out var passIndex);
        if (keyResult.Status != ResultStatus.Success)
        {
            throw new Exception(keyResult.ToString());
        }

        var hash = new GraphicsPipelineHash
        {
            Id = keyResult.Value.Identifier,
            RtvCount = 1,
            DsvFormat = TextureFormat.Unknown,
        };

        hash.RtvFormats[0] = TextureFormat.B8G8R8A8_UNorm;
        var pipelineKey = hash.GetKey();
        _directCmd.SetPipelineState(pipelineKey);

        _directCmd.SetConstantBufferView(RootSignatureLayout.PER_OBJECT_BUFFER_SLOT, meshRef.ObjectDataBuffer);

        // NOTE: We use fixed root signature layout for bindless rendering.
        var cache = materialRef.CBufferCache;
        if (cache.IsCreated)
        {
            _directCmd.SetConstantBufferView(RootSignatureLayout.PER_MATERIAL_BUFFER_SLOT, cache.GpuResource);
        }

        var threadGroupCountX = ((uint)meshRef.IndexCount + numThreadsX - 1) / numThreadsX;
        _directCmd.DispatchMesh(threadGroupCountX, 1, 1);
    }
}