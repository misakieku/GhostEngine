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

internal unsafe class MeshAssetEntry : AssetEntry, ILoadableAssetEntry, IUploadableAssetEntry
{
    private Handle<Mesh> _actualHandle;
    private Handle<Mesh> _tempHandle;

    private Stream _contentStream = null!;

    private MeshContentHeader _header;
    private PageDirectoryEntry[] _pageDirectory = null!;
    private uint _globalPageTableOffset;

    public MeshAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, Guid[] dependencies, string assetName)
        : base(manager, resourceDatabase, resourceManager, assetId, AssetType.Mesh, dependencies, assetName)
    {
        var mesh = default(Mesh);

        mesh.MeshDataBuffer = resourceDatabase.CreateEmpty(name: $"{assetName}_MeshDataBuffer").AsBuffer();
        mesh.MeshBuffer = resourceDatabase.CreateEmpty(name: $"{assetName}_MeshBuffer").AsBuffer();

        _actualHandle = resourceManager.RegisterMesh(ref mesh);
    }

    protected override void OnReleaseResource()
    {
        Manager.MeshletStreamingManager.UnregisterMesh(_globalPageTableOffset);

        ResourceManager.ReleaseMesh(_actualHandle);
        if (_tempHandle.IsValid)
        {
            ResourceManager.ReleaseMesh(_tempHandle);
        }
        _contentStream?.Dispose();
        _contentStream = null!;
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
        static bool ValidateRange(long offset, long size, long contentSize)
        {
            return offset >= 0 && size >= 0 && offset <= contentSize && size <= contentSize - offset;
        }

        try
        {
            var header = contentStream.Read<MeshContentHeader>();

            if (header.magic != MeshContentHeader.MAGIC || header.version != MeshContentHeader.VERSION)
            {
                contentStream.Dispose();
                return Result.Failure($"Unsupported mesh content format. Expected magic 0x{MeshContentHeader.MAGIC:X8} v{MeshContentHeader.VERSION}, got 0x{header.magic:X8} v{header.version}.");
            }

            if (header.meshletCount == 0 || header.meshletGroupCount == 0 ||
                header.meshletHierarchyNodeCount == 0 || header.pageCount == 0 ||
                header.rootPageCount == 0)
            {
                contentStream.Dispose();
                return Result.Failure("Mesh content is missing required meshlet or page data.");
            }

            if (!ValidateRange(header.hierarchyOffset, header.hierarchySize, contentSize) ||
                !ValidateRange(header.groupOffset, header.groupSize, contentSize) ||
                !ValidateRange(header.meshletOffset, header.meshletSize, contentSize) ||
                !ValidateRange(header.pageDirectoryOffset, header.pageDirectorySize, contentSize) ||
                !ValidateRange(header.rootPageOffset, header.rootPageSize, contentSize))
            {
                contentStream.Dispose();
                return Result.Failure("Mesh content contains an invalid data range.");
            }

            if (header.bulkPagesSize > 0 && !ValidateRange(header.bulkPagesOffset, header.bulkPagesSize, contentSize))
            {
                contentStream.Dispose();
                return Result.Failure("Mesh content contains an invalid bulk pages range.");
            }

            if (header.materialPartSize > 0 && !ValidateRange(header.materialPartOffset, header.materialPartSize, contentSize))
            {
                contentStream.Dispose();
                return Result.Failure("Mesh content contains an invalid material part range.");
            }

            _contentStream = contentStream;
            _header = header;

            _pageDirectory = new PageDirectoryEntry[_header.pageCount];
            _contentStream.Position = _header.pageDirectoryOffset;
            _contentStream.ReadExactly(MemoryMarshal.AsBytes(_pageDirectory.AsSpan()));

            return Result.Success();
        }
        catch (Exception)
        {
            contentStream.Dispose();
            throw;
        }
    }

    public int ReadPage(uint pageIndex, Span<byte> dst)
    {
        if (_contentStream == null || _pageDirectory == null || pageIndex >= (uint)_pageDirectory.Length)
        {
            return 0;
        }

        ref readonly var dir = ref _pageDirectory[pageIndex];
        var bytesToRead = (int)dir.uncompressedSize;
        if (bytesToRead <= 0 || dst.Length < bytesToRead)
        {
            return 0;
        }

        lock (_contentStream)
        {
            _contentStream.Position = (long)dir.fileOffset;
            _contentStream.ReadExactly(dst[..bytesToRead]);
        }

        return bytesToRead;
    }

    public Result OnRecordUploadCommands(in ResourceStreamingContext context)
    {
        var hierarchyBytes = (uint)_header.hierarchySize;
        var groupBytes = (uint)_header.groupSize;
        var meshletBytes = (uint)_header.meshletSize;
        var metaBytes = hierarchyBytes + groupBytes + meshletBytes;
        var alignedMetaBytes = (metaBytes + 15u) & ~15u;

        var pageBufferBase = alignedMetaBytes;
        var rootPageBytes = (uint)_header.rootPageSize;

        var streamingManager = Manager.MeshletStreamingManager;
        var totalGpuBufferSize = pageBufferBase + (ulong)rootPageBytes;

        var desc = new BufferDesc
        {
            Size = totalGpuBufferSize,
            Stride = 1,
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Default,
        };

        // Allocate staging memory to assemble GPU buffer
        var pStaging = (byte*)NativeMemory.AllocZeroed((nuint)totalGpuBufferSize);
        try
        {
            // 1. Hierarchy nodes
            _contentStream.Position = _header.hierarchyOffset;
            _contentStream.ReadExactly(new Span<byte>(pStaging, (int)hierarchyBytes));

            // 2. Groups
            _contentStream.Position = _header.groupOffset;
            _contentStream.ReadExactly(new Span<byte>(pStaging + hierarchyBytes, (int)groupBytes));

            // 3. Meshlets
            _contentStream.Position = _header.meshletOffset;
            _contentStream.ReadExactly(new Span<byte>(pStaging + hierarchyBytes + groupBytes, (int)meshletBytes));

            // 4. Root Page 0
            _contentStream.Position = _header.rootPageOffset;
            _contentStream.ReadExactly(new Span<byte>(pStaging + pageBufferBase, (int)rootPageBytes));

            var globalPageOffset = 0u;
            var rootPageSpan = new ReadOnlySpan<byte>(pStaging + pageBufferBase, (int)rootPageBytes);
            globalPageOffset = streamingManager.RegisterMesh(this, (uint)_header.pageCount, rootPageSpan);
            _globalPageTableOffset = globalPageOffset;

            var meshBuffer = ResourceUtility.CreateBuffer(
                context.ResourceManager,
                context.ResourceDatabase,
                context.ResourceAllocator,
                context.CopyCommandBuffer,
                pStaging,
                (nuint)totalGpuBufferSize,
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
                globalPageTableOffset = globalPageOffset,
                pageCount = (uint)_header.pageCount,
                rawBuffer = context.ResourceDatabase.GetBindlessIndex(meshBuffer.AsResource()),
                meshletHierarchyBufferOffset = 0,
                meshletGroupBufferOffset = hierarchyBytes,
                meshletBufferOffset = hierarchyBytes + groupBytes,
                meshletVerticesBufferOffset = pageBufferBase,
                meshletTrianglesBufferOffset = pageBufferBase,
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
                VertexCount = 0,
                IndexCount = 0,
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
                MeshletBufferOffset = hierarchyBytes + groupBytes,
                MeshletVerticesBufferOffset = pageBufferBase,
                MeshletTrianglesBufferOffset = pageBufferBase,
                MeshletGroupBufferOffset = hierarchyBytes,
                MeshletHierarchyBufferOffset = 0,
            };

            var newHandle = context.ResourceManager.RegisterMesh(ref mesh);
            if (newHandle.IsInvalid)
            {
                return Result.Failure("Failed to register uploaded mesh.");
            }

            _tempHandle = newHandle;

            return Result.Success();
        }
        finally
        {
            NativeMemory.Free(pStaging);
        }
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

        dstMesh.ReleaseCpuResources();
        context.ResourceManager.ReleaseMesh(_tempHandle);
        _tempHandle = Handle<Mesh>.Invalid;

        context.CommandBuffer.Barrier(
            BarrierDesc.Buffer(dstMesh.MeshBuffer, BarrierSync.Copy, BarrierSync.AllShading, BarrierAccess.CopyDest, BarrierAccess.ShaderResource),
            BarrierDesc.Buffer(dstMesh.MeshDataBuffer, BarrierSync.Copy, BarrierSync.AllShading, BarrierAccess.CopyDest, BarrierAccess.ShaderResource));
    }
}
