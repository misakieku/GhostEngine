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
using System.Xml.Linq;

namespace Ghost.Engine.Streaming;

// TODO: How can we handle meshlet streaming?
internal unsafe class MeshAssetEntry : AssetEntry, ILoadableAssetEntry, IUploadableAssetEntry
{
    private Handle<Mesh> _actualHandle;
    private Handle<Mesh> _tempHandle;

    private MemoryBlock _rawData;

    private MeshContentHeader _header;

    public MeshAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, Guid[] dependencies)
        : base(manager, resourceDatabase, resourceManager, assetId, AssetType.Mesh, dependencies)
    {
        var mesh = default(Mesh);

        mesh.MeshDataBuffer = resourceDatabase.CreateEmpty().AsBuffer();
        mesh.MeshBuffer = resourceDatabase.CreateEmpty().AsBuffer();

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
        _rawData = contentStream.ReadMemory(AllocationHandle.Persistent);
        if (_rawData.Size < (nuint)sizeof(MeshContentHeader))
        {
            return Result.Failure("Mesh content is too short for header.");
        }

        var pData = (byte*)_rawData.GetUnsafePtr();
        var header = *(MeshContentHeader*)pData;

        bool ValidateRange(long offset, int count, uint stride)
        {
            var size = count * stride;
            return offset <= (long)_rawData.Size && size <= (long)_rawData.Size - offset;
        }

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

        _header = header;

        return Result.Success();
    }

    public Result OnRecordUploadCommands(in ResourceStreamingContext context)
    {
        var desc = new BufferDesc
        {
            Size = (ulong)_rawData.Size,
            Stride = 1,
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Default,
        };

        var meshBuffer = ResourceUtility.CreateBuffer(
            context.ResourceManager,
            context.ResourceDatabase,
            context.ResourceAllocator,
            context.CopyCommandBuffer,
            _rawData.GetUnsafePtr(),
            (nuint)desc.Size,
            in desc,
            "Mesh_Buffer");

        if (meshBuffer.IsInvalid)
        {
            return Result.Failure("Failed to create mesh GPU buffer.");
        }

        var meshData = new MeshData
        {
            worldBoundsMin = _header.boundsMin,
            worldBoundsMax = _header.boundsMax,
            vertexBufferOffset = (uint)_header.vertexOffset,
            indexBufferOffset = (uint)_header.indexOffset,
            rawBuffer = context.ResourceDatabase.GetBindlessIndex(meshBuffer.AsResource()),
            meshletBufferOffset = (uint)_header.meshletOffset,
            meshletVerticesBufferOffset = (uint)_header.meshletVertexOffset,
            meshletTrianglesBufferOffset = (uint)_header.meshletTriangleOffset,
            meshletGroupBufferOffset = (uint)_header.meshletGroupOffset,
            meshletHierarchyBufferOffset = (uint)_header.meshletHierarchyNodeOffset,
            meshletCount = (uint)_header.meshletCount,
            meshletGroupCount = (uint)_header.meshletGroupCount,
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

        var meshDataBuffer = ResourceUtility.CreateBuffer(
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
            MeshBuffer = meshBuffer,
            MeshDataBuffer = meshDataBuffer,
            BoundingBox = new AABB(_header.boundsMin, _header.boundsMax),
            MeshletData = new MeshletMeshData
            {
                meshletCount = _header.meshletCount,
                meshletGroupCount = _header.meshletGroupCount,
                lodLevelCount = _header.lodLevelCount,
                materialSlotCount = _header.materialSlotCount,
            },
            VertexBufferOffset = (ulong)_header.vertexOffset,
            IndexBufferOffset = (ulong)_header.indexOffset,
            MeshletBufferOffset = (ulong)_header.meshletOffset,
            MeshletVerticesBufferOffset = (ulong)_header.meshletVertexOffset,
            MeshletTrianglesBufferOffset = (ulong)_header.meshletTriangleOffset,
            MeshletGroupBufferOffset = (ulong)_header.meshletGroupOffset,
            MeshletHierarchyBufferOffset = (ulong)_header.meshletHierarchyNodeOffset,
        };

        var newHandle = context.ResourceManager.RegisterMesh(ref mesh);
        if (newHandle.IsInvalid)
        {
            return Result.Failure("Failed to register uploaded mesh.");
        }

        _tempHandle = newHandle;

        return Result.Success();
    }

    public void OnUploadComplete(in ResourceStreamingContext context)
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

        dstMesh.MeshBuffer = context.ResourceDatabase.Replace(temp.MeshBuffer.AsResource(), srcMesh.MeshBuffer.AsResource()).AsBuffer();
        dstMesh.MeshDataBuffer = context.ResourceDatabase.Replace(temp.MeshDataBuffer.AsResource(), srcMesh.MeshDataBuffer.AsResource()).AsBuffer();

        context.ResourceManager.ReleaseMesh(_tempHandle);
        _tempHandle = Handle<Mesh>.Invalid;

        context.CommandBuffer.Barrier(
            BarrierDesc.Buffer(dstMesh.MeshBuffer, BarrierSync.Copy, BarrierSync.AllShading, BarrierAccess.CopyDest, BarrierAccess.ShaderResource),
            BarrierDesc.Buffer(dstMesh.MeshDataBuffer, BarrierSync.Copy, BarrierSync.AllShading, BarrierAccess.CopyDest, BarrierAccess.ShaderResource));

        _rawData.Dispose();
    }
}
