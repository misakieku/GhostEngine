using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Streaming;

internal unsafe class MeshAssetEntry : AssetEntry, ILoadableAssetEntry, IUploadableAssetEntry
{
    private Handle<Mesh> _actualHandle;
    private Handle<Mesh> _tempHandle;

    private AssetPayload _payload;

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
        _payload.Dispose();
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

    public Result OnLoadContent([Owner] ref AssetPayload payload)
    {
        static bool ValidateRange(long offset, long size, long contentSize)
        {
            return offset >= 0 && size >= 0 && offset <= contentSize && size <= contentSize - offset;
        }

        try
        {
            var contentSize = payload.Size;
            if (contentSize < sizeof(MeshContentHeader))
            {
                payload.Dispose();
                return Result.Failure("Mesh payload is too small to contain a valid header.");
            }

            var header = payload.Read<MeshContentHeader>(0);

            if (header.magic != MeshContentHeader.MAGIC || header.version != MeshContentHeader.VERSION)
            {
                payload.Dispose();
                return Result.Failure($"Unsupported mesh content format. Expected magic 0x{MeshContentHeader.MAGIC:X8} v{MeshContentHeader.VERSION}, got 0x{header.magic:X8} v{header.version}.");
            }

            if (header.meshletCount == 0 || header.meshletGroupCount == 0 ||
                header.meshletHierarchyNodeCount == 0 || header.pageCount == 0 ||
                header.rootPageCount == 0)
            {
                payload.Dispose();
                return Result.Failure("Mesh content is missing required meshlet or page data.");
            }

            if (!ValidateRange(header.hierarchyOffset, header.hierarchySize, contentSize) ||
                !ValidateRange(header.groupOffset, header.groupSize, contentSize) ||
                !ValidateRange(header.meshletOffset, header.meshletSize, contentSize) ||
                !ValidateRange(header.pageDirectoryOffset, header.pageDirectorySize, contentSize) ||
                !ValidateRange(header.rootPageOffset, header.rootPageSize, contentSize))
            {
                payload.Dispose();
                return Result.Failure("Mesh content contains an invalid data range.");
            }

            if (header.bulkPagesSize > 0 && !ValidateRange(header.bulkPagesOffset, header.bulkPagesSize, contentSize))
            {
                payload.Dispose();
                return Result.Failure("Mesh content contains an invalid bulk pages range.");
            }

            if (header.materialPartSize > 0 && !ValidateRange(header.materialPartOffset, header.materialPartSize, contentSize))
            {
                payload.Dispose();
                return Result.Failure("Mesh content contains an invalid material part range.");
            }

            if (header.rayTracingProxySize > 0 && !ValidateRange(header.rayTracingProxyOffset, header.rayTracingProxySize, contentSize))
            {
                payload.Dispose();
                return Result.Failure("Mesh content contains an invalid ray tracing proxy range.");
            }

            if (header.hierarchyUncompressedSize < 0 || header.groupUncompressedSize < 0 ||
                header.meshletUncompressedSize < 0 || header.rayTracingProxyUncompressedSize < 0)
            {
                payload.Dispose();
                return Result.Failure("Mesh content contains invalid uncompressed section sizes.");
            }

            _payload = payload;
            _header = header;

            _pageDirectory = new PageDirectoryEntry[_header.pageCount];
            _payload.Slice(_header.pageDirectoryOffset, (int)_header.pageDirectorySize).CopyTo(MemoryMarshal.AsBytes(_pageDirectory.AsSpan()));

            for (var p = 0; p < _pageDirectory.Length; p++)
            {
                ref readonly var dir = ref _pageDirectory[p];
                if (!ValidateRange((long)dir.fileOffset, (long)dir.compressedSize, contentSize) ||
                    dir.uncompressedSize > MeshletPageConstants.PAGE_SIZE)
                {
                    _payload = default;
                    payload.Dispose();
                    return Result.Failure($"Mesh content contains an invalid page directory entry at index {p}.");
                }
            }

            return Result.Success();
        }
        catch (Exception)
        {
            payload.Dispose();
            throw;
        }
    }

    internal bool TryGetPageReadDesc(uint pageIndex, out byte* pSrc, out int compressedSize, out int uncompressedSize, out PageCompressionMethod compressionMethod)
    {
        if (!_payload.IsValid || _pageDirectory == null || pageIndex >= (uint)_pageDirectory.Length)
        {
            pSrc = null;
            compressedSize = 0;
            uncompressedSize = 0;
            compressionMethod = PageCompressionMethod.None;
            return false;
        }

        ref readonly var dir = ref _pageDirectory[pageIndex];
        if (dir.uncompressedSize == 0 || dir.compressedSize == 0)
        {
            pSrc = null;
            compressedSize = 0;
            uncompressedSize = 0;
            compressionMethod = PageCompressionMethod.None;
            return false;
        }

        pSrc = _payload.Pointer + (long)dir.fileOffset;
        compressedSize = (int)dir.compressedSize;
        uncompressedSize = (int)dir.uncompressedSize;
        compressionMethod = dir.compressionMethod;
        return true;
    }

    public int ReadPage(uint pageIndex, Span<byte> dst)
    {
        if (!TryGetPageReadDesc(pageIndex, out var pSrc, out var compressedSize, out var uncompressedSize, out var compressionMethod))
        {
            return 0;
        }

        if (dst.Length < uncompressedSize)
        {
            return 0;
        }

        var srcSpan = new ReadOnlySpan<byte>(pSrc, compressedSize);
        if (compressionMethod == PageCompressionMethod.None)
        {
            srcSpan.CopyTo(dst);
            return uncompressedSize;
        }

        if (compressionMethod == PageCompressionMethod.Zstd)
        {
            if (ZstandardDecoder.TryDecompress(srcSpan, dst[..uncompressedSize], out var bytesWritten) && bytesWritten == uncompressedSize)
            {
                return bytesWritten;
            }

            Logger.Error($"Failed to Zstd-decompress page {pageIndex} for mesh '{AssetName}'.");
            return 0;
        }

        Logger.Error($"Unsupported page compression method {compressionMethod} for mesh '{AssetName}'.");
        return 0;
    }

    private Result DecompressSection(long offset, long compressedSize, long uncompressedSize, bool isCompressed, Span<byte> dst)
    {
        if (uncompressedSize <= 0)
        {
            return Result.Success();
        }

        if (dst.Length < uncompressedSize)
        {
            return Result.Failure("Destination buffer is smaller than uncompressed mesh section size.");
        }

        var srcSpan = _payload.Slice(offset, (int)compressedSize);
        if (!isCompressed)
        {
            srcSpan.CopyTo(dst);
            return Result.Success();
        }

        if (!ZstandardDecoder.TryDecompress(srcSpan, dst[..(int)uncompressedSize], out var bytesWritten) || bytesWritten != uncompressedSize)
        {
            return Result.Failure($"Failed to Zstd-decompress mesh section for '{AssetName}'.");
        }

        return Result.Success();
    }

    public Result OnRecordUploadCommands(scoped in RenderContext context, ICommandBuffer copyCommandBuffer)
    {
        var hierarchyBytes = (uint)_header.hierarchyUncompressedSize;
        var groupBytes = (uint)_header.groupUncompressedSize;
        var meshletBytes = (uint)_header.meshletUncompressedSize;
        var metaBytes = hierarchyBytes + groupBytes + meshletBytes;
        var alignedMetaBytes = (metaBytes + 15u) & ~15u;

        var pageBufferBase = alignedMetaBytes;
        var rootPageBytes = _pageDirectory.Length > 0 ? _pageDirectory[0].uncompressedSize : 0u;
        var rootEndOffset = pageBufferBase + (ulong)rootPageBytes;

        var proxyBytes = (uint)_header.rayTracingProxyUncompressedSize;
        var proxyBaseOffset = (rootEndOffset + 15u) & ~15u;
        var totalGpuBufferSize = proxyBytes > 0 ? (proxyBaseOffset + proxyBytes) : rootEndOffset;

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
            var mask = _header.metadataCompressionMask;
            var res = DecompressSection(_header.hierarchyOffset, _header.hierarchySize, hierarchyBytes, (mask & MeshContentHeader.METADATA_MASK_HIERARCHY) != 0, new Span<byte>(pStaging, (int)hierarchyBytes));
            if (res.IsFailure)
            {
                return res;
            }

            res = DecompressSection(_header.groupOffset, _header.groupSize, groupBytes, (mask & MeshContentHeader.METADATA_MASK_GROUP) != 0, new Span<byte>(pStaging + hierarchyBytes, (int)groupBytes));
            if (res.IsFailure)
            {
                return res;
            }

            res = DecompressSection(_header.meshletOffset, _header.meshletSize, meshletBytes, (mask & MeshContentHeader.METADATA_MASK_MESHLET) != 0, new Span<byte>(pStaging + hierarchyBytes + groupBytes, (int)meshletBytes));
            if (res.IsFailure)
            {
                return res;
            }

            if (rootPageBytes > 0)
            {
                var rootRead = ReadPage(0, new Span<byte>(pStaging + pageBufferBase, (int)rootPageBytes));
                if (rootRead != (int)rootPageBytes)
                {
                    return Result.Failure($"Failed to decode Root Page 0 for mesh '{AssetName}'.");
                }
            }

            if (proxyBytes > 0)
            {
                res = DecompressSection(_header.rayTracingProxyOffset, _header.rayTracingProxySize, proxyBytes, (mask & MeshContentHeader.METADATA_MASK_RAYTRACING_PROXY) != 0, new Span<byte>(pStaging + proxyBaseOffset, (int)proxyBytes));
                if (res.IsFailure)
                {
                    return res;
                }
            }

            var streamingManager = Manager.MeshletStreamingManager;
            var rootPageSpan = new ReadOnlySpan<byte>(pStaging + pageBufferBase, (int)rootPageBytes);
            var globalPageOffset = streamingManager.RegisterMesh(this, (uint)_header.pageCount, rootPageSpan);
            _globalPageTableOffset = globalPageOffset;

            var meshBuffer = ResourceUtility.CreateBuffer(
                context.ResourceManager,
                context.ResourceDatabase,
                context.ResourceAllocator,
                copyCommandBuffer,
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
                positionQuantStep = _header.positionQuantStep,
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
                copyCommandBuffer,
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
                RayTracingProxyVertexBufferOffset = proxyBytes > 0 ? proxyBaseOffset : 0,
                RayTracingProxyIndexBufferOffset = proxyBytes > 0 ? (proxyBaseOffset + (ulong)(_header.rayTracingProxyVertexCount * sizeof(float3))) : 0,
                RayTracingProxyVertexCount = _header.rayTracingProxyVertexCount,
                RayTracingProxyIndexCount = _header.rayTracingProxyIndexCount,
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

    public void OnUploadComplete(scoped in RenderContext context, ICommandBuffer copyCommandBuffer)
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

        copyCommandBuffer.Barrier(
            BarrierDesc.Buffer(dstMesh.MeshBuffer, BarrierSync.Copy, BarrierSync.AllShading, BarrierAccess.CopyDest, BarrierAccess.ShaderResource),
            BarrierDesc.Buffer(dstMesh.MeshDataBuffer, BarrierSync.Copy, BarrierSync.AllShading, BarrierAccess.CopyDest, BarrierAccess.ShaderResource));
    }
}
