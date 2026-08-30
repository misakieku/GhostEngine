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
    public Handle<GPUBuffer> MeshletBuffer
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

    public readonly Mesh Clone()
    {
        var newData = this;

        newData._vertices = _vertices.Clone(AllocationHandle.Persistent);
        newData._indices = _indices.Clone(AllocationHandle.Persistent);
        newData._meshletData = _meshletData.Clone();

        return newData;
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
        database.ReleaseResource(MeshletBuffer.AsResource());
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
