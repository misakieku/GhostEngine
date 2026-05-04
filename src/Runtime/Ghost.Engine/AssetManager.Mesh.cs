using Ghost.Core;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Runtime.CompilerServices;

namespace Ghost.Engine;

internal unsafe partial class AssetEntry
{
    private sealed unsafe class MeshParsedData
    {
        public MeshContentHeader header;
        public byte* pVertices;
        public byte* pIndices;
        public byte* pMeshlets;
        public byte* pMeshletGroups;
        public byte* pMeshletHierarchyNodes;
        public byte* pMeshletVertices;
        public byte* pMeshletTriangles;
    }

    private static void RegisterMeshCallback()
    {
        s_onCreation[(int)AssetType.Mesh] = static (e) =>
        {
            var handle = e._resourceManager.CreateEmptyMesh();
            e.SetStorage(handle);
        };

        s_onParseRawData[(int)AssetType.Mesh] = static (e) => e.ParseMeshData();
        s_onRecordUpload[(int)AssetType.Mesh] = static (e, ctx) => e.RecordMeshUpload(ctx);
        s_onUploadComplete[(int)AssetType.Mesh] = static (e, ctx) => e.OnMeshUploadComplete(ctx);
        s_onReleaseResource[(int)AssetType.Mesh] = static (e) =>
        {
            var handle = e.GetStorage<Handle<Mesh>>();
            if (handle.IsValid)
            {
                e._resourceManager.ReleaseMesh(handle);
            }
        };
    }

    private Result ParseMeshData()
    {
        var pData = (byte*)_rawData.GetUnsafePtr();
        Logger.DebugAssert(pData != null);

        if (_rawData.Size < (nuint)sizeof(MeshContentHeader))
        {
            return Result.Failure("Mesh content is smaller than the header.");
        }

        var header = Unsafe.ReadUnaligned<MeshContentHeader>(pData);
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

        _parsedObject = new MeshParsedData
        {
            header = header,
            pVertices = pData + header.vertexOffset,
            pIndices = pData + header.indexOffset,
            pMeshlets = pData + header.meshletOffset,
            pMeshletGroups = pData + header.meshletGroupOffset,
            pMeshletHierarchyNodes = pData + header.meshletHierarchyNodeOffset,
            pMeshletVertices = pData + header.meshletVertexOffset,
            pMeshletTriangles = pData + header.meshletTriangleOffset,
        };

        return Result.Success();

        bool ValidateRange(ulong offset, uint count, uint stride)
        {
            var size = (ulong)count * stride;
            return offset <= _rawData.Size && size <= _rawData.Size - (nuint)offset;
        }
    }

    private Result RecordMeshUpload(ResourceStreamingContext context)
    {
        if (_parsedObject is not MeshParsedData data)
        {
            return Result.Failure("Mesh parse data is missing.");
        }

        ref readonly var header = ref data.header;

        var vertexBuffer = CreateBuffer(context, data.pVertices, header.vertexCount, (uint)sizeof(Vertex),
            BufferUsage.Vertex | BufferUsage.ShaderResource | BufferUsage.Raw, "Mesh_VertexBuffer");
        var indexBuffer = CreateBuffer(context, data.pIndices, header.indexCount, sizeof(uint),
            BufferUsage.Index | BufferUsage.ShaderResource | BufferUsage.Raw, "Mesh_IndexBuffer");
        var meshletBuffer = CreateBuffer(context, data.pMeshlets, header.meshletCount, (uint)sizeof(Meshlet),
            BufferUsage.Raw | BufferUsage.ShaderResource, "Mesh_Meshlets");
        var meshletVerticesBuffer = CreateBuffer(context, data.pMeshletVertices, header.meshletVertexCount, sizeof(uint),
            BufferUsage.Raw | BufferUsage.ShaderResource, "Mesh_MeshletVertices");
        var meshletTrianglesBuffer = CreateBuffer(context, data.pMeshletTriangles, header.meshletTriangleCount, sizeof(uint),
            BufferUsage.Raw | BufferUsage.ShaderResource, "Mesh_MeshletTriangles");
        var meshletGroupBuffer = CreateBuffer(context, data.pMeshletGroups, header.meshletGroupCount, (uint)sizeof(MeshletGroup),
            BufferUsage.Raw | BufferUsage.ShaderResource, "Mesh_MeshletGroups");
        var meshletHierarchyBuffer = CreateBuffer(context, data.pMeshletHierarchyNodes, header.meshletHierarchyNodeCount, (uint)sizeof(MeshletHierarchyNode),
            BufferUsage.Raw | BufferUsage.ShaderResource, "Mesh_MeshletHierarchy");

        if (vertexBuffer.IsInvalid || indexBuffer.IsInvalid || meshletBuffer.IsInvalid ||
            meshletVerticesBuffer.IsInvalid || meshletTrianglesBuffer.IsInvalid ||
            meshletGroupBuffer.IsInvalid || meshletHierarchyBuffer.IsInvalid)
        {
            return Result.Failure("Failed to create one or more mesh GPU buffers.");
        }

        var meshData = new MeshData
        {
            worldBoundsMin = header.boundsMin,
            worldBoundsMax = header.boundsMax,
            vertexBuffer = context.ResourceDatabase.GetBindlessIndex(vertexBuffer.AsResource()),
            indexBuffer = context.ResourceDatabase.GetBindlessIndex(indexBuffer.AsResource()),
            meshletBuffer = context.ResourceDatabase.GetBindlessIndex(meshletBuffer.AsResource()),
            meshletVerticesBuffer = context.ResourceDatabase.GetBindlessIndex(meshletVerticesBuffer.AsResource()),
            meshletTrianglesBuffer = context.ResourceDatabase.GetBindlessIndex(meshletTrianglesBuffer.AsResource()),
            meshletGroupBuffer = context.ResourceDatabase.GetBindlessIndex(meshletGroupBuffer.AsResource()),
            meshletHierarchyBuffer = context.ResourceDatabase.GetBindlessIndex(meshletHierarchyBuffer.AsResource()),
            meshletCount = header.meshletCount,
            lodLevelCount = header.lodLevelCount,
            materialSlotCount = header.materialSlotCount,
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
            context.CopyPipeline.GetCommandBuffer(),
            &meshData,
            (nuint)sizeof(MeshData),
            in meshDataBufferDesc,
            "Mesh_MeshDataBuffer");

        if (meshDataBuffer.IsInvalid)
        {
            return Result.Failure("Failed to create mesh data buffer.");
        }

        var newHandle = context.ResourceManager.CreateUploadedMesh(
            vertexBuffer,
            indexBuffer,
            meshletBuffer,
            meshletVerticesBuffer,
            meshletTrianglesBuffer,
            meshletGroupBuffer,
            meshletHierarchyBuffer,
            meshDataBuffer,
            (int)header.vertexCount,
            (int)header.indexCount,
            (int)header.meshletCount,
            (int)header.lodLevelCount,
            (int)header.materialSlotCount,
            new AABB(header.boundsMin, header.boundsMax));

        if (newHandle.IsInvalid)
        {
            return Result.Failure("Failed to register uploaded mesh.");
        }

        var oldHandle = GetStorage<Handle<Mesh>>();
        SetStorage((oldHandle, newHandle));

        return Result.Success();
    }

    private static Handle<GPUBuffer> CreateBuffer(ResourceStreamingContext context, void* pData, uint count, uint stride, BufferUsage usage, string name)
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
            context.CopyPipeline.GetCommandBuffer(),
            pData,
            (nuint)desc.Size,
            in desc,
            name);
    }

    private void OnMeshUploadComplete(ResourceStreamingContext context)
    {
        var (oldHandle, newHandle) = GetStorage<(Handle<Mesh>, Handle<Mesh>)>();
        var actualHandle = context.ResourceManager.ReplaceMesh(oldHandle, newHandle);
        if (actualHandle.IsInvalid)
        {
            SetStorage(oldHandle);
            return;
        }

        var result = context.ResourceManager.GetMeshReference(actualHandle);
        if (result.IsSuccess)
        {
            ref readonly var mesh = ref result.Value;
            context.GraphicsCommandBuffer.Barrier(
                BarrierDesc.Buffer(mesh.VertexBuffer.AsResource(), BarrierSync.VertexShading, BarrierAccess.VertexBuffer | BarrierAccess.ShaderResource),
                BarrierDesc.Buffer(mesh.IndexBuffer.AsResource(), BarrierSync.IndexInput, BarrierAccess.IndexBuffer | BarrierAccess.ShaderResource),
                BarrierDesc.Buffer(mesh.MeshLetBuffer.AsResource(), BarrierSync.AllShading, BarrierAccess.ShaderResource),
                BarrierDesc.Buffer(mesh.MeshletVerticesBuffer.AsResource(), BarrierSync.AllShading, BarrierAccess.ShaderResource),
                BarrierDesc.Buffer(mesh.MeshletTrianglesBuffer.AsResource(), BarrierSync.AllShading, BarrierAccess.ShaderResource),
                BarrierDesc.Buffer(mesh.MeshletGroupBuffer.AsResource(), BarrierSync.AllShading, BarrierAccess.ShaderResource),
                BarrierDesc.Buffer(mesh.MeshletHierarchyBuffer.AsResource(), BarrierSync.AllShading, BarrierAccess.ShaderResource),
                BarrierDesc.Buffer(mesh.MeshDataBuffer.AsResource(), BarrierSync.AllShading, BarrierAccess.ShaderResource));
        }

        SetStorage(actualHandle);

        _rawData.Dispose();
        _parsedObject = null;
    }
}

internal partial class AssetManager
{
    public Handle<Mesh> ResolveMesh(Guid assetID)
    {
        if (assetID == Guid.Empty)
        {
            return Handle<Mesh>.Invalid;
        }

        var entry = GetOrCreateEntry(assetID);
        Logger.DebugAssert(entry.AssetType == AssetType.Mesh);

        return entry.GetStorage<Handle<Mesh>>();
    }

    public int ReleaseMesh(Guid assetID)
    {
        if (assetID == Guid.Empty)
        {
            return 0;
        }

        if (!_entries.TryGetValue(assetID, out var entry) || entry.AssetType != AssetType.Mesh)
        {
            return 0;
        }

        return entry.Release();
    }
}
