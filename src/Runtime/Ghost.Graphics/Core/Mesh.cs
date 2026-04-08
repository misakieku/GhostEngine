using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Core;

[StructLayout(LayoutKind.Sequential)]
public struct Meshlet
{
    public SphereBounds boundingSphere;         // 16 bytes
    public SphereBounds parentBoundingSphere;   // 16 bytes
    public AABB boundingBox;                    // 24 bytes
    public uint vertexOffset;                   // offset into meshlet vertex index array
    public uint triangleOffset;                 // offset into packed triangle array
    public uint groupIndex;                     // owning group
    public float clusterError;                  // geometric error of this meshlet/cluster
    public float parentError;                   // geometric refinement error carried into runtime LOD tests
    public byte vertexCount;                    // max 64
    public byte triangleCount;                  // max 124
    public byte localMaterialIndex;             // mesh-local material slot
    public byte lodLevel;                       // this meshlet's LOD level
}

[StructLayout(LayoutKind.Sequential)]
public struct MeshletGroup
{
    public SphereBounds boundingSphere;   // 16 bytes
    public AABB boundingBox;              // 24 bytes
    public float parentError;             // error of refining to the previous level
    public uint meshletStartIndex;        // contiguous meshlet range
    public uint meshletCount;             // number of meshlets in the group
    public uint lodLevel;                 // group LOD level
}

[StructLayout(LayoutKind.Sequential)]
public struct MeshletHierarchyNode
{
    public SphereBounds boundingSphere;   // 16 bytes
    public AABB boundingBox;              // 24 bytes
    public float maxParentError;          // maximum error in this subtree
    public uint nodeData;                 // packed leaf/internal metadata
}

[StructLayout(LayoutKind.Sequential)]
public struct MeshletMeshData : IDisposable
{
    public UnsafeList<Meshlet> meshlets;
    public UnsafeList<MeshletGroup> groups;
    public UnsafeList<MeshletHierarchyNode> hierarchyNodes;
    public UnsafeList<uint> meshletVertices;
    public UnsafeList<uint> meshletTriangles;
    public int meshletCount;
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

public struct Mesh : IResourceReleasable
{
    private UnsafeList<Vertex> _vertices;
    private UnsafeList<uint> _indices;
    private MeshletMeshData _meshletData;

    [UnscopedRef]
    public readonly ref readonly MeshletMeshData MeshletData => ref _meshletData;

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
    public Handle<GPUBuffer> VertexBuffer
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the handle to the index buffer on the GPU.
    /// </summary>
    public Handle<GPUBuffer> IndexBuffer
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the handle to the meshlet buffer on the GPU.
    /// </summary>
    public Handle<GPUBuffer> MeshLetBuffer
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the handle to the meshlet vertices buffer on the GPU.
    /// </summary>
    public Handle<GPUBuffer> MeshletVerticesBuffer
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the handle to the meshlet triangles buffer on the GPU.
    /// </summary>
    public Handle<GPUBuffer> MeshletTrianglesBuffer
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the handle to the mesh data buffer on the GPU.
    /// </summary>
    public Handle<GPUBuffer> ObjectDataBuffer
    {
        get; internal set;
    }

    internal Mesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices, Handle<GPUBuffer> vertexBuffer, Handle<GPUBuffer> indexBuffer)
    {
        Vertices = new UnsafeList<Vertex>(vertices.Length, Allocator.Persistent);
        Indices = new UnsafeList<uint>(indices.Length, Allocator.Persistent);
        Vertices.CopyFrom(vertices);
        Indices.CopyFrom(indices);
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;

        this.ComputeBounds();
    }

    public void ReleaseCpuResources()
    {
        _vertices.Dispose();
        _indices.Dispose();
        _meshletData.Dispose();
    }

    public unsafe void CookMeshlets()
    {
        if (_meshletData.meshlets.IsCreated)
        {
            _meshletData.meshlets.Dispose();
        }

        if (_meshletData.groups.IsCreated)
        {
            _meshletData.groups.Dispose();
        }

        if (_meshletData.hierarchyNodes.IsCreated)
        {
            _meshletData.hierarchyNodes.Dispose();
        }

        if (_meshletData.meshletVertices.IsCreated)
        {
            _meshletData.meshletVertices.Dispose();
        }

        if (_meshletData.meshletTriangles.IsCreated)
        {
            _meshletData.meshletTriangles.Dispose();
        }

        _meshletData.meshletCount = 0;
        _meshletData.lodLevelCount = 0;
        _meshletData.materialSlotCount = 0;

        // 1. Prepare Configuration
        var config = new ClodConfig
        {
            maxVertices = 64,
            minTriangles = 32,
            maxTriangles = 124,

            partitionSpatial = true,
            partitionSize = 16,

            clusterSpatial = false,
            clusterSplitFactor = 2.0f,

            optimizeClusters = true,
            optimizeClustersLevel = 1,

            simplifyRatio = 0.5f,
            simplifyThreshold = 0.85f,
            simplifyErrorMergePrevious = 1.0f,
            simplifyErrorFactorSloppy = 2.0f,
            simplifyPermissive = true,
            simplifyFallbackPermissive = false,
            simplifyFallbackSloppy = true,
        };

        // 2. Map Mesh to ClodMesh
        var clodMesh = new ClodMesh
        {
            vertexPositions = (float*)Unsafe.AsPointer(ref _vertices[0].position),
            vertexCount = (nuint)_vertices.Count,
            vertexPositionsStride = (nuint)sizeof(Vertex),
            vertexAttributes = (float*)Unsafe.AsPointer(ref _vertices[0].normal),
            vertexAttributesStride = (nuint)sizeof(Vertex),
            indices = (uint*)_indices.GetUnsafePtr(),
            indexCount = (nuint)_indices.Count,
            attributeProtectMask = 0,
        };

        // 3. Build
        MeshletUtility.Build(in config, in clodMesh, Unsafe.AsPointer(ref this), MeshletOutputCallback);

        _meshletData.meshletCount = _meshletData.meshlets.IsCreated ? _meshletData.meshlets.Count : 0;

        if (_meshletData.groups.IsCreated && _meshletData.groups.Count > 0)
        {
            var maxLodLevel = 0u;
            for (var i = 0; i < _meshletData.groups.Count; i++)
            {
                maxLodLevel = Math.Max(maxLodLevel, _meshletData.groups[i].lodLevel);
            }

            _meshletData.lodLevelCount = (int)maxLodLevel + 1;
        }

        _meshletData.materialSlotCount = 1;
    }

    private static unsafe int MeshletOutputCallback(void* context, ClodGroup group, ReadOnlyUnsafeCollection<ClodCluster> clusters)
    {
        var mesh = (Mesh*)context;

        ref var data = ref mesh->_meshletData;

        // Ensure lists are initialized
        if (!data.groups.IsCreated) data.groups = new UnsafeList<MeshletGroup>(16, Allocator.Persistent);
        if (!data.meshlets.IsCreated) data.meshlets = new UnsafeList<Meshlet>(64, Allocator.Persistent);
        if (!data.meshletVertices.IsCreated) data.meshletVertices = new UnsafeList<uint>(128, Allocator.Persistent);
        if (!data.meshletTriangles.IsCreated) data.meshletTriangles = new UnsafeList<uint>(128, Allocator.Persistent);

        var meshletGroup = new MeshletGroup
        {
            boundingSphere = new SphereBounds(group.simplified.center, group.simplified.radius),
            boundingBox = new AABB(group.simplified.center - group.simplified.radius, group.simplified.center + group.simplified.radius),
            parentError = group.simplified.error,
            meshletStartIndex = (uint)data.meshlets.Count,
            meshletCount = (uint)clusters.Count,
            lodLevel = (uint)group.depth
        };
        data.groups.Add(meshletGroup);

        for (var i = 0; i < clusters.Count; i++)
        {
            var cluster = clusters[i];

            var meshlet = new Meshlet
            {
                boundingSphere = new SphereBounds(cluster.bounds.center, cluster.bounds.radius),
                parentBoundingSphere = new SphereBounds(group.simplified.center, group.simplified.radius),
                boundingBox = new AABB(cluster.bounds.center - cluster.bounds.radius, cluster.bounds.center + cluster.bounds.radius),
                vertexCount = (byte)cluster.vertexCount,
                triangleCount = (byte)(cluster.localIndexCount / 3),
                vertexOffset = (uint)data.meshletVertices.Count,
                triangleOffset = (uint)data.meshletTriangles.Count,
                groupIndex = (uint)data.groups.Count - 1,
                clusterError = cluster.bounds.error,
                parentError = group.simplified.error,
                localMaterialIndex = 0, // TODO: support multiple materials
                lodLevel = (byte)group.depth,
            };
            data.meshlets.Add(meshlet);

            // Add unique vertices
            for (nuint j = 0; j < cluster.vertexCount; j++)
            {
                data.meshletVertices.Add(cluster.uniqueVertices[j]);
            }
            // Add local triangles (packed into uints)
            var triangleCount = cluster.localIndexCount / 3;
            for (nuint j = 0; j < triangleCount; j++)
            {
                uint i0 = cluster.localIndices[j * 3 + 0];
                uint i1 = cluster.localIndices[j * 3 + 1];
                uint i2 = cluster.localIndices[j * 3 + 2];
                var packedTriangle = i0 | (i1 << 8) | (i2 << 16);
                data.meshletTriangles.Add(packedTriangle);
            }
        }

        return 0;
    }

    public void ReleaseResource(IResourceDatabase database)
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
