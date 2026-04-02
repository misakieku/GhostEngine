using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics;

namespace Ghost.Graphics.Core;

// TODO: Temporary rendering context for heap creation and data upload. We will refactor it later when we have a better understanding of the engine architecture.
public readonly unsafe ref struct RenderingContext
{
    private readonly IGraphicsEngine _engine;
    private readonly ResourceManager _resourceManager;
    private readonly ICommandBuffer _directCmd;

    public ICommandBuffer DirectCommandBuffer => _directCmd;

    public IShaderCompiler ShaderCompiler => _engine.ShaderCompiler;
    public ResourceManager ResourceManager => _resourceManager;
    public IResourceAllocator ResourceAllocator => _engine.ResourceAllocator;
    public IResourceDatabase ResourceDatabase => _engine.ResourceDatabase;
    public IPipelineLibrary PipelineLibrary => _engine.PipelineLibrary;

    internal RenderingContext(IGraphicsEngine engine, ResourceManager resourceManager, ICommandBuffer directCmd)
    {
        _engine = engine;
        _resourceManager = resourceManager;
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

        _directCmd.Barrier(new ReadOnlySpan<BarrierDesc>(in desc));
        ResourceDatabase.SetResourceBarrierData(resource, new ResourceBarrierData(newLayout, newAccess, newSync));
    }

    private void UploadBuffer<T>(Handle<GPUBuffer> buffer, params ReadOnlySpan<T> data)
        where T : unmanaged
    {
        var r = _engine.ResourceDatabase.GetResourceDescription(buffer.AsResource());
        if (r.IsFailure)
        {
            return;
        }

        Debug.Assert(r.Value.Type == ResourceType.Buffer);

        var sizeInBytes = (nuint)(data.Length * sizeof(T));
        var memoryType = r.Value.BufferDescription.MemoryType;

        if (memoryType == ResourceMemoryType.Upload)
        {
            fixed (T* pData = data)
            {
                ResourceDatabase.Map(buffer.AsResource(), 0, null, null, pData, sizeInBytes);
            }
        }
        else
        {
            //var uploadHandle = _resourceAllocator.CreateTempUploadBuffer(sizeInBytes, out var offset);
            //var uploadResource = _resourceDatabase.GetResource(uploadHandle.AsResource());
            var uploadDesc = new BufferDesc
            {
                Size = sizeInBytes,
                Usage = BufferUsage.Upload,
                MemoryType = ResourceMemoryType.Upload,
            };

            var uploadHandle = _resourceManager.CreateTransientBuffer(in uploadDesc);

            fixed (T* pData = data)
            {
                ResourceDatabase.Map(uploadHandle.AsResource(), 0, null, null, pData, sizeInBytes);
            }

            _directCmd.CopyBuffer(buffer, uploadHandle, 0, 0, sizeInBytes);
        }
    }

    public Handle<Mesh> CreateMesh(UnsafeList<Vertex> vertices, UnsafeList<uint> indices, bool staticMesh)
    {
        var mesh = _resourceManager.CreateMesh(vertices, indices);
        var r = _resourceManager.GetMeshReference(mesh);
        if (r.IsFailure)
        {
            return mesh;
        }

        ref var meshData = ref r.Value;
        var vertexHandle = meshData.VertexBuffer.AsResource();
        var indexHandle = meshData.IndexBuffer.AsResource();

        TransitionBarrier(vertexHandle, false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);
        TransitionBarrier(indexHandle, false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);

        UploadBuffer(meshData.VertexBuffer, meshData.Vertices.AsSpan());
        UploadBuffer(meshData.IndexBuffer, meshData.Indices.AsSpan());

        TransitionBarrier(vertexHandle, false, BarrierLayout.Undefined, BarrierAccess.ShaderResource, BarrierSync.VertexShading);
        TransitionBarrier(indexHandle, false, BarrierLayout.Undefined, BarrierAccess.IndexBuffer, BarrierSync.IndexInput);

        if (staticMesh)
        {
            meshData.CookMeshlets();
            UploadMeshlets(mesh);
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
        var r = _resourceManager.GetMeshReference(mesh);
        if (r.IsFailure)
        {
            return;
        }

        ref var meshRef = ref r.Value;
        var vertexHandle = meshRef.VertexBuffer.AsResource();
        var indexHandle = meshRef.IndexBuffer.AsResource();

        TransitionBarrier(vertexHandle, false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);
        TransitionBarrier(indexHandle, false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);

        UploadBuffer(meshRef.VertexBuffer, meshRef.Vertices.AsSpan());
        UploadBuffer(meshRef.IndexBuffer, meshRef.Indices.AsSpan());

        TransitionBarrier(vertexHandle, false, BarrierLayout.Undefined, BarrierAccess.ShaderResource, BarrierSync.VertexShading);
        TransitionBarrier(indexHandle, false, BarrierLayout.Undefined, BarrierAccess.IndexBuffer, BarrierSync.IndexInput);

        if (markMeshStatic)
        {
            meshRef.ReleaseCpuResources();
        }
    }

    public void UploadMeshlets(Handle<Mesh> mesh)
    {
        var r = _resourceManager.GetMeshReference(mesh);
        if (r.IsFailure)
        {
            return;
        }

        ref var meshRef = ref r.Value;
        ref readonly var meshletData = ref meshRef.MeshletData;

        if (!meshletData.meshlets.IsCreated || meshletData.meshlets.Count == 0) return;

        var meshletDesc = new BufferDesc
        {
            Size = (uint)(meshletData.meshlets.Count * sizeof(Meshlet)),
            Stride = (uint)sizeof(Meshlet),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            MemoryType = ResourceMemoryType.Default,
        };
        var verticesDesc = new BufferDesc
        {
            Size = (uint)(meshletData.meshletVertices.Count * sizeof(uint)),
            Stride = sizeof(uint),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            MemoryType = ResourceMemoryType.Default,
        };
        // Ensure size is multiple of 4 for Raw buffer
        var trianglesSize = (uint)meshletData.meshletTriangles.Count * sizeof(uint);
        var trianglesDesc = new BufferDesc
        {
            Size = trianglesSize,
            Stride = sizeof(uint),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            MemoryType = ResourceMemoryType.Default,
        };

        meshRef.MeshLetBuffer = _engine.ResourceAllocator.CreateBuffer(in meshletDesc, "Meshlets");
        meshRef.MeshletVerticesBuffer = _engine.ResourceAllocator.CreateBuffer(in verticesDesc, "MeshletVertices");
        meshRef.MeshletTrianglesBuffer = _engine.ResourceAllocator.CreateBuffer(in trianglesDesc, "MeshletTriangles");

        TransitionBarrier(meshRef.MeshLetBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);
        TransitionBarrier(meshRef.MeshletVerticesBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);
        TransitionBarrier(meshRef.MeshletTrianglesBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);

        UploadBuffer(meshRef.MeshLetBuffer, meshletData.meshlets.AsSpan());
        UploadBuffer(meshRef.MeshletVerticesBuffer, meshletData.meshletVertices.AsSpan());
        UploadBuffer(meshRef.MeshletTrianglesBuffer, meshletData.meshletTriangles.AsSpan());

        TransitionBarrier(meshRef.MeshLetBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.ShaderResource, BarrierSync.NonPixelShading | BarrierSync.PixelShading);
        TransitionBarrier(meshRef.MeshletVerticesBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.ShaderResource, BarrierSync.NonPixelShading | BarrierSync.PixelShading);
        TransitionBarrier(meshRef.MeshletTrianglesBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.ShaderResource, BarrierSync.NonPixelShading | BarrierSync.PixelShading);
    }

    public void UpdateObjectData(Handle<Mesh> mesh)
    {
        var r = _resourceManager.GetMeshReference(mesh);
        if (r.IsFailure)
        {
            return;
        }

        ref readonly var meshData = ref r.Value;
        var data = new MeshData
        {
            worldBoundsMin = meshData.BoundingBox.Min,
            worldBoundsMax = meshData.BoundingBox.Max,
            vertexBuffer = _engine.ResourceDatabase.GetBindlessIndex(meshData.VertexBuffer.AsResource()),
            indexBuffer = _engine.ResourceDatabase.GetBindlessIndex(meshData.IndexBuffer.AsResource()),
            meshletBuffer = _engine.ResourceDatabase.GetBindlessIndex(meshData.MeshLetBuffer.AsResource()),
            meshletVerticesBuffer = _engine.ResourceDatabase.GetBindlessIndex(meshData.MeshletVerticesBuffer.AsResource()),
            meshletTrianglesBuffer = _engine.ResourceDatabase.GetBindlessIndex(meshData.MeshletTrianglesBuffer.AsResource()),
        };

        var bufferHandle = meshData.ObjectDataBuffer.AsResource();

        TransitionBarrier(bufferHandle, false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);
        UploadBuffer(meshData.ObjectDataBuffer, data);
        TransitionBarrier(bufferHandle, false, BarrierLayout.Undefined, BarrierAccess.ShaderResource, BarrierSync.PixelShading | BarrierSync.NonPixelShading);
    }

    public Handle<GPUTexture> CreateTexture<T>(ref readonly TextureDesc desc, ReadOnlySpan<T> data, string name)
        where T : unmanaged
    {
        var handle = ResourceAllocator.CreateTexture(in desc, name);
        UploadTexture(handle, data);

        return handle;
    }

    public void UploadTexture<T>(Handle<GPUTexture> texture, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        //var size = ResourceAllocator.GetSizeInfo(desc).Size;
        //if ((ulong)(data.Length * sizeof(T)) != ResourceAllocator.GetSizeInfo(desc).Size)
        //{
        //    throw new ArgumentException("Data size does not match texture size.");
        //}

        var desc = ResourceDatabase.GetResourceDescription(texture.AsResource()).GetValueOrThrow();
        desc.TextureDescription.Format.GetSurfaceInfo(desc.TextureDescription.Width, desc.TextureDescription.Height, out var rowPitch, out var slicePitch, out _);

        var requiredSize = ResourceDatabase.GetIntermediateResourceSize(texture.AsResource(), 0, 1);
        var uploadDesc = new BufferDesc
        {
            Size = requiredSize,
            Usage = BufferUsage.Upload,
            MemoryType = ResourceMemoryType.Upload,
        };

        var uploadHandle = _resourceManager.CreateTransientBuffer(in uploadDesc);
        if (uploadHandle.IsInvalid)
        {
            throw new OutOfMemoryException("Failed to create upload buffer for texture data.");
        }

        TransitionBarrier(texture.AsResource(), true, BarrierLayout.CopyDest, BarrierAccess.CopyDest, BarrierSync.Copy);

        fixed (T* pData = data)
        {
            var subresourceData = new SubResourceData
            {
                pData = pData,
                rowPitch = rowPitch,
                slicePitch = slicePitch
            };

            _directCmd.UpdateSubResources(texture.AsResource(), uploadHandle.AsResource(), subresourceData);
        }
    }
}
