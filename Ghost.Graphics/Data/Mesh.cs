using Misaki.HighPerformance.Unsafe.Collections;
using Misaki.HighPerformance.Unsafe.Helpers;
using System.Numerics;

namespace Ghost.Graphics.Data;

public sealed class Mesh(int initialVertexCapacity = 256, int initialIndexCapacity = 512) : IDisposable
{
    private UnsafeList<Vector3> _vertices = new(initialVertexCapacity, Allocator.Persistent);
    private UnsafeList<Vector3> _normals = new(initialVertexCapacity, Allocator.Persistent);
    private UnsafeList<Vector4> _tangents = new(initialVertexCapacity, Allocator.Persistent);
    private UnsafeList<Color32> _colors = new(initialVertexCapacity, Allocator.Persistent);
    private UnsafeList<Vector2> _uvs = new(initialVertexCapacity, Allocator.Persistent);
    private UnsafeList<int> _indices = new(initialIndexCapacity, Allocator.Persistent);

    public Span<Vector3> Vertices => _vertices.AsSpan();
    public Span<Vector3> Normals => _normals.AsSpan();
    public Span<Vector4> Tangents => _tangents.AsSpan();
    public Span<Color32> Colors => _colors.AsSpan();
    public Span<Vector2> UVs => _uvs.AsSpan();
    public Span<int> Indices => _indices.AsSpan();

    public int VertexCount => _vertices.Count;

    ~Mesh()
    {
        Dispose();
    }

    /// <summary>
    /// Adds a vertex to the mesh with the specified attributes.
    /// </summary>
    /// <remarks>This method adds the vertex attributes to their respective collections, allowing the mesh    
    /// to be constructed with detailed vertex data. Ensure that all parameters are provided with valid values to
    /// avoid incomplete or incorrect mesh data.</remarks>
    /// <param name="position">The position of the vertex in 3D space.</param>
    /// <param name="normal">The normal vector at the vertex.</param>
    /// <param name="tangent">The tangent vector at the vertex.</param>
    /// <param name="color">The color of the vertex.</param>
    /// <param name="uv">The UV coordinates of the vertex.</param>
    public void AddVertex(Vector3 position, Vector3 normal, Vector4 tangent, Color32 color, Vector2 uv)
    {
        _vertices.Add(position);
        _normals.Add(normal);
        _tangents.Add(tangent);
        _colors.Add(color);
        _uvs.Add(uv);
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

    public void TrimExcess()
    {
        _vertices.Resize(_vertices.Count);
        _normals.Resize(_normals.Count);
        _tangents.Resize(_tangents.Count);
        _colors.Resize(_colors.Count);
        _uvs.Resize(_uvs.Count);
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

        if (!_normals.IsCreated)
        {
            _normals = new(_vertices.Count, Allocator.Persistent);
        }
        else
        {
            _normals.Clear();
            _normals.Resize(_vertices.Count);
        }

        for (var i = 0; i < _indices.Count; i += 3)
        {
            var i0 = _indices[i];
            var i1 = _indices[i + 1];
            var i2 = _indices[i + 2];

            var v0 = _vertices[i0];
            var v1 = _vertices[i1];
            var v2 = _vertices[i2];

            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            var faceNormal = Vector3.Cross(edge1, edge2);

            _normals[i0] += faceNormal;
            _normals[i1] += faceNormal;
            _normals[i2] += faceNormal;
        }

        for (var i = 0; i < _normals.Count; i++)
        {
            _normals[i] = Vector3.Normalize(_normals[i]);
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
        if (!_vertices.IsCreated || _vertices.Count < 3
            || !_indices.IsCreated || _indices.Count < 3
            || !_uvs.IsCreated || _uvs.Count < _vertices.Count)
        {
            return;
        }

        if (!_normals.IsCreated || _normals.Count != _vertices.Count)
        {
            ComputeNormal();
        }

        if (!_tangents.IsCreated)
        {
            _tangents = new(_vertices.Count, Allocator.Persistent);
        }
        else
        {
            _tangents.Clear();
            _tangents.Resize(_vertices.Count);
        }

        var bitangents = new Vector3[_vertices.Count];

        for (var i = 0; i < _indices.Count; i += 3)
        {
            var i0 = _indices[i];
            var i1 = _indices[i + 1];
            var i2 = _indices[i + 2];

            var v0 = _vertices[i0];
            var v1 = _vertices[i1];
            var v2 = _vertices[i2];
            var uv0 = _uvs[i0];
            var uv1 = _uvs[i1];
            var uv2 = _uvs[i2];

            var deltaPos1 = v1 - v0;
            var deltaPos2 = v2 - v0;
            var deltaUV1 = uv1 - uv0;
            var deltaUV2 = uv2 - uv0;

            var r = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV1.Y * deltaUV2.X);
            var tangent = (deltaPos1 * deltaUV2.Y - deltaPos2 * deltaUV1.Y) * r;
            var bitangent = (deltaPos2 * deltaUV1.X - deltaPos1 * deltaUV2.X) * r;

            for (var j = 0; j < 3; j++)
            {
                var idx = _indices[i + j];
                var t = _tangents[idx];
                _tangents[idx] = new Vector4(
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
            var n = _normals![i];
            var t = _tangents[i];
            var t3 = new Vector3(t.X, t.Y, t.Z);

            var proj = n * Vector3.Dot(n, t3);
            t3 = Vector3.Normalize(t3 - proj);

            var b = bitangents[i];
            var w = Vector3.Dot(Vector3.Cross(n, t3), b) < 0.0f ? -1.0f : 1.0f;

            _tangents[i] = new Vector4(t3.X, t3.Y, t3.Z, w);
        }
    }

    public void Clear()
    {
        _vertices.Clear();
        _normals.Clear();
        _tangents.Clear();
        _colors.Clear();
        _uvs.Clear();
        _indices.Clear();
    }

    public void Dispose()
    {
        _vertices.Dispose();
        _normals.Dispose();
        _tangents.Dispose();
        _colors.Dispose();
        _uvs.Dispose();
        _indices.Dispose();

        GC.SuppressFinalize(this);
    }
}