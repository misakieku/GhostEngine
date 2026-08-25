using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Core;

// TODO: Temporary rendering context for heap creation and data upload. We will refactor it later when we have a better understanding of the engine architecture.
public unsafe class RenderContext
{
    public ICommandBuffer CommandBuffer
    {
        get; internal set;
    } = null!;

    public ResourceManager ResourceManager
    {
        get;
    }

    public IResourceAllocator ResourceAllocator
    {
        get;
    }

    public IResourceDatabase ResourceDatabase
    {
        get;
    }

    public IPipelineLibrary PipelineLibrary
    {
        get;
    }

    internal ShaderLibrary ShaderLibrary
    {
        get;
    }

    internal RenderContext(ResourceManager resourceManager, IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase, IPipelineLibrary pipelineLibrary, ShaderLibrary shaderLibrary)
    {
        ResourceManager = resourceManager;
        ResourceAllocator = resourceAllocator;
        ResourceDatabase = resourceDatabase;
        PipelineLibrary = pipelineLibrary;
        ShaderLibrary = shaderLibrary;
    }

    private void TransitionBarrier(Handle<GPUResource> resource, bool isTexture, BarrierLayout newLayout, BarrierAccess newAccess, BarrierSync newSync)
    {
        BarrierDesc desc;
        if (isTexture)
        {
            desc = BarrierDesc.Texture(resource.AsTexture(), newSync, newAccess, newLayout);
        }
        else
        {
            desc = BarrierDesc.Buffer(resource.AsBuffer(), newSync, newAccess);
        }

        CommandBuffer.Barrier(desc);
    }

    public void UploadBuffer<T>(Handle<GPUBuffer> buffer, params ReadOnlySpan<T> data)
        where T : unmanaged
    {
        var r = ResourceDatabase.GetResourceDescription(buffer.AsResource());
        if (r.IsFailure)
        {
            return;
        }

        Logger.DebugAssert(r.Value.Type == ResourceType.Buffer);

        var sizeInBytes = (nuint)(data.Length * sizeof(T));
        var memoryType = r.Value.BufferDescriptor.HeapType;

        if (memoryType == HeapType.Upload)
        {
            fixed (T* pData = data)
            {
                var mappedData = ResourceDatabase.MapResource(buffer.AsResource(), 0, null);
                MemoryUtility.MemCpy(mappedData, pData, sizeInBytes);
                ResourceDatabase.UnmapResource(buffer.AsResource(), 0, null);
            }
        }
        else
        {
            var uploadDesc = new BufferDesc
            {
                Size = sizeInBytes,
                Usage = BufferUsage.Upload,
                HeapType = HeapType.Upload,
            };

            var uploadHandle = ResourceManager.CreateTransientBuffer(in uploadDesc);
            if (uploadHandle.IsInvalid)
            {
                throw new OutOfMemoryException("Failed to create upload buffer for buffer data.");
            }

            fixed (T* pData = data)
            {
                var mappedData = ResourceDatabase.MapResource(uploadHandle.AsResource(), 0, null);
                MemoryUtility.MemCpy(mappedData, pData, sizeInBytes);
                ResourceDatabase.UnmapResource(uploadHandle.AsResource(), 0, null);
            }

            CommandBuffer.CopyBuffer(buffer, uploadHandle, 0, 0, sizeInBytes);
        }
    }

    /// <summary>
    /// Uploads a sub-range of data into an existing GPU buffer at the specified byte offset.
    /// Used for incremental uploads (e.g. dirty palette ranges).
    /// </summary>
    public void UploadBufferRange<T>(Handle<GPUBuffer> buffer, ReadOnlySpan<T> data, uint byteOffset)
        where T : unmanaged
    {
        if (data.IsEmpty)
        {
            return;
        }

        var sizeInBytes = (nuint)(data.Length * sizeof(T));

        var uploadDesc = new BufferDesc
        {
            Size = sizeInBytes,
            Usage = BufferUsage.Upload,
            HeapType = HeapType.Upload,
        };

        var uploadHandle = ResourceManager.CreateTransientBuffer(in uploadDesc);
        if (uploadHandle.IsInvalid)
        {
            throw new OutOfMemoryException("Failed to create upload buffer for range upload.");
        }

        fixed (T* pData = data)
        {
            var mappedData = ResourceDatabase.MapResource(uploadHandle.AsResource(), 0, null);
            MemoryUtility.MemCpy(mappedData, pData, sizeInBytes);
            ResourceDatabase.UnmapResource(uploadHandle.AsResource(), 0, null);
        }

        CommandBuffer.CopyBuffer(buffer, uploadHandle, byteOffset, 0, sizeInBytes);
    }

    public Handle<Mesh> CreateMesh(UnsafeList<Vertex> vertices, UnsafeList<uint> indices, bool staticMesh)
    {
        var mesh = ResourceManager.CreateMesh(vertices, indices);
        var r = ResourceManager.GetMeshReference(mesh);
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
            //meshData.CookMeshlets();
            UploadMeshlets(mesh);
            meshData.ReleaseCpuResources();
        }

        return mesh;
    }

    public Handle<Mesh> CreateMesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices, bool staticMesh)
    {
        var vertexList = new UnsafeList<Vertex>(vertices.Length, AllocationHandle.Persistent);
        var indexList = new UnsafeList<uint>(indices.Length, AllocationHandle.Persistent);

        vertexList.CopyFrom(vertices);
        indexList.CopyFrom(indices);

        return CreateMesh(vertexList, indexList, staticMesh);
    }

    /// <summary>
    /// Uploads the mesh data to the GPU.
    /// </summary>
    /// <param name="mesh">The handle point to the mesh buffer</param>
    /// <param name="markMeshStatic">Whether to mark the mesh as static. If it's true, the cpu buffer of the mesh will not be avaliable any more</param>
    public void UploadMesh(Handle<Mesh> mesh, bool markMeshStatic)
    {
        var r = ResourceManager.GetMeshReference(mesh);
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
        var r = ResourceManager.GetMeshReference(mesh);
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
            HeapType = HeapType.Default,
        };
        var verticesDesc = new BufferDesc
        {
            Size = (uint)(meshletData.meshletVertices.Count * sizeof(uint)),
            Stride = sizeof(uint),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Default,
        };
        // Ensure size is multiple of 4 for Raw buffer
        var trianglesSize = (uint)meshletData.meshletTriangles.Count * sizeof(uint);
        var trianglesDesc = new BufferDesc
        {
            Size = trianglesSize,
            Stride = sizeof(uint),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Default,
        };
        var groupsDesc = new BufferDesc
        {
            Size = (uint)(meshletData.groups.Count * sizeof(MeshletGroup)),
            Stride = (uint)sizeof(MeshletGroup),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Default,
        };
        var hierarchyDesc = new BufferDesc
        {
            Size = (uint)(meshletData.hierarchyNodes.Count * sizeof(MeshletHierarchyNode)),
            Stride = (uint)sizeof(MeshletHierarchyNode),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Default,
        };

        meshRef.MeshletBuffer = ResourceAllocator.CreateBuffer(in meshletDesc, "Meshlets");
        meshRef.MeshletVerticesBuffer = ResourceAllocator.CreateBuffer(in verticesDesc, "MeshletVertices");
        meshRef.MeshletTrianglesBuffer = ResourceAllocator.CreateBuffer(in trianglesDesc, "MeshletTriangles");
        meshRef.MeshletGroupBuffer = ResourceAllocator.CreateBuffer(in groupsDesc, "MeshletGroups");
        meshRef.MeshletHierarchyBuffer = ResourceAllocator.CreateBuffer(in hierarchyDesc, "MeshletHierarchy");

        TransitionBarrier(meshRef.MeshletBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);
        TransitionBarrier(meshRef.MeshletVerticesBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);
        TransitionBarrier(meshRef.MeshletTrianglesBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);
        TransitionBarrier(meshRef.MeshletGroupBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);
        TransitionBarrier(meshRef.MeshletHierarchyBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);

        UploadBuffer(meshRef.MeshletBuffer, meshletData.meshlets.AsSpan());
        UploadBuffer(meshRef.MeshletVerticesBuffer, meshletData.meshletVertices.AsSpan());
        UploadBuffer(meshRef.MeshletTrianglesBuffer, meshletData.meshletTriangles.AsSpan());
        UploadBuffer(meshRef.MeshletGroupBuffer, meshletData.groups.AsSpan());
        UploadBuffer(meshRef.MeshletHierarchyBuffer, meshletData.hierarchyNodes.AsSpan());

        TransitionBarrier(meshRef.MeshletBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.ShaderResource, BarrierSync.NonPixelShading | BarrierSync.PixelShading);
        TransitionBarrier(meshRef.MeshletVerticesBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.ShaderResource, BarrierSync.NonPixelShading | BarrierSync.PixelShading);
        TransitionBarrier(meshRef.MeshletTrianglesBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.ShaderResource, BarrierSync.NonPixelShading | BarrierSync.PixelShading);
        TransitionBarrier(meshRef.MeshletGroupBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.ShaderResource, BarrierSync.NonPixelShading | BarrierSync.PixelShading);
        TransitionBarrier(meshRef.MeshletHierarchyBuffer.AsResource(), false, BarrierLayout.Undefined, BarrierAccess.ShaderResource, BarrierSync.NonPixelShading | BarrierSync.PixelShading);
    }

    public void UpdateObjectData(Handle<Mesh> mesh)
    {
        var r = ResourceManager.GetMeshReference(mesh);
        if (r.IsFailure)
        {
            return;
        }

        ref readonly var meshData = ref r.Value;
        var data = new MeshData
        {
            worldBoundsMin = meshData.BoundingBox.Min,
            worldBoundsMax = meshData.BoundingBox.Max,
            vertexBuffer = ResourceDatabase.GetBindlessIndex(meshData.VertexBuffer.AsResource()),
            indexBuffer = ResourceDatabase.GetBindlessIndex(meshData.IndexBuffer.AsResource()),
            meshletBuffer = ResourceDatabase.GetBindlessIndex(meshData.MeshletBuffer.AsResource()),
            meshletVerticesBuffer = ResourceDatabase.GetBindlessIndex(meshData.MeshletVerticesBuffer.AsResource()),
            meshletTrianglesBuffer = ResourceDatabase.GetBindlessIndex(meshData.MeshletTrianglesBuffer.AsResource()),
            meshletGroupBuffer = ResourceDatabase.GetBindlessIndex(meshData.MeshletGroupBuffer.AsResource()),
            meshletHierarchyBuffer = ResourceDatabase.GetBindlessIndex(meshData.MeshletHierarchyBuffer.AsResource()),
            meshletCount = (uint)meshData.MeshletData.meshletCount,
            lodLevelCount = (uint)meshData.MeshletData.lodLevelCount,
            materialSlotCount = (uint)meshData.MeshletData.materialSlotCount,
        };

        var bufferHandle = meshData.MeshDataBuffer.AsResource();

        TransitionBarrier(bufferHandle, false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);
        UploadBuffer(meshData.MeshDataBuffer, data);
        TransitionBarrier(bufferHandle, false, BarrierLayout.Undefined, BarrierAccess.ShaderResource, BarrierSync.PixelShading | BarrierSync.NonPixelShading);
    }

    public Handle<GPUTexture> CreateTexture<T>(scoped in TextureDesc desc, ReadOnlySpan<T> data, string name)
        where T : unmanaged
    {
        var handle = ResourceAllocator.CreateTexture(in desc, name);
        UploadTexture(handle, data);

        return handle;
    }

    public void UploadTexture<T>(Handle<GPUTexture> texture, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        var desc = ResourceDatabase.GetResourceDescription(texture.AsResource()).GetValueOrThrow();
        desc.TextureDescriptor.Format.GetSurfaceInfo(desc.TextureDescriptor.Width, desc.TextureDescriptor.Height, out var rowPitch, out var slicePitch, out _);

        var requiredSize = ResourceDatabase.GetIntermediateResourceSize(texture.AsResource(), 0, 1);
        var uploadDesc = new BufferDesc
        {
            Size = requiredSize,
            Usage = BufferUsage.Upload,
            HeapType = HeapType.Upload,
        };

        var uploadHandle = ResourceManager.CreateTransientBuffer(in uploadDesc);
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

            CommandBuffer.UpdateSubResources(texture.AsResource(), uploadHandle.AsResource(), subresourceData);
        }
    }

    public void DispatchCompute<T>(Handle<ComputeShader> compute, int entryIndex, scoped in T property, uint3 threadGroupCount)
        where T : unmanaged
    {
        ref var shader = ref ResourceManager.GetComputeShaderReference(compute).GetValueOrThrow();

        // TODO: Refactor this into a helper method.
        var (compiledHash, error) = ShaderLibrary.GetCompiledHash(shader.UniqueID, entryIndex);
        if (error.IsFailure)
        {
            // TODO: Fallback to an error material.
            Logger.Debug($"No compiled shader found for compute shader {shader.UniqueID} with entry point {entryIndex}.");
            return;
        }

        var pipelineKey = RHIUtility.CreateComputePipelineKey(compiledHash);

        if (!PipelineLibrary.HasPipelineStateObject(pipelineKey))
        {
            var compiledCacheResult = ShaderLibrary.GetCompiledCache(shader.UniqueID, entryIndex);
            if (compiledCacheResult.IsFailure)
            {
                Logger.Warning($"Failed to load compiled shader cache for compute pipeline {pipelineKey}. Skipping compute dispatch.");
                return;
            }

            var cache = compiledCacheResult.Value;
            Logger.DebugAssert(cache.compiledHash == compiledHash);

            ShaderLibrary.ParseCacheData(cache.byteCode, out _, out var byteCodeOffsets, out var byteCodes);
            Logger.DebugAssert(byteCodeOffsets.Length == 1);

            var psoDes = new ComputePSODesc
            {
                CompiledHash = compiledHash,
                CsCode = byteCodes.Slice((int)byteCodeOffsets[0]),
            };

            PipelineLibrary.CreateComputePipeline(in psoDes).GetValueOrThrow();
        }

        CommandBuffer.SetPipelineState(pipelineKey);


        var propertySpan = MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in property));
        // TODO: Placed resource has 64k alignment requirement, which can waste lots of memory. We can allocate a large buffer and slice it for each dispatch to avoid this issue.
        var propertyBufferDesc = new BufferDesc
        {
            Size = (uint)propertySpan.Length,
            Stride = (uint)sizeof(T),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Upload,
        };
        var properyBuffer = ResourceManager.CreateTransientBuffer(in propertyBufferDesc);

        var mappedData = ResourceDatabase.MapResource(properyBuffer.AsResource(), 0, null);
        Logger.DebugAssert(mappedData != null, "Failed to map property buffer.");

        fixed (byte* pData = propertySpan)
        {
            MemoryUtility.MemCpy(mappedData, pData, (nuint)propertySpan.Length);
        }

        error = ResourceDatabase.UnmapResource(properyBuffer.AsResource(), 0, null);
        Logger.DebugAssert(error.IsSuccess, $"Failed to unmap property buffer: {error}.");

        var pushConstant = new PushConstantsData
        {
            // TODO: Support frame and view buffer.
            frameBuffer = 0,
            viewBuffer = 0,
            propertyBuffer = ResourceDatabase.GetBindlessIndex(properyBuffer.AsResource()),
        };

        CommandBuffer.SetGraphicsRoot32Constants(0, pushConstant.AsUInts());
        CommandBuffer.DispatchCompute(threadGroupCount.x, threadGroupCount.y, threadGroupCount.z);
    }
}
