using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Core;

public struct Meshlet
{
    public SphereBounds boundingSphere;   // 16 bytes
    public AABB boundingBox;              // 24 bytes
    public uint vertexOffset;             // offset into meshlet vertex index array
    public uint triangleOffset;           // offset into packed triangle array
    public uint groupIndex;               // owning group
    public float parentError;             // geometric refinement error carried into runtime LOD tests
    public byte vertexCount;              // max 64
    public byte triangleCount;            // max 124
    public byte localMaterialIndex;       // mesh-local material slot
    public byte lodLevel;                 // this meshlet's LOD level
}

public struct MeshletGroup
{
    public SphereBounds boundingSphere;   // 16 bytes
    public AABB boundingBox;              // 24 bytes
    public float parentError;             // error of refining to the previous level
    public uint meshletStartIndex;        // contiguous meshlet range
    public uint meshletCount;             // number of meshlets in the group
    public uint lodLevel;                 // group LOD level
}

public struct MeshletHierarchyNode
{
    public SphereBounds boundingSphere;   // 16 bytes
    public AABB boundingBox;              // 24 bytes
    public float maxParentError;          // maximum error in this subtree
    public uint nodeData;                 // packed leaf/internal metadata
}

public struct MeshletMeshData : IDisposable
{
    public UnsafeList<Meshlet> meshlets;
    public UnsafeList<MeshletGroup> groups;
    public UnsafeList<MeshletHierarchyNode> hierarchyNodes;
    public UnsafeList<uint> meshletVertices;
    public UnsafeList<byte> meshletTriangles;
    public int lodLevelCount;
    public int materialSlotCount;

    public void Dispose()
    {
        meshlets.Dispose();
        groups.Dispose();
        hierarchyNodes.Dispose();
        meshletVertices.Dispose();
        meshletTriangles.Dispose();
    }
}

// TODO: Support and meshlets.
public struct Mesh : IResourceReleasable
{
    private UnsafeList<Vertex> _vertices;
    private UnsafeList<uint> _indices;
    private MeshletMeshData _meshletData;

    public MeshletMeshData MeshletData => _meshletData;

    internal bool IsMeshDataDirty
    {
        get; private set;
    }

    /// <summary>
    /// Gets or sets the collection of vertices that define the geometry.
    /// </summary>
    public UnsafeList<Vertex> Vertices
    {
        readonly get => _vertices;
        set
        {
            _vertices = value;
            VertexCount = value.Count;
            IsMeshDataDirty = true;
        }
    }

    /// <summary>
    /// Gets or sets the collection of indices that define the order of vertices.
    /// </summary>
    public UnsafeList<uint> Indices
    {
        readonly get => _indices;
        set
        {
            _indices = value;
            IndexCount = value.Count;
            IsMeshDataDirty = true;
        }
    }

    /// <summary>
    /// Get the number of vertices in the mesh.
    /// </summary>
    public int VertexCount
    {
        get; private set;
    }

    /// <summary>
    /// Get the number of indices in the mesh.
    /// </summary>
    public int IndexCount
    {
        get; private set;
    }

    /// <summary>
    /// Gets or sets the axis-aligned bounding box (AABB) of the mesh.
    /// </summary>
    public AABB BoundingBox
    {
        get; set;
    }

    /// <summary>
    /// Gets the handle to the vertex buffer on the GPU.
    /// </summary>
    public Handle<GraphicsBuffer> VertexBuffer
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the handle to the index buffer on the GPU.
    /// </summary>
    public Handle<GraphicsBuffer> IndexBuffer
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the handle to the meshlet buffer on the GPU.
    /// </summary>
    public Handle<GraphicsBuffer> MeshLetBuffer
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the handle to the meshlet vertices buffer on the GPU.
    /// </summary>
    public Handle<GraphicsBuffer> MeshletVerticesBuffer
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the handle to the meshlet triangles buffer on the GPU.
    /// </summary>
    public Handle<GraphicsBuffer> MeshletTrianglesBuffer
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the handle to the mesh data buffer on the GPU.
    /// </summary>
    public Handle<GraphicsBuffer> ObjectDataBuffer
    {
        get; internal set;
    }

    internal Mesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices, Handle<GraphicsBuffer> vertexBuffer, Handle<GraphicsBuffer> indexBuffer)
    {
        Vertices = new UnsafeList<Vertex>(vertices.Length, Allocator.Persistent);
        Indices = new UnsafeList<uint>(indices.Length, Allocator.Persistent);
        Vertices.CopyFrom(vertices);
        Indices.CopyFrom(indices);
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;

        this.ComputeBounds();
    }

    public readonly void ReleaseCpuResources()
    {
        _vertices.Dispose();
        _indices.Dispose();
        _meshletData.Dispose();
    }

    public unsafe void CookMeshlets()
    {
        // 1. Prepare Configuration
        var config = new ClodConfig
        {
            maxVertices = 64,
            minTriangles = 32,
            maxTriangles = 124,
            partitionSize = 128,
            clusterSpatial = true,
            clusterFillWeight = 1.0f,
            clusterSplitFactor = 1.0f,
            simplifyRatio = 0.5f,
            simplifyThreshold = 0.5f,
            simplifyErrorMergePrevious = 0.5f,
            simplifyErrorMergeAdditive = 0.5f,
            simplifyErrorFactorSloppy = 1.0f,
            simplifyErrorEdgeLimit = 1.0f,
            optimizeBounds = true,
            optimizeClusters = true
        };

        // 2. Map Mesh to ClodMesh
        ClodMesh clodMesh = new ClodMesh
        {
            vertexPositions = (float*)_vertices.GetUnsafePtr(),
            vertexCount = (nuint)_vertices.Count,
            vertexPositionsStride = (nuint)sizeof(Vertex),
            indices = (uint*)_indices.GetUnsafePtr(),
            indexCount = (nuint)_indices.Count,
            attributeProtectMask = 0
        };

        // 3. Build
        MeshletUtility.Build(config, clodMesh, Unsafe.AsPointer(ref this), MeshletOutputCallback);
    }

    private static unsafe int MeshletOutputCallback(void* context, ClodGroup group, ClodCluster* clusters, nuint clusterCount)
    {
        Mesh* mesh = (Mesh*)context;
        ref var data = ref mesh->_meshletData;

        // Ensure lists are initialized
        if (!data.groups.IsCreated) data.groups = new UnsafeList<MeshletGroup>(16, Allocator.Persistent);
        if (!data.meshlets.IsCreated) data.meshlets = new UnsafeList<Meshlet>(64, Allocator.Persistent);
        if (!data.meshletVertices.IsCreated) data.meshletVertices = new UnsafeList<uint>(128, Allocator.Persistent);
        if (!data.meshletTriangles.IsCreated) data.meshletTriangles = new UnsafeList<byte>(128, Allocator.Persistent);

        var meshletGroup = new MeshletGroup
        {
            meshletStartIndex = (uint)data.meshlets.Count,
            meshletCount = (uint)clusterCount,
            lodLevel = (uint)group.depth
        };
        data.groups.Add(meshletGroup);

        for (nuint i = 0; i < clusterCount; i++)
        {
            var cluster = clusters[i];
            
            var meshlet = new Meshlet
            {
                vertexCount = (byte)cluster.vertexCount,
                triangleCount = (byte)(cluster.indexCount / 3),
                vertexOffset = (uint)data.meshletVertices.Count,
                triangleOffset = (uint)data.meshletTriangles.Count,
                groupIndex = (uint)data.groups.Count - 1
            };
            data.meshlets.Add(meshlet);

            // Add indices
            for (nuint j = 0; j < cluster.indexCount; j++)
            {
                data.meshletVertices.Add(cluster.indices[j]);
            }
            // Add triangles (packed indices or byte offsets)
            // Assuming 8-bit local indices for meshlets as per standard convention
            for (nuint j = 0; j < cluster.indexCount; j++)
            {
                data.meshletTriangles.Add((byte)j);
            }
        }

        return 0;
    }

    public readonly void ReleaseResource(IResourceDatabase database)
    {
        ReleaseCpuResources();

        database.ReleaseResource(VertexBuffer.AsResource());
        database.ReleaseResource(IndexBuffer.AsResource());
        database.ReleaseResource(MeshLetBuffer.AsResource());
        database.ReleaseResource(MeshletVerticesBuffer.AsResource());
        database.ReleaseResource(MeshletTrianglesBuffer.AsResource());
        database.ReleaseResource(ObjectDataBuffer.AsResource());
    }
}

public static class MeshExtension
{
    /// <summary>
    /// Computes the bounding box of the mesh based on its vertices.
    /// </summary>
    public static void ComputeBounds(ref this Mesh mesh)
    {
        if (mesh.Vertices.Count == 0)
        {
            return;
        }

        var min = new float3(float.MaxValue);
        var max = new float3(float.MinValue);
        foreach (var vertex in mesh.Vertices)
        {
            var pos = vertex.position.xyz;
            min = math.min(min, pos);
            max = math.max(max, pos);
        }

        mesh.BoundingBox = new AABB(min, max);
    }

    /// <summary>
    /// Auto-compute smooth per-vertex normals.
    /// </summary>
    /// <remarks>
    /// Call this method before vertices and indices are valid.
    /// </remarks>
    public static void ComputeNormal(ref this Mesh mesh)
    {
        MeshBuilder.ComputeNormal(mesh.Vertices, mesh.Indices);
    }

    /// <summary>
    /// Auto-compute per-vertex tangents.
    /// </summary>
    /// <remarks>
    /// Call this method before vertices, normals, and UVs are valid.
    /// </remarks>
    public static void ComputeTangents(ref this Mesh mesh)
    {
        MeshBuilder.ComputeTangents(mesh.Vertices, mesh.Indices);
    }
}
