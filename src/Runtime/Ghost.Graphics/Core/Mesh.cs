using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Diagnostics.CodeAnalysis;
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
    public float4 minX;
    public float4 minY;
    public float4 minZ;
    public float4 maxX;
    public float4 maxY;
    public float4 maxZ;
    public float4 maxParentError;
    
    // x,y,z,w correspond to children 0,1,2,3.
    // MSB (1 << 31) indicates it's an Internal Node.
    // If MSB is 0, the remaining 31 bits are the MeshletIndex.
    // If MSB is 1, the remaining 31 bits are the child MeshletHierarchyNode index.
    // 0xFFFFFFFF means invalid/empty slot.
    public uint4 nodeData;
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
    /// Gets the handle to the meshlet group buffer on the GPU.
    /// </summary>
    public Handle<GPUBuffer> MeshletGroupBuffer
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the handle to the meshlet hierarchy buffer on the GPU.
    /// </summary>
    public Handle<GPUBuffer> MeshletHierarchyBuffer
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the handle to the mesh data buffer on the GPU.
    /// </summary>
    public Handle<GPUBuffer> MeshDataBuffer
    {
        get; internal set;
    }

    internal void SetMeshletSummary(int meshletCount, int lodLevelCount, int materialSlotCount)
    {
        _meshletData.meshletCount = meshletCount;
        _meshletData.lodLevelCount = lodLevelCount;
        _meshletData.materialSlotCount = materialSlotCount;
    }

    internal void SetCounts(int vertexCount, int indexCount)
    {
        VertexCount = vertexCount;
        IndexCount = indexCount;
    }

    public void ReleaseCpuResources()
    {
        _vertices.Dispose();
        _indices.Dispose();
        _meshletData.Dispose();
    }

    public void ReleaseResource(IResourceDatabase database)
    {
        ReleaseCpuResources();

        database.ReleaseResource(VertexBuffer.AsResource());
        database.ReleaseResource(IndexBuffer.AsResource());
        database.ReleaseResource(MeshLetBuffer.AsResource());
        database.ReleaseResource(MeshletVerticesBuffer.AsResource());
        database.ReleaseResource(MeshletTrianglesBuffer.AsResource());
        database.ReleaseResource(MeshletGroupBuffer.AsResource());
        database.ReleaseResource(MeshletHierarchyBuffer.AsResource());
        database.ReleaseResource(MeshDataBuffer.AsResource());
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
