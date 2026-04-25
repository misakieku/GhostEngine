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
public readonly unsafe ref struct RenderContext
{
    public required ICommandBuffer CommandBuffer
    {
        get; init;
    }

    public required ResourceManager ResourceManager
    {
        get; init;
    }

    public required IResourceAllocator ResourceAllocator
    {
        get; init;
    }

    public required IResourceDatabase ResourceDatabase
    {
        get; init;
    }

    public required IPipelineLibrary PipelineLibrary
    {
        get; init;
    }

    internal ShaderLibrary ShaderLibrary
    {
        get; init;
    }

    private void TransitionBarrier(Handle<GPUResource> resource, bool isTexture, BarrierLayout newLayout, BarrierAccess newAccess, BarrierSync newSync)
    {
        BarrierDesc desc;
        if (isTexture)
        {
            desc = BarrierDesc.Texture(resource, newSync, newAccess, newLayout);
        }
        else
        {
            desc = BarrierDesc.Buffer(resource, newSync, newAccess);
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

        meshRef.MeshLetBuffer = ResourceAllocator.CreateBuffer(in meshletDesc, "Meshlets");
        meshRef.MeshletVerticesBuffer = ResourceAllocator.CreateBuffer(in verticesDesc, "MeshletVertices");
        meshRef.MeshletTrianglesBuffer = ResourceAllocator.CreateBuffer(in trianglesDesc, "MeshletTriangles");

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
            meshletBuffer = ResourceDatabase.GetBindlessIndex(meshData.MeshLetBuffer.AsResource()),
            meshletVerticesBuffer = ResourceDatabase.GetBindlessIndex(meshData.MeshletVerticesBuffer.AsResource()),
            meshletTrianglesBuffer = ResourceDatabase.GetBindlessIndex(meshData.MeshletTrianglesBuffer.AsResource()),
        };

        var bufferHandle = meshData.MeshDataBuffer.AsResource();

        TransitionBarrier(bufferHandle, false, BarrierLayout.Undefined, BarrierAccess.CopyDest, BarrierSync.Copy);
        UploadBuffer(meshData.MeshDataBuffer, data);
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

    public void DispatchCompute<T>(Handle<ComputeShader> compute, int entryIndex, ref readonly LocalKeywordSet keywordSet, ref readonly T property, uint3 threadGroupCount)
        where T : unmanaged
    {
        ref var shader = ref ResourceManager.GetComputeShaderReference(compute).GetValueOrThrow();

        var entryHash = shader.GetEntryID(entryIndex);
        var variantKey = RHIUtility.CreateShaderVariantKey(entryHash, in keywordSet);

        // TODO: Refactor this into a helper method.
        var (compiledHash, error) = ShaderLibrary.GetCompiledHash(variantKey);
        if (error.IsFailure)
        {
            // TODO: Fallback to an error material.
            Logger.Debug($"No compiled shader found for compute shader {shader.UniqueID} with entry point {entryIndex} and keywords {keywordSet}.");
            return;
        }

        var pipelineKey = RHIUtility.CreateComputePipelineKey(compiledHash);

        if (!PipelineLibrary.HasPipelineStateObject(pipelineKey))
        {
            using var scope = AllocationManager.CreateStackScope();
            var compiledCacheResult = ShaderLibrary.GetCompiledCache(shader.UniqueID, entryIndex, scope.AllocationHandle);
            if (compiledCacheResult.IsFailure)
            {
                // TODO: Fallback to a checkerboard shader.
                throw new InvalidOperationException("Failed to load compiled shader cache for pipeline state object creation.");
            }

            var cache = compiledCacheResult.Value;
            Logger.DebugAssert(cache.compiledHash == compiledHash);

            ShaderLibrary.ParseCacheData(cache.byteCode, out _, out var byteCodeOffsets, out var byteCodes);
            Logger.DebugAssert(byteCodeOffsets.Length == 1);

            var psoDes = new ComputePSODesc
            {
                CompiledHash = compiledHash,
                VariantKey = variantKey,
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
