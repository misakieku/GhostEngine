using Ghost.Graphics.Contracts;
using Misaki.HighPerformance.Unsafe.Collections;
using Misaki.HighPerformance.Unsafe.Helpers;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Ghost.Graphics.Data;

public sealed class Mesh(int initialVertexCapacity = 256, int initialIndexCapacity = 512) : IDisposable
{
    private UnsafeList<Vertex> _vertices = new(initialVertexCapacity, Allocator.Persistent);
    private UnsafeList<int> _indices = new(initialIndexCapacity, Allocator.Persistent);

    private BoundingBox _bounds;

    private IResource? _vertexBuffer;
    private IResource? _indexBuffer;
    private VertexBufferView _vertexBufferView;
    private IndexBufferView _indexBufferView;

    public Span<Vertex> Vertices => _vertices.AsSpan();
    public Span<int> Indices => _indices.AsSpan();
    public BoundingBox Bounds => _bounds;
    public int VertexCount => _vertices.Count;
    public int IndexCount => _indices.Count;

    ~Mesh()
    {
        Dispose();
    }

    /// <summary>
    /// Adds a vertex to the mesh with the specified attributes.
    /// </summary>
    /// <param name="vertex">The vertex data to add</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddVertex(Vertex vertex)
    {
        _vertices.Add(vertex);
    }

    /// <summary>
    /// Adds a triangle to the mesh by specifying the indices of its three vertices.
    /// </summary>
    /// <param name="index0">The index of the first vertex in the triangle. Must be within the range of the current vertex count.</param>
    /// <param name="index1">The index of the second vertex in the triangle. Must be within the range of the current vertex count.</param>
    /// <param name="index2">The index of the third vertex in the triangle. Must be within the range of the current vertex count.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any of the specified indices are out of range for the current vertex count.</exception>
    public void AddTriangle(int index0, int index1, int index2)
    {
        if (index0 >= _vertices.Count || index1 >= _vertices.Count || index2 >= _vertices.Count)
        {
            throw new ArgumentOutOfRangeException("Index out of range for the current vertex count.");
        }

        _indices.Add(index0);
        _indices.Add(index1);
        _indices.Add(index2);
    }

    /// <summary>
    /// Reduces the memory usage of the internal collections by resizing them to match their current element count.
    /// </summary>
    public void TrimExcess()
    {
        _vertices.Resize(_vertices.Count);
        _indices.Resize(_indices.Count);
    }

    /// <summary>
    /// Auto-compute smooth per-vertex normals.
    /// </summary>
    /// <remarks>
    /// Call this method before vertices and indices are valid.
    /// </remarks>
    public void ComputeNormal()
    {
        if (!_vertices.IsCreated || _vertices.Count < 3
            || !_indices.IsCreated || _indices.Count < 3)
        {
            return;
        }

        for (var i = 0; i < _indices.Count; i += 3)
        {
            var i0 = _indices[i];
            var i1 = _indices[i + 1];
            var i2 = _indices[i + 2];

            var v0 = _vertices[i0];
            var v1 = _vertices[i1];
            var v2 = _vertices[i2];

            var edge1 = v1.Position - v0.Position;
            var edge2 = v2.Position - v0.Position;
            var faceNormal = Vector3.Cross(edge1.AsVector3(), edge2.AsVector3());

            _vertices[i0].Normal += faceNormal.AsVector4();
            _vertices[i1].Normal += faceNormal.AsVector4();
            _vertices[i2].Normal += faceNormal.AsVector4();
        }

        for (var i = 0; i < _vertices.Count; i++)
        {
            _vertices[i].Normal = Vector4.Normalize(_vertices[i].Normal);
        }
    }

    /// <summary>
    /// Auto-compute per-vertex tangents.
    /// </summary>
    /// <remarks>
    /// Call this method before vertices, normals, and UVs are valid.
    /// </remarks>
    public void ComputeTangents()
    {
        var bitangents = new Vector4[_vertices.Count];

        for (var i = 0; i < _indices.Count; i += 3)
        {
            var i0 = _indices[i];
            var i1 = _indices[i + 1];
            var i2 = _indices[i + 2];

            var v0 = _vertices[i0];
            var v1 = _vertices[i1];
            var v2 = _vertices[i2];

            var uv0 = _vertices[i0].UV;
            var uv1 = _vertices[i1].UV;
            var uv2 = _vertices[i2].UV;

            var deltaPos1 = v1.Position - v0.Position;
            var deltaPos2 = v2.Position - v0.Position;
            var deltaUV1 = uv1 - uv0;
            var deltaUV2 = uv2 - uv0;

            var r = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV1.Y * deltaUV2.X);
            var tangent = (deltaPos1 * deltaUV2.Y - deltaPos2 * deltaUV1.Y) * r;
            var bitangent = (deltaPos2 * deltaUV1.X - deltaPos1 * deltaUV2.X) * r;

            for (var j = 0; j < 3; j++)
            {
                var idx = _indices[i + j];
                var t = _vertices[idx].Tangent;
                _vertices[idx].Tangent = new Vector4(
                    t.X + tangent.X,
                    t.Y + tangent.Y,
                    t.Z + tangent.Z,
                    0.0f  // we’ll fill w later
                );

                bitangents[idx] += bitangent;
            }
        }

        for (var i = 0; i < _vertices.Count; i++)
        {
            var n = _vertices[i].Normal;
            var t = _vertices[i].Tangent;
            var n3 = n.AsVector3();
            var t3 = t.AsVector3();

            var proj = n3 * Vector3.Dot(n3, t3);
            t3 = Vector3.Normalize(t3 - proj);

            var b = bitangents[i];
            var w = Vector3.Dot(Vector3.Cross(n3, t3), b.AsVector3()) < 0.0f ? -1.0f : 1.0f;

            _vertices[i].Tangent = new Vector4(t3.X, t3.Y, t3.Z, w);
        }
    }

    /// <summary>
    /// Computes the bounding box of the mesh based on its vertices.
    /// </summary>
    public void ComputeBounds()
    {
        if (_vertices.Count == 0)
        {
            _bounds = BoundingBox.Zero;
            return;
        }

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var vertex in _vertices)
        {
            var pos = vertex.Position.AsVector3();
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }

        _bounds = new BoundingBox(min, max);
    }

    /// <summary>
    /// Uploads the mesh data to GPU resources.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="commandList">The Direct3D 12 command list to record the upload commands.</param>
    public unsafe void UploadMeshData(ICommandBuffer cmb)
    {
        if (VertexCount == 0 || IndexCount == 0)
        {
            return;
        }

        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();

        var vertexBufferSize = (uint)(VertexCount * sizeof(Vertex));
        var indexBufferSize = (uint)(IndexCount * sizeof(int));
        _vertexBuffer = GraphicsPipeline.ResourceAllocator.CreateCopyDestinationBuffer(vertexBufferSize);
        _indexBuffer = GraphicsPipeline.ResourceAllocator.CreateCopyDestinationBuffer(indexBufferSize);

        using var vertexUploadBuffer = GraphicsPipeline.ResourceAllocator.CreateUploadBuffer(vertexBufferSize);
        using var indexUploadBuffer = GraphicsPipeline.ResourceAllocator.CreateUploadBuffer(indexBufferSize);

        vertexUploadBuffer.SetData(_vertices.AsSpan());
        indexUploadBuffer.SetData(_indices.AsSpan());

        cmb.CopyResource(_vertexBuffer, 0, vertexUploadBuffer, 0, vertexBufferSize);
        cmb.CopyResource(_indexBuffer, 0, indexUploadBuffer, 0, indexBufferSize);

        _vertexBufferView = new VertexBufferView
        {
            BufferLocation = _vertexBuffer.GPUAddress,
            SizeInBytes = vertexBufferSize,
            StrideInBytes = (uint)sizeof(Vertex)
        };

        _indexBufferView = new IndexBufferView
        {
            BufferLocation = _indexBuffer.GPUAddress,
            SizeInBytes = indexBufferSize,
            Format = Format.R32_SInt
        };
    }

    /// <summary>
    /// Clears all vertex and index data and releases associated GPU resources.
    /// </summar>
    public void Clear()
    {
        _vertices.Clear();
        _indices.Clear();
        DisposeGpuResources();
    }

    private void DisposeGpuResources()
    {
        _vertexBuffer?.Dispose();
        _vertexBuffer = null;

        _indexBuffer?.Dispose();
        _indexBuffer = null;
    }

    public void Dispose()
    {
        _vertices.Dispose();
        _indices.Dispose();
        DisposeGpuResources();

        GC.SuppressFinalize(this);
    }
}