using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Engine;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

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
            width = width,
            height = height,
            bpc = 8,
            mipLevels = 1,
            dimension = 2, // Texture2D
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

    public void AddMockMesh(Guid guid, int readDelayMs = 0)
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
                vertexOffset = 0,
                triangleOffset = 0,
                groupIndex = 0,
                clusterError = 0,
                parentError = 0,
                vertexCount = 3,
                triangleCount = 1,
                localMaterialIndex = 0,
                lodLevel = 0,
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
            }
        };
        var hierarchy = new[]
        {
            new MeshletHierarchyNode
            {
                minX = new float4(0, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                minY = new float4(0, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                minZ = new float4(0, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                maxX = new float4(1, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity),
                maxY = new float4(1, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity),
                maxZ = new float4(0, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity),
                maxParentError = new float4(0),
                nodeData = new uint4(0, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF),
            }
        };
        var meshletVertices = new uint[] { 0, 1, 2 };
        var meshletTriangles = new uint[] { 0 | (1u << 8) | (2u << 16) };

        using var stream = new MemoryStream();
        var header = new MeshContentHeader
        {
            magic = MeshContentHeader.MAGIC,
            version = MeshContentHeader.VERSION,
            vertexCount = (uint)vertices.Length,
            indexCount = (uint)indices.Length,
            materialPartCount = (uint)materialParts.Length,
            meshletCount = (uint)meshlets.Length,
            meshletGroupCount = (uint)groups.Length,
            meshletHierarchyNodeCount = (uint)hierarchy.Length,
            meshletVertexCount = (uint)meshletVertices.Length,
            meshletTriangleCount = (uint)meshletTriangles.Length,
            materialSlotCount = 1,
            lodLevelCount = 1,
            boundsMin = new float3(0, 0, 0),
            boundsMax = new float3(1, 1, 0),
        };

        stream.Write(header);
        header.vertexOffset = (ulong)stream.Position; stream.Write(vertices);
        header.indexOffset = (ulong)stream.Position; stream.Write(indices);
        header.materialPartOffset = (ulong)stream.Position; stream.Write(materialParts);
        header.meshletOffset = (ulong)stream.Position; stream.Write(meshlets);
        header.meshletGroupOffset = (ulong)stream.Position; stream.Write(groups);
        header.meshletHierarchyNodeOffset = (ulong)stream.Position; stream.Write(hierarchy);
        header.meshletVertexOffset = (ulong)stream.Position; stream.Write(meshletVertices);
        header.meshletTriangleOffset = (ulong)stream.Position; stream.Write(meshletTriangles);

        stream.Position = 0;
        stream.Write(header);

        AddMockAsset(guid, new MockAssetData
        {
            type = AssetType.Mesh,
            data = stream.ToArray(),
            readDelayMs = readDelayMs
        });
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

    public Result<Stream> OpenRead(Guid guid, CancellationToken token = default)
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
            return Result<Stream>.Success(new MemoryStream(asset.data, writable: false));
        }

        return Result<Stream>.Failure($"Mock asset {guid} not found.");
    }
}
