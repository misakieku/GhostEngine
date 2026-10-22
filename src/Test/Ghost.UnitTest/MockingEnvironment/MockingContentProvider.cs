using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Engine.Streaming;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Collections.Concurrent;

namespace Ghost.UnitTest.MockingEnvironment;

internal class MockingContentProvider : IContentProvider
{
    public class MockAssetData
    {
        public AssetType type;
        public byte[] data = Array.Empty<byte>();
        public Guid[] dependencies = Array.Empty<Guid>();

        // This is crucial for multi-threaded testing: we can inject random or fixed
        // delays to ensure our locking and state machines actually get stressed.
        public int readDelayMs = 0;
    }

    private readonly ConcurrentDictionary<Guid, MockAssetData> _assets = new();

    public IReadOnlyList<ShaderCatalogEntry> ShaderCatalog { get; init; } = Array.Empty<ShaderCatalogEntry>();

    public void AddMockAsset(Guid guid, MockAssetData data)
    {
        _assets[guid] = data;
    }

    /// <summary>
    /// Helper method to create a valid dummy texture byte stream that the AssetEntry can parse.
    /// </summary>
    public unsafe void AddMockTexture(Guid guid, uint width = 4, uint height = 4, int readDelayMs = 0)
    {
        var header = new TextureContentHeader
        {
            magic = TextureContentHeader.MAGIC,
            version = TextureContentHeader.VERSION,
            width = width,
            height = height,
            bpc = 8,
            mipLevels = 1,
            dimension = Ghost.Core.TextureDimension.Texture2D, // Texture2D
            colorComponents = 4
        };

        // Header size is strictly 64 bytes due to [StructLayout(LayoutKind.Sequential, Size = 64)]
        var headerSize = 64;
        var pixelDataSize = width * height * 4;

        var buffer = new byte[headerSize + pixelDataSize];

        fixed (byte* pBuffer = buffer)
        {
            *(TextureContentHeader*)pBuffer = header;
            // The rest of the array remains 0 (black/transparent pixels) which is fine for tests
        }

        AddMockAsset(guid, new MockAssetData
        {
            type = AssetType.Texture,
            data = buffer,
            readDelayMs = readDelayMs
        });
    }

    public unsafe void AddMockMesh(Guid guid, int readDelayMs = 0)
    {
        var vertices = new[]
        {
            new Vertex { position = new float3(0, 0, 0), normal = new float3(0, 1, 0), tangent = new float4(1, 0, 0, 1), uv = new float2(0, 0), color = new Color128(1, 1, 1, 1) },
            new Vertex { position = new float3(1, 0, 0), normal = new float3(0, 1, 0), tangent = new float4(1, 0, 0, 1), uv = new float2(1, 0), color = new Color128(1, 1, 1, 1) },
            new Vertex { position = new float3(0, 1, 0), normal = new float3(0, 1, 0), tangent = new float4(1, 0, 0, 1), uv = new float2(0, 1), color = new Color128(1, 1, 1, 1) },
        };
        var indices = new uint[] { 0, 1, 2 };
        var materialParts = new[]
        {
            new MeshContentMaterialPart { materialIndex = 0, indexStart = 0, indexCount = 3, vertexStart = 0, vertexCount = 3 }
        };
        var meshlets = new[]
        {
            new Meshlet
            {
                boundingSphere = new SphereBounds(new float3(0.5f, 0.5f, 0), 1.0f),
                parentBoundingSphere = new SphereBounds(new float3(0.5f, 0.5f, 0), 1.0f),
                boundingBox = new AABB(new float3(0, 0, 0), new float3(1, 1, 0)),
                vertexOffset = (uint)(sizeof(PageHeader) + sizeof(Meshlet)),
                triangleOffset = (uint)(sizeof(PageHeader) + sizeof(Meshlet) + 3 * sizeof(PackedVertex)),
                pageIndex = 0,
                clusterError = 0,
                parentError = 0,
                vertexCount = 3,
                triangleCount = 1,
                localMaterialIndex = 0,
                lodLevel = 0
            }
        };
        var groups = new[]
        {
            new MeshletGroup
            {
                boundingSphere = new SphereBounds(new float3(0.5f, 0.5f, 0), 1.0f),
                boundingBox = new AABB(new float3(0, 0, 0), new float3(1, 1, 0)),
                parentError = 0,
                meshletStartIndex = 0,
                meshletCount = 1,
                lodLevel = 0,
                pageIndex = 0,
            }
        };
        var hierarchy = new[]
        {
            new MeshletHierarchyNode
            {
                bounds = new SphereBounds(new float3(0.5f, 0.5f, 0), 1.0f),
                error = 0,
                groupIndex = 0,
                childOffset = 0,
                childCount = 0,
            }
        };
        var meshletVertices = new PackedVertex[]
        {
            PackedVertex.Pack(in vertices[0]),
            PackedVertex.Pack(in vertices[1]),
            PackedVertex.Pack(in vertices[2])
        };
        var meshletTriangles = new uint[] { 0 | (1u << 8) | (2u << 16) };

        using var stream = new MemoryStream();
        var header = new MeshContentHeader
        {
            magic = MeshContentHeader.MAGIC,
            version = MeshContentHeader.VERSION,
            pageCount = 1,
            rootPageCount = 1,
            rootMeshletCount = 1,
            meshletCount = meshlets.Length,
            meshletGroupCount = groups.Length,
            meshletHierarchyNodeCount = hierarchy.Length,
            materialSlotCount = 1,
            lodLevelCount = 1,
            boundsMin = new float3(0, 0, 0),
            boundsMax = new float3(1, 1, 0),
            boundingSphereCenter = new float3(0.5f, 0.5f, 0),
            boundingSphereRadius = 1.0f,
        };

        var pageHeader = new PageHeader
        {
            magic = PageHeader.MAGIC,
            meshletCount = 1,
            vertexCount = (ushort)meshletVertices.Length,
            triangleCount = (uint)meshletTriangles.Length,
            meshletOffset = (uint)sizeof(PageHeader),
            vertexOffset = (uint)(sizeof(PageHeader) + sizeof(Meshlet)),
            triangleOffset = (uint)(sizeof(PageHeader) + sizeof(Meshlet) + meshletVertices.Length * sizeof(PackedVertex)),
            pageIndex = 0,
            reserved = 0,
        };
        var pageDirectory = new[]
        {
            new PageDirectoryEntry
            {
                fileOffset = 0,
                compressedSize = (uint)(sizeof(PageHeader) + sizeof(Meshlet) + meshletVertices.Length * sizeof(PackedVertex) + meshletTriangles.Length * sizeof(uint)),
                uncompressedSize = (uint)(sizeof(PageHeader) + sizeof(Meshlet) + meshletVertices.Length * sizeof(PackedVertex) + meshletTriangles.Length * sizeof(uint)),
                meshletStartIndex = 0,
                meshletCount = 1,
                lodLevel = 0,
                isRootPage = 1,
            }
        };

        stream.Write(header);
        header.hierarchyOffset = stream.Position; stream.Write(hierarchy); header.hierarchySize = sizeof(MeshletHierarchyNode);
        header.groupOffset = stream.Position; stream.Write(groups); header.groupSize = sizeof(MeshletGroup);
        header.meshletOffset = stream.Position; stream.Write(meshlets); header.meshletSize = sizeof(Meshlet);
        var pageDirPos = stream.Position;
        header.pageDirectoryOffset = stream.Position; stream.Write(pageDirectory); header.pageDirectorySize = sizeof(PageDirectoryEntry);
        header.rootPageOffset = stream.Position;
        stream.Write(pageHeader);
        stream.Write(meshlets);
        stream.Write(meshletVertices);
        stream.Write(meshletTriangles);
        header.rootPageSize = stream.Position - header.rootPageOffset;
        header.materialPartOffset = stream.Position; stream.Write(materialParts); header.materialPartSize = sizeof(MeshContentMaterialPart);

        pageDirectory[0].fileOffset = (ulong)header.rootPageOffset;
        var endPos = stream.Position;
        stream.Position = pageDirPos;
        stream.Write(pageDirectory);
        stream.Position = 0;
        stream.Write(header);
        stream.Position = endPos;

        AddMockAsset(guid, new MockAssetData
        {
            type = AssetType.Mesh,
            data = stream.ToArray(),
            readDelayMs = readDelayMs
        });
    }

    public Guid VirtualPathToGuid(string path)
    {
        return Guid.Empty;
    }

    public string VirtualPathFromGuid(Guid guid)
    {
        return string.Empty;
    }

    public AssetType GetAssetType(Guid guid)
    {
        return _assets.TryGetValue(guid, out var asset) ? asset.type : AssetType.Unknown;
    }

    public Guid[] GetDependencies(Guid guid)
    {
        return _assets.TryGetValue(guid, out var asset) ? asset.dependencies : Array.Empty<Guid>();
    }

    public bool HasAsset(Guid guid)
    {
        return _assets.ContainsKey(guid);
    }

    public Result<AssetReadData> OpenReadAsync(Guid guid, CancellationToken token = default)
    {
        if (_assets.TryGetValue(guid, out var asset))
        {
            if (asset.readDelayMs > 0)
            {
                // Inject our simulated I/O latency to widen race condition windows.
                // In a real multi-threaded test, this forces the executing thread to yield 
                // and lets other threads interact with the AssetManager in the meantime.
                Thread.Sleep(asset.readDelayMs);
            }

            // Return a fast, in-memory stream representing our file
            return new AssetReadData
            {
                assetId = guid,
                assetType = asset.type,
                stream = new MemoryStream(asset.data, writable: false),
            };
        }

        return Result<AssetReadData>.Failure($"Mock asset {guid} not found.");
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
