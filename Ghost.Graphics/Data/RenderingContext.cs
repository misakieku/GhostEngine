using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;

namespace Ghost.Graphics.Data;

public unsafe readonly ref struct RenderingContext
{
    private readonly IRenderDevice _device;
    private readonly ICommandBuffer _cmd;
    private readonly ICommandBuffer _copyCmd;
    private readonly ICommandBuffer _computeCmd;
    private readonly IResourceAllocator _resourceAllocator;
    private readonly IResourceDatabase _resourceDatabase;
    private readonly IPipelineLibrary _stateController;

    internal RenderingContext(
        IRenderDevice device,
        ICommandBuffer cmd,
        ICommandBuffer copyCmd,
        ICommandBuffer computeCmd,
        IResourceAllocator resourceAllocator,
        IResourceDatabase resourceDatabase,
        IPipelineLibrary stateController)
    {
        _device = device;
        _cmd = cmd;
        _copyCmd = copyCmd;
        _computeCmd = computeCmd;
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
            _copyCmd.ResourceBarrier(meshData.vertexBuffer.AsResource(), vertexState, ResourceState.CopyDest);
            _resourceDatabase.SetResourceState(meshData.vertexBuffer.AsResource(), ResourceState.CopyDest);
        }

        if (needIndexTransition)
        {
            _copyCmd.ResourceBarrier(meshData.indexBuffer.AsResource(), indexState, ResourceState.CopyDest);
            _resourceDatabase.SetResourceState(meshData.indexBuffer.AsResource(), ResourceState.CopyDest);
        }

        _copyCmd.Upload(meshData.vertexBuffer, meshData.vertices.AsSpan());
        _copyCmd.Upload(meshData.indexBuffer, meshData.indices.AsSpan());

        if (needVertexTransition)
        {
            _copyCmd.ResourceBarrier(meshData.vertexBuffer.AsResource(), ResourceState.CopyDest, vertexState);
            _resourceDatabase.SetResourceState(meshData.vertexBuffer.AsResource(), vertexState);
        }

        if (needIndexTransition)
        {
            _resourceDatabase.SetResourceState(meshData.indexBuffer.AsResource(), indexState);
            _copyCmd.ResourceBarrier(meshData.indexBuffer.AsResource(), ResourceState.CopyDest, indexState);
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
            _copyCmd.ResourceBarrier(texture.AsResource(), sateBefore, ResourceState.CopyDest);
            _resourceDatabase.SetResourceState(texture.AsResource(), ResourceState.CopyDest);
        }

        _copyCmd.Upload(texture, subresourceData);

        if (needTransition)
        {
            _copyCmd.ResourceBarrier(texture.AsResource(), ResourceState.CopyDest, sateBefore);
            _resourceDatabase.SetResourceState(texture.AsResource(), sateBefore);
        }
    }

    public void RenderMesh(Handle<Mesh> mesh, Handle<Material> material)
    {
        _cmd.DrawMesh(mesh, material);
    }

    public void ExecuteCopyCommands()
    {
        _device.CopyQueue.Submit(_copyCmd);
        _device.CopyQueue.WaitIdle();
    }
}