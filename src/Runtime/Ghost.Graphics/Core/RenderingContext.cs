using Ghost.Core;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Graphics.Core;

public readonly unsafe ref struct RenderingContext
{
    private readonly IGraphicsEngine _engine;
    private readonly ICommandBuffer _directCmd;

    public ICommandBuffer DirectCommandBuffer => _directCmd;

    public IShaderCompiler ShaderCompiler => _engine.ShaderCompiler;
    public IResourceAllocator ResourceAllocator => _engine.ResourceAllocator;
    public IResourceDatabase ResourceDatabase => _engine.ResourceDatabase;
    public IPipelineLibrary PipelineLibrary => _engine.PipelineLibrary;

    internal RenderingContext(IGraphicsEngine engine, ICommandBuffer directCmd)
    {
        _engine = engine;
        _directCmd = directCmd;
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
            _ => throw new InvalidOperationException("Unknown command buffer type."),
        };

        queue.Submit(commandBuffer);
        queue.WaitIdle();
    }

    private void TransitionBarrier(Handle<GPUResource> resource, bool isTexture, BarrierLayout newLayout, BarrierAccess newAccess, BarrierSync newSync)
    {
        var r = ResourceDatabase.GetResourceBarrierData(resource);
        if (r.IsFailure)
        {
            return;
        }

        var data = r.Value;
        if (data.layout == newLayout && data.access == newAccess && data.sync == newSync)
        {
            return;
        }

        BarrierDesc desc;
        if (isTexture)
        {
            desc = BarrierDesc.Texture(
            resource,
            data.sync, newSync,
            data.access, newAccess,
            data.layout, newLayout);
        }
        else
        {
            desc = BarrierDesc.Buffer(
            resource,
            data.sync, newSync,
            data.access, newAccess);
        }

        _directCmd.ResourceBarrier(new ReadOnlySpan<BarrierDesc>(in desc));
        ResourceDatabase.SetResourceBarrierData(resource, new ResourceBarrierData(newLayout, newAccess, newSync));
    }

    public Handle<Mesh> CreateMesh(UnsafeList<Vertex> vertices, UnsafeList<uint> indices, bool staticMesh)
    {
        var mesh = ResourceAllocator.CreateMesh(vertices, indices);
        var r = ResourceDatabase.GetMeshReference(mesh);
        if (r.IsFailure)
        {
            return mesh;
        }

        ref readonly var meshData = ref r.Value;
        var vertexHandle = meshData.VertexBuffer.AsResource();
        var indexHandle = meshData.IndexBuffer.AsResource();

        TransitionBarrier(vertexHandle, false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);
        TransitionBarrier(indexHandle, false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);

        _directCmd.UploadBuffer(meshData.VertexBuffer, meshData.Vertices.AsSpan());
        _directCmd.UploadBuffer(meshData.IndexBuffer, meshData.Indices.AsSpan());

        if (staticMesh)
        {
            meshData.ReleaseCpuResources();
            TransitionBarrier(vertexHandle, false, BarrierLayout.Undefined, BarrierAccess.ShaderResource, BarrierSync.VertexShading);
            TransitionBarrier(indexHandle, false, BarrierLayout.Undefined, BarrierAccess.IndexBuffer, BarrierSync.IndexInput);
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
        var r = ResourceDatabase.GetMeshReference(mesh);
        if (r.IsFailure)
        {
            return;
        }

        ref readonly var meshRef = ref r.Value;
        var vertexHandle = meshRef.VertexBuffer.AsResource();
        var indexHandle = meshRef.IndexBuffer.AsResource();

        TransitionBarrier(vertexHandle, false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);
        TransitionBarrier(indexHandle, false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);

        _directCmd.UploadBuffer(meshRef.VertexBuffer, meshRef.Vertices.AsSpan());
        _directCmd.UploadBuffer(meshRef.IndexBuffer, meshRef.Indices.AsSpan());

        TransitionBarrier(vertexHandle, false, BarrierLayout.Undefined, BarrierAccess.ShaderResource, BarrierSync.VertexShading);
        TransitionBarrier(indexHandle, false, BarrierLayout.Undefined, BarrierAccess.IndexBuffer, BarrierSync.IndexInput);

        if (markMeshStatic)
        {
            meshRef.ReleaseCpuResources();
        }
    }

    public void UpdateObjectData(Handle<Mesh> mesh, float4x4 localToWorld)
    {
        var r = ResourceDatabase.GetMeshReference(mesh);
        if (r.IsFailure)
        {
            return;
        }

        ref readonly var meshData = ref r.Value;
        var data = new PerObjectData
        {
            localToWorld = localToWorld,
            worldBoundsMin = meshData.BoundingBox.Min,
            worldBoundsMax = meshData.BoundingBox.Max,
            vertexBuffer = _engine.ResourceDatabase.GetBindlessIndex(meshData.VertexBuffer.AsResource()),
            indexBuffer = _engine.ResourceDatabase.GetBindlessIndex(meshData.IndexBuffer.AsResource()),
        };

        var bufferHandle = meshData.ObjectDataBuffer.AsResource();

        TransitionBarrier(bufferHandle, false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);
        _directCmd.UploadBuffer(meshData.ObjectDataBuffer, data);
        TransitionBarrier(bufferHandle, false, BarrierLayout.Undefined, BarrierAccess.ShaderResource, BarrierSync.PixelShading | BarrierSync.NonPixelShading);
    }

    public Handle<Texture> CreateTexture<T>(ref readonly TextureDesc desc, ReadOnlySpan<T> data, string name)
        where T : unmanaged
    {
        var handle = ResourceAllocator.CreateTexture(in desc, name);
        UploadTexture(handle, data);

        return handle;
    }

    public void UploadTexture<T>(Handle<Texture> texture, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        var desc = ResourceDatabase.GetResourceDescription(texture.AsResource()).GetValueOrThrow();
        
        //var size = ResourceAllocator.GetSizeInfo(desc).Size;
        //if ((ulong)(data.Length * sizeof(T)) != ResourceAllocator.GetSizeInfo(desc).Size)
        //{
        //    throw new ArgumentException("Data size does not match texture size.");
        //}

        desc.TextureDescription.Format.GetSurfaceInfo(desc.TextureDescription.Width, desc.TextureDescription.Height, out var rowPitch, out var slicePitch, out _);

        TransitionBarrier(texture.AsResource(), true, BarrierLayout.CopyDest, BarrierAccess.CopyDest, BarrierSync.Copy);

        fixed (T* pData = data)
        {
            var subresourceData = new SubResourceData
            {
                pData = pData,
                rowPitch = rowPitch,
                slicePitch = slicePitch
            };

            _directCmd.UploadTexture(texture, subresourceData);
        }
    }
}
