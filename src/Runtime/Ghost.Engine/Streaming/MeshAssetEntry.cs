using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Streaming;

// TODO: How can we handle meshlet streaming?
internal unsafe class MeshAssetEntry : AssetEntry, ILoadableAssetEntry, IUploadableAssetEntry
{
    private Handle<Mesh> _actualHandle;
    private Handle<Mesh> _tempHandle;

    private Stream _contentStream = null!;
    private long _contentSize;

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

    public Result OnLoadContent([Owner] Stream contentStream, long contentSize)
    {
        static bool ValidateRange(long offset, long contentSize, int count, uint stride)
        {
            var size = count * stride;
            return offset <= contentSize && size <= contentSize - offset;
        }

        try
        {
            var header = contentStream.Read<MeshContentHeader>();

            if (header.magic != MeshContentHeader.MAGIC || header.version != MeshContentHeader.VERSION)
            {
                contentStream.Dispose();
                return Result.Failure("Unsupported mesh content format.");
            }

            if (header.vertexCount == 0 || header.indexCount == 0 ||
                header.meshletCount == 0 || header.meshletGroupCount == 0 ||
                header.meshletHierarchyNodeCount == 0 || header.meshletVertexCount == 0 ||
                header.meshletTriangleCount == 0)
            {
                contentStream.Dispose();
                return Result.Failure("Mesh content is missing required geometry or meshlet data.");
            }

            if (!ValidateRange(header.vertexOffset, contentSize, header.vertexCount, (uint)sizeof(Vertex)) ||
                !ValidateRange(header.indexOffset, contentSize, header.indexCount, sizeof(uint)) ||
                !ValidateRange(header.meshletOffset, contentSize, header.meshletCount, (uint)sizeof(Meshlet)) ||
                !ValidateRange(header.meshletGroupOffset, contentSize, header.meshletGroupCount, (uint)sizeof(MeshletGroup)) ||
                !ValidateRange(header.meshletHierarchyNodeOffset, contentSize, header.meshletHierarchyNodeCount, (uint)sizeof(MeshletHierarchyNode)) ||
                !ValidateRange(header.meshletVertexOffset, contentSize, header.meshletVertexCount, sizeof(uint)) ||
                !ValidateRange(header.meshletTriangleOffset, contentSize, header.meshletTriangleCount, sizeof(uint)))
            {
                contentStream.Dispose();
                return Result.Failure("Mesh content contains an invalid data range.");
            }

            if (header.materialPartCount > 0 && !ValidateRange(header.materialPartOffset, contentSize, header.materialPartCount, (uint)sizeof(MeshContentMaterialPart)))
            {
                contentStream.Dispose();
                return Result.Failure("Mesh content contains an invalid material part range.");
            }

            _contentStream = contentStream;
            _contentSize = contentSize;
            _header = header;

            return Result.Success();
        }
        catch (Exception)
        {
            contentStream.Dispose();
            throw;
        }
    }

    public Result OnRecordUploadCommands(in ResourceStreamingContext context)
    {
        var headerSize = (uint)sizeof(MeshContentHeader);
        var size = _contentSize - headerSize;
        var desc = new BufferDesc
        {
            Size = (ulong)size,
            Stride = 1,
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Default,
        };

        var meshBuffer = ResourceUtility.CreateBuffer(
            context.ResourceManager,
            context.ResourceDatabase,
            context.ResourceAllocator,
            context.CopyCommandBuffer,
            _contentStream,
            size,
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
            vertexBufferOffset = (uint)_header.vertexOffset - headerSize,
            indexBufferOffset = (uint)_header.indexOffset - headerSize,
            rawBuffer = context.ResourceDatabase.GetBindlessIndex(meshBuffer.AsResource()),
            meshletBufferOffset = (uint)_header.meshletOffset - headerSize,
            meshletVerticesBufferOffset = (uint)_header.meshletVertexOffset - headerSize,
            meshletTrianglesBufferOffset = (uint)_header.meshletTriangleOffset - headerSize,
            meshletGroupBufferOffset = (uint)_header.meshletGroupOffset - headerSize,
            meshletHierarchyBufferOffset = (uint)_header.meshletHierarchyNodeOffset - headerSize,
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
        try
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

            dstMesh.ReleaseCpuResources();
            context.ResourceManager.ReleaseMesh(_tempHandle);
            _tempHandle = Handle<Mesh>.Invalid;

            context.CommandBuffer.Barrier(
                BarrierDesc.Buffer(dstMesh.MeshBuffer, BarrierSync.Copy, BarrierSync.AllShading, BarrierAccess.CopyDest, BarrierAccess.ShaderResource),
                BarrierDesc.Buffer(dstMesh.MeshDataBuffer, BarrierSync.Copy, BarrierSync.AllShading, BarrierAccess.CopyDest, BarrierAccess.ShaderResource));
        }
        finally
        {
            _contentStream.Dispose();
        }
    }
}
