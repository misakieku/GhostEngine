using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Diagnostics.CodeAnalysis;

namespace Ghost.Graphics.Core;


public struct Mesh : IResourceReleasable
{
    private UnsafeList<Vertex> _vertices;
    private UnsafeList<uint> _indices;
    private MeshletMeshData _meshletData;

    [UnscopedRef]
    public ref MeshletMeshData MeshletData => ref _meshletData;

    internal bool IsMeshDataDirty
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the collection of vertices that define the geometry.
    /// </summary>
    public UnsafeList<Vertex> Vertices
    {
        readonly get => _vertices;
        set
        {
            _vertices.Dispose();
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
            _indices.Dispose();
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
        get; internal set;
    }

    /// <summary>
    /// Get the number of indices in the mesh.
    /// </summary>
    public int IndexCount
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the number of meshlets in the mesh.
    /// </summary>
    public readonly int MeshletCount => _meshletData.meshletCount;

    /// <summary>
    /// Gets or sets the axis-aligned bounding box (AABB) of the mesh.
    /// </summary>
    public AABB BoundingBox
    {
        get; set;
    }

    /// <summary>
    /// Gets the handle to the mesh buffer on the GPU, which contains all mesh-related data (vertices, indices, meshlets, etc.).
    /// </summary>
    public Handle<GPUBuffer> MeshBuffer
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the offset of the vertex buffer in the GPU buffer.
    /// </summary>
    public ulong VertexBufferOffset
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the offset of the index buffer in the GPU buffer.
    /// </summary>
    public ulong IndexBufferOffset
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the offset of the meshlet buffer in the GPU buffer.
    /// </summary>
    public ulong MeshletBufferOffset
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the offset of the meshlet vertices buffer in the GPU buffer.
    /// </summary>
    public ulong MeshletVerticesBufferOffset
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the offset of the meshlet triangles buffer in the GPU buffer.
    /// </summary>
    public ulong MeshletTrianglesBufferOffset
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the offset of the meshlet group buffer in the GPU buffer.
    /// </summary>
    public ulong MeshletGroupBufferOffset
    {
        get; internal set;
    }

    /// <summary>
    /// Gets the offset of the meshlet hierarchy buffer in the GPU buffer.
    /// </summary>
    public ulong MeshletHierarchyBufferOffset
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

    /// <summary>
    /// Creates a deep copy of the current mesh, including its vertices, indices, and meshlet data. The cloned mesh will have its own separate memory allocation for these resources.
    /// </summary>
    /// <remarks>
    /// This does not clone the GPU resources (MeshBuffer and MeshDataBuffer). The cloned mesh will need to be uploaded to the GPU separately if required.
    /// </remarks>
    /// <returns>The cloned mesh.</returns>
    public readonly Mesh Clone()
    {
        var newData = this;

        newData._vertices = _vertices.Clone(AllocationHandle.Persistent);
        newData._indices = _indices.Clone(AllocationHandle.Persistent);
        newData._meshletData = _meshletData.Clone();

        return newData;
    }

    /// <summary>
    /// Releases the CPU-side resources (vertices, indices, and meshlet data) associated with the mesh.
    /// </summary>
    public void ReleaseCpuResources()
    {
        _vertices.Dispose();
        _indices.Dispose();
        _meshletData.Dispose();
    }

    public void ReleaseResource(IResourceDatabase database)
    {
        ReleaseCpuResources();

        database.ReleaseResource(MeshBuffer.AsResource());
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
