using Ghost.Core;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

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

        _directCmd.TransitionBarrier(vertexHandle, ResourceState.CopyDest);
        _directCmd.TransitionBarrier(indexHandle, ResourceState.CopyDest);

        _directCmd.UploadBuffer(meshData.VertexBuffer, meshData.Vertices.AsSpan());
        _directCmd.UploadBuffer(meshData.IndexBuffer, meshData.Indices.AsSpan());

        if (staticMesh)
        {
            meshData.ReleaseCpuResources();
            _directCmd.TransitionBarrier(vertexHandle, ResourceState.NonPixelShaderResource);
            _directCmd.TransitionBarrier(indexHandle, ResourceState.NonPixelShaderResource);
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

        _directCmd.TransitionBarrier(meshRef.VertexBuffer.AsResource(), ResourceState.CopyDest);
        _directCmd.TransitionBarrier(meshRef.IndexBuffer.AsResource(), ResourceState.CopyDest);

        _directCmd.UploadBuffer(meshRef.VertexBuffer, meshRef.Vertices.AsSpan());
        _directCmd.UploadBuffer(meshRef.IndexBuffer, meshRef.Indices.AsSpan());

        _directCmd.TransitionBarrier(meshRef.VertexBuffer.AsResource(), ResourceState.NonPixelShaderResource);
        _directCmd.TransitionBarrier(meshRef.IndexBuffer.AsResource(), ResourceState.NonPixelShaderResource);

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

        _directCmd.TransitionBarrier(bufferHandle, ResourceState.CopyDest);
        _directCmd.UploadBuffer(meshData.ObjectDataBuffer, [data]);
        _directCmd.TransitionBarrier(bufferHandle, ResourceState.NonPixelShaderResource | ResourceState.PixelShaderResource);
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
        var desc = ResourceDatabase.GetResourceDescription(texture.AsResource())
            .GetValueOrThrow();

        if (data.Length * sizeof(T) != desc.TextureDescription.GetTotalBytes())
        {
            throw new ArgumentException("Data size does not match texture size.");
        }

        desc.TextureDescription.Format.GetSurfaceInfo(desc.TextureDescription.Width, desc.TextureDescription.Height, out var rowPitch, out var slicePitch, out _);

        _directCmd.TransitionBarrier(texture.AsResource(), ResourceState.CopyDest);

        fixed (T* pData = data)
        {
            var subresourceData = new SubResourceData
            {
                pData = pData,
                rowPitch = rowPitch,
                slicePitch = slicePitch
            };

            _directCmd.UploadTexture(texture, [subresourceData]);
        }
    }
}
