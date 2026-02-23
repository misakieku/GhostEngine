using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;

namespace Ghost.Graphics.Core;

public struct Mesh : IResourceReleasable
{
    private UnsafeList<Vertex> _vertices;
    private UnsafeList<uint> _indices;

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
    }

    readonly void IResourceReleasable.ReleaseResource(IResourceDatabase database)
    {
        ReleaseCpuResources();

        database.ScheduleReleaseResource(VertexBuffer.AsResource());
        database.ScheduleReleaseResource(IndexBuffer.AsResource());
        database.ScheduleReleaseResource(ObjectDataBuffer.AsResource());
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
