using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Streaming;

internal unsafe class MeshAssetEntry : AssetEntry, ILoadableAssetEntry, IUploadableAssetEntry
{
    private Handle<Mesh> _actualHandle;
    private Handle<Mesh> _tempHandle;

    private MemoryBlock _rawData;

    private MeshContentHeader _header;
    private byte* _pVertices;
    private byte* _pIndices;
    private byte* _pMeshlets;
    private byte* _pMeshletGroups;
    private byte* _pMeshletHierarchyNodes;
    private byte* _pMeshletVertices;
    private byte* _pMeshletTriangles;

    public MeshAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, Guid[] dependencies)
        : base(manager, resourceDatabase, resourceManager, assetId, AssetType.Mesh, dependencies)
    {
        var mesh = default(Mesh);

        mesh.VertexBuffer = resourceDatabase.CreateEmpty().AsBuffer();
        mesh.IndexBuffer = resourceDatabase.CreateEmpty().AsBuffer();
        mesh.MeshDataBuffer = resourceDatabase.CreateEmpty().AsBuffer();
        mesh.MeshletBuffer = resourceDatabase.CreateEmpty().AsBuffer();
        mesh.MeshletGroupBuffer = resourceDatabase.CreateEmpty().AsBuffer();
        mesh.MeshletHierarchyBuffer = resourceDatabase.CreateEmpty().AsBuffer();
        mesh.MeshletVerticesBuffer = resourceDatabase.CreateEmpty().AsBuffer();
        mesh.MeshletTrianglesBuffer = resourceDatabase.CreateEmpty().AsBuffer();

        _actualHandle = resourceManager.RegisterMesh(ref mesh);
    }

    protected override void OnReleaseResource()
    {
        ResourceManager.ReleaseMesh(_actualHandle);
        if (_tempHandle.IsValid)
        {
            ResourceManager.ReleaseMesh(_tempHandle);
        }
    }

    public override void ReadAssetData(Span<byte> dst)
    {
        Logger.DebugAssert(dst.Length == sizeof(Handle<Mesh>));
        Logger.DebugAssert(_actualHandle.IsValid);

        ref var address = ref MemoryMarshal.GetReference(dst);
        Unsafe.WriteUnaligned(ref address, _actualHandle);
    }

    public override void ReadAssetData<T>(ref T dst)
    {
        Logger.DebugAssert(typeof(T) == typeof(Handle<Mesh>));
        Logger.DebugAssert(_actualHandle.IsValid);

        dst = Unsafe.BitCast<Handle<Mesh>, T>(_actualHandle);
    }

    public Result OnLoadContent(Stream contentStream)
    {
        bool ValidateRange(long offset, int count, uint stride)
        {
            var size = count * stride;
            return offset <= contentStream.Length && size <= contentStream.Length - offset;
        }

        var header = contentStream.Read<MeshContentHeader>();
        if (header.magic != MeshContentHeader.MAGIC || header.version != MeshContentHeader.VERSION)
        {
            return Result.Failure("Unsupported mesh content format.");
        }

        if (header.vertexCount == 0 || header.indexCount == 0 ||
            header.meshletCount == 0 || header.meshletGroupCount == 0 ||
            header.meshletHierarchyNodeCount == 0 || header.meshletVertexCount == 0 ||
            header.meshletTriangleCount == 0)
        {
            return Result.Failure("Mesh content is missing required geometry or meshlet data.");
        }

        if (!ValidateRange(header.vertexOffset, header.vertexCount, (uint)sizeof(Vertex)) ||
            !ValidateRange(header.indexOffset, header.indexCount, sizeof(uint)) ||
            !ValidateRange(header.meshletOffset, header.meshletCount, (uint)sizeof(Meshlet)) ||
            !ValidateRange(header.meshletGroupOffset, header.meshletGroupCount, (uint)sizeof(MeshletGroup)) ||
            !ValidateRange(header.meshletHierarchyNodeOffset, header.meshletHierarchyNodeCount, (uint)sizeof(MeshletHierarchyNode)) ||
            !ValidateRange(header.meshletVertexOffset, header.meshletVertexCount, sizeof(uint)) ||
            !ValidateRange(header.meshletTriangleOffset, header.meshletTriangleCount, sizeof(uint)))
        {
            return Result.Failure("Mesh content contains an invalid data range.");
        }

        if (header.materialPartCount > 0 && !ValidateRange(header.materialPartOffset, header.materialPartCount, (uint)sizeof(MeshContentMaterialPart)))
        {
            return Result.Failure("Mesh content contains an invalid material part range.");
        }

        contentStream.Position = 0;

        // TODO: Replace the full persistent payload with range-based bounded staging.
        _rawData = contentStream.ReadMemory(AllocationHandle.Persistent);
        var pData = (byte*)_rawData.GetUnsafePtr();

        _header = header;
        _pVertices = pData + header.vertexOffset;
        _pIndices = pData + header.indexOffset;
        _pMeshlets = pData + header.meshletOffset;
        _pMeshletGroups = pData + header.meshletGroupOffset;
        _pMeshletHierarchyNodes = pData + header.meshletHierarchyNodeOffset;
        _pMeshletVertices = pData + header.meshletVertexOffset;
        _pMeshletTriangles = pData + header.meshletTriangleOffset;

        return Result.Success();
    }

    private static Handle<GPUBuffer> CreateBuffer(ResourceStreamingContext context, void* pData, int count, uint stride, BufferUsage usage, string name)
    {
        var desc = new BufferDesc
        {
            Size = (ulong)count * stride,
            Stride = stride,
            Usage = usage,
            HeapType = HeapType.Default,
        };

        return RenderingUtility.CreateBuffer(
            context.ResourceManager,
            context.ResourceDatabase,
            context.ResourceAllocator,
            context.CopyCommandBuffer,
            pData,
            (nuint)desc.Size,
            in desc,
            name);
    }

    public Result OnRecordUploadCommands(ResourceStreamingContext context)
    {
        var vertexBuffer = CreateBuffer(context, _pVertices, _header.vertexCount, (uint)sizeof(Vertex),
            BufferUsage.Vertex | BufferUsage.ShaderResource | BufferUsage.Raw, "Mesh_VertexBuffer");
        var indexBuffer = CreateBuffer(context, _pIndices, _header.indexCount, sizeof(uint),
            BufferUsage.Index | BufferUsage.ShaderResource | BufferUsage.Raw, "Mesh_IndexBuffer");
        var meshletBuffer = CreateBuffer(context, _pMeshlets, _header.meshletCount, (uint)sizeof(Meshlet),
            BufferUsage.Raw | BufferUsage.ShaderResource, "Mesh_Meshlets");
        var meshletVerticesBuffer = CreateBuffer(context, _pMeshletVertices, _header.meshletVertexCount, sizeof(uint),
            BufferUsage.Raw | BufferUsage.ShaderResource, "Mesh_MeshletVertices");
        var meshletTrianglesBuffer = CreateBuffer(context, _pMeshletTriangles, _header.meshletTriangleCount, sizeof(uint),
            BufferUsage.Raw | BufferUsage.ShaderResource, "Mesh_MeshletTriangles");
        var meshletGroupBuffer = CreateBuffer(context, _pMeshletGroups, _header.meshletGroupCount, (uint)sizeof(MeshletGroup),
            BufferUsage.Raw | BufferUsage.ShaderResource, "Mesh_MeshletGroups");
        var meshletHierarchyBuffer = CreateBuffer(context, _pMeshletHierarchyNodes, _header.meshletHierarchyNodeCount, (uint)sizeof(MeshletHierarchyNode),
            BufferUsage.Raw | BufferUsage.ShaderResource, "Mesh_MeshletHierarchy");

        if (vertexBuffer.IsInvalid || indexBuffer.IsInvalid || meshletBuffer.IsInvalid ||
            meshletVerticesBuffer.IsInvalid || meshletTrianglesBuffer.IsInvalid ||
            meshletGroupBuffer.IsInvalid || meshletHierarchyBuffer.IsInvalid)
        {
            return Result.Failure("Failed to create one or more mesh GPU buffers.");
        }

        var meshData = new MeshData
        {
            worldBoundsMin = _header.boundsMin,
            worldBoundsMax = _header.boundsMax,
            vertexBuffer = context.ResourceDatabase.GetBindlessIndex(vertexBuffer.AsResource()),
            indexBuffer = context.ResourceDatabase.GetBindlessIndex(indexBuffer.AsResource()),
            meshletBuffer = context.ResourceDatabase.GetBindlessIndex(meshletBuffer.AsResource()),
            meshletVerticesBuffer = context.ResourceDatabase.GetBindlessIndex(meshletVerticesBuffer.AsResource()),
            meshletTrianglesBuffer = context.ResourceDatabase.GetBindlessIndex(meshletTrianglesBuffer.AsResource()),
            meshletGroupBuffer = context.ResourceDatabase.GetBindlessIndex(meshletGroupBuffer.AsResource()),
            meshletHierarchyBuffer = context.ResourceDatabase.GetBindlessIndex(meshletHierarchyBuffer.AsResource()),
            meshletCount = (uint)_header.meshletCount,
            lodLevelCount = (uint)_header.lodLevelCount,
            materialSlotCount = (uint)_header.materialSlotCount,
        };

        var meshDataBufferDesc = new BufferDesc
        {
            Size = (ulong)sizeof(MeshData),
            Stride = (uint)sizeof(MeshData),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Default,
        };

        var meshDataBuffer = RenderingUtility.CreateBuffer(
            context.ResourceManager,
            context.ResourceDatabase,
            context.ResourceAllocator,
            context.CopyCommandBuffer,
            &meshData,
            (nuint)sizeof(MeshData),
            in meshDataBufferDesc,
            "Mesh_MeshDataBuffer");

        if (meshDataBuffer.IsInvalid)
        {
            return Result.Failure("Failed to create mesh data buffer.");
        }

        var mesh = new Mesh
        {
            IsMeshDataDirty = true,
            VertexCount = _header.vertexCount,
            IndexCount = _header.indexCount,
            VertexBuffer = vertexBuffer,
            IndexBuffer = indexBuffer,
            MeshletBuffer = meshletBuffer,
            MeshletVerticesBuffer = meshletVerticesBuffer,
            MeshletTrianglesBuffer = meshletTrianglesBuffer,
            MeshletGroupBuffer = meshletGroupBuffer,
            MeshletHierarchyBuffer = meshletHierarchyBuffer,
            MeshDataBuffer = meshDataBuffer,
            BoundingBox = new AABB(_header.boundsMin, _header.boundsMax),
            MeshletData = new MeshletMeshData
            {
                meshletCount = _header.meshletCount,
                lodLevelCount = _header.lodLevelCount,
                materialSlotCount = _header.materialSlotCount,
            }
        };

        var newHandle = context.ResourceManager.RegisterMesh(ref mesh);
        if (newHandle.IsInvalid)
        {
            return Result.Failure("Failed to register uploaded mesh.");
        }

        _tempHandle = newHandle;

        return Result.Success();
    }

    public void OnUploadComplete(ResourceStreamingContext context)
    {
        var (dstMeshRef, dstError) = context.ResourceManager.GetMeshReference(_actualHandle);
        var (srcMeshRef, srcError) = context.ResourceManager.GetMeshReference(_tempHandle);
        if (dstError.IsFailure || srcError.IsFailure)
        {
            return;
        }

        ref var dstMesh = ref dstMeshRef.Get();
        ref var srcMesh = ref srcMeshRef.Get();

        var temp = dstMesh;

        Logger.DebugAssert(!dstMesh.Vertices.IsCreated);
        Logger.DebugAssert(!dstMesh.Indices.IsCreated);

        dstMesh = srcMesh.Clone();

        dstMesh.IsMeshDataDirty = false;

        dstMesh.VertexBuffer = context.ResourceDatabase.Replace(temp.VertexBuffer.AsResource(), srcMesh.VertexBuffer.AsResource()).AsBuffer();
        dstMesh.IndexBuffer = context.ResourceDatabase.Replace(temp.IndexBuffer.AsResource(), srcMesh.IndexBuffer.AsResource()).AsBuffer();
        dstMesh.MeshDataBuffer = context.ResourceDatabase.Replace(temp.MeshDataBuffer.AsResource(), srcMesh.MeshDataBuffer.AsResource()).AsBuffer();

        dstMesh.MeshletBuffer = context.ResourceDatabase.Replace(temp.MeshletBuffer.AsResource(), srcMesh.MeshletBuffer.AsResource()).AsBuffer();
        dstMesh.MeshletGroupBuffer = context.ResourceDatabase.Replace(temp.MeshletGroupBuffer.AsResource(), srcMesh.MeshletGroupBuffer.AsResource()).AsBuffer();
        dstMesh.MeshletHierarchyBuffer = context.ResourceDatabase.Replace(temp.MeshletHierarchyBuffer.AsResource(), srcMesh.MeshletHierarchyBuffer.AsResource()).AsBuffer();
        dstMesh.MeshletVerticesBuffer = context.ResourceDatabase.Replace(temp.MeshletVerticesBuffer.AsResource(), srcMesh.MeshletVerticesBuffer.AsResource()).AsBuffer();
        dstMesh.MeshletTrianglesBuffer = context.ResourceDatabase.Replace(temp.MeshletTrianglesBuffer.AsResource(), srcMesh.MeshletTrianglesBuffer.AsResource()).AsBuffer();

        context.ResourceManager.ReleaseMesh(_tempHandle);
        _tempHandle = Handle<Mesh>.Invalid;

        context.CommandBuffer.Barrier(
            BarrierDesc.Buffer(dstMesh.VertexBuffer, BarrierSync.Copy, BarrierSync.VertexShading, BarrierAccess.CopyDest, BarrierAccess.VertexBuffer | BarrierAccess.ShaderResource),
            BarrierDesc.Buffer(dstMesh.IndexBuffer, BarrierSync.Copy, BarrierSync.IndexInput, BarrierAccess.CopyDest, BarrierAccess.IndexBuffer | BarrierAccess.ShaderResource),
            BarrierDesc.Buffer(dstMesh.MeshletBuffer, BarrierSync.Copy, BarrierSync.AllShading, BarrierAccess.CopyDest, BarrierAccess.ShaderResource),
            BarrierDesc.Buffer(dstMesh.MeshletVerticesBuffer, BarrierSync.Copy, BarrierSync.AllShading, BarrierAccess.CopyDest, BarrierAccess.ShaderResource),
            BarrierDesc.Buffer(dstMesh.MeshletTrianglesBuffer, BarrierSync.Copy, BarrierSync.AllShading, BarrierAccess.CopyDest, BarrierAccess.ShaderResource),
            BarrierDesc.Buffer(dstMesh.MeshletGroupBuffer, BarrierSync.Copy, BarrierSync.AllShading, BarrierAccess.CopyDest, BarrierAccess.ShaderResource),
            BarrierDesc.Buffer(dstMesh.MeshletHierarchyBuffer, BarrierSync.Copy, BarrierSync.AllShading, BarrierAccess.CopyDest, BarrierAccess.ShaderResource),
            BarrierDesc.Buffer(dstMesh.MeshDataBuffer, BarrierSync.Copy, BarrierSync.AllShading, BarrierAccess.CopyDest, BarrierAccess.ShaderResource));

        _rawData.Dispose();
        _pVertices = null;
        _pIndices = null;
        _pMeshlets = null;
        _pMeshletGroups = null;
        _pMeshletHierarchyNodes = null;
        _pMeshletVertices = null;
        _pMeshletTriangles = null;
    }
}
