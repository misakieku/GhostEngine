using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Engine.Streaming;
using Misaki.HighPerformance.LowLevel.Buffer;
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
    private readonly ConcurrentDictionary<string, Guid> _pathToGuid = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, string> _guidToPath = new();

    public IReadOnlyList<ShaderCatalogEntry> ShaderCatalog { get; init; } = Array.Empty<ShaderCatalogEntry>();

    public void RegisterPath(string path, Guid guid)
    {
        _pathToGuid[path] = guid;
        _guidToPath[guid] = path;
    }

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
        var boundsMin = new float3(0, 0, 0);
        var boundsMax = new float3(1, 1, 0);
        var quantStep = PackedVertex.ComputePositionQuantStep(boundsMin, boundsMax);
        var maxGrid = (1u << PackedVertex.MAX_POSITION_QUANT_BITS) - 1u;

        var meshletVertices = new PackedVertex[]
        {
            PackedVertex.Pack(in vertices[0]),
            PackedVertex.Pack(in vertices[1]),
            PackedVertex.Pack(in vertices[2])
        };

        // Quantize positions and pack as bitstream (bitsZ == 0 since z == 0)
        PackedVertex.QuantizePosition(meshletVertices[0].position, boundsMin, quantStep, maxGrid, out var qx0, out var qy0, out var qz0);
        PackedVertex.QuantizePosition(meshletVertices[1].position, boundsMin, quantStep, maxGrid, out var qx1, out var qy1, out var qz1);
        PackedVertex.QuantizePosition(meshletVertices[2].position, boundsMin, quantStep, maxGrid, out var qx2, out var qy2, out var qz2);
        var minX = Math.Min(qx0, Math.Min(qx1, qx2));
        var minY = Math.Min(qy0, Math.Min(qy1, qy2));
        var minZ = Math.Min(qz0, Math.Min(qz1, qz2));
        var maxX = Math.Max(qx0, Math.Max(qx1, qx2));
        var maxY = Math.Max(qy0, Math.Max(qy1, qy2));
        var maxZ = Math.Max(qz0, Math.Max(qz1, qz2));
        var bitsX = PackedVertex.BitWidth(maxX - minX);
        var bitsY = PackedVertex.BitWidth(maxY - minY);
        var bitsZ = PackedVertex.BitWidth(maxZ - minZ);
        var totalPosBits = bitsX + bitsY + bitsZ;
        var posWordCount = totalPosBits > 0 ? ((3 * totalPosBits + 31) >> 5) : 0;
        var posWords = new uint[posWordCount];
        static void WriteTestBits(uint[] dst, int bitOffset, uint value, int bitCount)
        {
            if (bitCount <= 0) return;
            var wordIdx = bitOffset >> 5;
            var bitInWord = bitOffset & 31;
            var masked = value & ((1u << bitCount) - 1u);
            dst[wordIdx] |= masked << bitInWord;
            if (bitInWord + bitCount > 32) dst[wordIdx + 1] |= masked >> (32 - bitInWord);
        }
        if (totalPosBits > 0)
        {
            WriteTestBits(posWords, 0 * totalPosBits, qx0 - minX, bitsX);
            WriteTestBits(posWords, 0 * totalPosBits + bitsX, qy0 - minY, bitsY);
            WriteTestBits(posWords, 0 * totalPosBits + bitsX + bitsY, qz0 - minZ, bitsZ);
            WriteTestBits(posWords, 1 * totalPosBits, qx1 - minX, bitsX);
            WriteTestBits(posWords, 1 * totalPosBits + bitsX, qy1 - minY, bitsY);
            WriteTestBits(posWords, 1 * totalPosBits + bitsX + bitsY, qz1 - minZ, bitsZ);
            WriteTestBits(posWords, 2 * totalPosBits, qx2 - minX, bitsX);
            WriteTestBits(posWords, 2 * totalPosBits + bitsX, qy2 - minY, bitsY);
            WriteTestBits(posWords, 2 * totalPosBits + bitsX + bitsY, qz2 - minZ, bitsZ);
        }

        var pageAttributes = new PackedAttribute[]
        {
            new() { packedNormal = meshletVertices[0].packedNormal, uv = meshletVertices[0].uv },
            new() { packedNormal = meshletVertices[1].packedNormal, uv = meshletVertices[1].uv },
            new() { packedNormal = meshletVertices[2].packedNormal, uv = meshletVertices[2].uv },
        };
        var posByteOffset = (uint)sizeof(PageHeader);
        var attrByteOffset = posByteOffset + (uint)(posWords.Length * sizeof(uint));
        var triByteOffset = attrByteOffset + (uint)(pageAttributes.Length * sizeof(PackedAttribute));
        var meshletTriangles = new uint[] { 0u | (1u << 6) | (2u << 12) };

        PackedVertex.PackQuantOrigin(minX, minY, minZ, out var origin0, out var origin1);
        var meshlets = new[]
        {
            new Meshlet
            {
                boundingSphere = new SphereBounds(new float3(0.5f, 0.5f, 0), 1.0f),
                parentBoundingSphere = new SphereBounds(new float3(0.5f, 0.5f, 0), 1.0f),
                boundingBox = new AABB(boundsMin, boundsMax),
                vertexOffset = posByteOffset,
                triangleOffset = triByteOffset,
                pageIndex = 0,
                clusterError = 0,
                parentError = 0,
                vertexCount = 3,
                triangleCount = 1,
                localMaterialIndex = 0,
                lodLevel = 0,
                packedQuantOrigin0 = origin0,
                packedQuantOrigin1 = origin1,
                packedFormat = PackedVertex.PackMeshletFormat(bitsX, bitsY, bitsZ, false, attrByteOffset),
            }
        };
        var groups = new[]
        {
            new MeshletGroup
            {
                boundingSphere = new SphereBounds(new float3(0.5f, 0.5f, 0), 1.0f),
                boundingBox = new AABB(boundsMin, boundsMax),
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
            boundsMin = boundsMin,
            boundsMax = boundsMax,
            boundingSphereCenter = new float3(0.5f, 0.5f, 0),
            boundingSphereRadius = 1.0f,
            positionQuantStep = quantStep,
        };

        var pageHeader = new PageHeader
        {
            magic = PageHeader.MAGIC,
            meshletCount = 1,
            vertexCount = (ushort)meshletVertices.Length,
            triangleCount = 1,
            meshletOffset = 0,
            vertexOffset = posByteOffset,
            triangleOffset = triByteOffset,
            pageIndex = 0,
            reserved = attrByteOffset,
        };
        var pagePayloadSize = triByteOffset + (uint)(meshletTriangles.Length * sizeof(uint));
        var pageDirectory = new[]
        {
            new PageDirectoryEntry
            {
                fileOffset = 0,
                compressedSize = pagePayloadSize,
                uncompressedSize = pagePayloadSize,
                meshletStartIndex = 0,
                meshletCount = 1,
                lodLevel = 0,
                isRootPage = 1,
                compressionMethod = PageCompressionMethod.None,
            }
        };

        stream.Write(header);
        header.hierarchyOffset = stream.Position; stream.Write(hierarchy); header.hierarchySize = sizeof(MeshletHierarchyNode); header.hierarchyUncompressedSize = header.hierarchySize;
        header.groupOffset = stream.Position; stream.Write(groups); header.groupSize = sizeof(MeshletGroup); header.groupUncompressedSize = header.groupSize;
        header.meshletOffset = stream.Position; stream.Write(meshlets); header.meshletSize = sizeof(Meshlet); header.meshletUncompressedSize = header.meshletSize;
        var pageDirPos = stream.Position;
        header.pageDirectoryOffset = stream.Position; stream.Write(pageDirectory); header.pageDirectorySize = sizeof(PageDirectoryEntry);
        header.rootPageOffset = stream.Position;
        stream.Write(pageHeader);
        if (posWords.Length > 0) stream.Write(posWords);
        stream.Write(pageAttributes);
        stream.Write(meshletTriangles);
        header.rootPageSize = stream.Position - header.rootPageOffset;
        header.materialPartOffset = stream.Position; stream.Write(materialParts); header.materialPartSize = sizeof(MeshContentMaterialPart);

        var proxyPositions = new float3[] { new float3(0, 0, 0), new float3(1, 0, 0), new float3(0, 1, 0) };
        var proxyIndices = new uint[] { 0, 1, 2 };
        header.rayTracingProxyOffset = stream.Position;
        header.rayTracingProxyVertexCount = proxyPositions.Length;
        header.rayTracingProxyIndexCount = proxyIndices.Length;
        stream.Write(proxyPositions);
        stream.Write(proxyIndices);
        header.rayTracingProxySize = stream.Position - header.rayTracingProxyOffset;
        header.rayTracingProxyUncompressedSize = header.rayTracingProxySize;

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
        return _pathToGuid.TryGetValue(path, out var guid) ? guid : Guid.Empty;
    }

    public string VirtualPathFromGuid(Guid guid)
    {
        return _guidToPath.TryGetValue(guid, out var path) ? path : string.Empty;
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

    public unsafe Result<AssetPayload> OpenReadAsync(Guid guid, CancellationToken token = default)
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

            var alignedSize = Misaki.HighPerformance.LowLevel.Utilities.MemoryUtility.AlignUp((nuint)asset.data.Length, 16);
            var block = new Misaki.HighPerformance.LowLevel.Buffer.MemoryBlock(alignedSize, 16, AllocationHandle.Persistent);
            fixed (byte* pSrc = asset.data)
            {
                Misaki.HighPerformance.LowLevel.Utilities.MemoryUtility.MemCpy(block.GetUnsafePtr(), pSrc, (nuint)asset.data.Length);
            }

            return AssetPayload.FromAllocatedMemory(block, asset.data.Length);
        }

        return Result.Failure($"Mock asset {guid} not found.");
    }

    public void Dispose()
    {
    }
}
