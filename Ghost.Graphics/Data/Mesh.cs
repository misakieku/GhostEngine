using Ghost.Graphics.D3D12;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Helpers;
using System.Numerics;
using System.Runtime.CompilerServices;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.Data;

public unsafe sealed class Mesh : IDisposable
{
    private UnsafeList<Vertex> _vertices;
    private UnsafeList<int> _indices;

    private Bounds _boundingBox;

    private IBuffer? _vertexBuffer;
    private IBuffer? _indexBuffer;

    public Span<Vertex> Vertices => _vertices.AsSpan();
    public Span<int> Indices => _indices.AsSpan();
    public Bounds BoundingBox => _boundingBox;

    public uint VertexCount => (uint)_vertices.Count;
    public uint IndexCount => (uint)_indices.Count;

    public uint VertexBufferDescriptorIndex
    {
        get
        {
            if (_vertexBuffer == null || !_vertexBuffer.Handle.IsValid)
            {
                throw new InvalidOperationException("Vertex buffer is not created.");
            }

            var bindlessDesc = _vertexBuffer.Handle.BindlessDescriptor;
            if (!bindlessDesc.IsValid)
            {
                throw new InvalidOperationException("Vertex buffer is not created with bindless.");
            }

            return bindlessDesc.Index;
        }
    }

    public uint IndexBufferDescriptorIndex
    {
        get
        {
            if (_indexBuffer == null || !_indexBuffer.Handle.IsValid)
            {
                throw new InvalidOperationException("Index buffer is not created.");
            }

            var bindlessDesc = _indexBuffer.Handle.BindlessDescriptor;
            if (!bindlessDesc.IsValid)
            {
                throw new InvalidOperationException("Index buffer is not created with bindless.");
            }

            return bindlessDesc.Index;
        }
    }

    public Mesh(int initialVertexCapacity = 256, int initialIndexCapacity = 512)
    {
        _vertices = new(initialVertexCapacity, Allocator.Persistent);
        _indices = new(initialIndexCapacity, Allocator.Persistent);
    }

    public Mesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<int> indices)
        : this(vertices.Length, indices.Length)
    {
        _vertices = new(vertices.Length, Allocator.Persistent);
        _indices = new(indices.Length, Allocator.Persistent);

        _vertices.CopyFrom(vertices);
        _indices.CopyFrom(indices);
    }

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

    public void AddTriangles(params ReadOnlySpan<int> indices)
    {
        if (indices.Length % 3 != 0)
        {
            throw new ArgumentException("The number of indices must be a multiple of 3 to form triangles.");
        }

        foreach (var index in indices)
        {
            if (index < 0 || index >= _vertices.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(indices), "Index out of range for the current vertex count.");
            }

            _indices.Add(index);
        }
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
            _boundingBox = Bounds.Zero;
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

        _boundingBox = new Bounds(min, max);
    }

    /// <summary>
    /// Uploads the mesh data to GPU resources.
    /// </summary>
    public unsafe void UploadMeshData()
    {
        if (VertexCount == 0 || IndexCount == 0)
        {
            return;
        }

        DisposeGpuResources();

        var vertexBufferSize = (uint)(VertexCount * sizeof(Vertex));
        var indexBufferSize = IndexCount * sizeof(int);
        _vertexBuffer = GraphicsBuffer.Create(vertexBufferSize, GraphicsBuffer.Usage.CopyDestination);
        _indexBuffer = GraphicsBuffer.Create(indexBufferSize, GraphicsBuffer.Usage.CopyDestination);

        var uploadBatch = GraphicsPipeline.UploadBatch;
        uploadBatch.Upload(_vertexBuffer.NativeResource, _vertices.AsSpan());
        uploadBatch.Upload(_indexBuffer.NativeResource, _indices.AsSpan());
        uploadBatch.Transition(_vertexBuffer.NativeResource, ResourceStates.CopyDest, ResourceStates.VertexAndConstantBuffer);
        uploadBatch.Transition(_indexBuffer.NativeResource, ResourceStates.CopyDest, ResourceStates.IndexBuffer);

        // Create bindless descriptors for vertex and index buffers
        CreateBindlessDescriptors();
    }

    /// <summary>
    /// Creates SRVs for vertex and index buffers in the bindless descriptor heap
    /// </summary>
    private void CreateBindlessDescriptors()
    {
        if (_vertexBuffer == null || _indexBuffer == null)
        {
            return;
        }

        // Allocate new descriptors from the descriptor allocator
        _vertexBufferDescriptor = GraphicsPipeline.DescriptorAllocator.AllocateBindless();
        _indexBufferDescriptor = GraphicsPipeline.DescriptorAllocator.AllocateBindless();

        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;

        var vertexSrvDesc = new ShaderResourceViewDescription
        {
            Format = Format.R32Typeless,
            ViewDimension = SrvDimension.Buffer,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Anonymous = new()
            {
                Buffer = new()
                {
                    FirstElement = 0,
                    NumElements = (uint)(_vertexBuffer.GPUAddress != 0 ? (VertexCount * sizeof(Vertex)) / 4 : 0), // Divide by 4 for R32 format
                    StructureByteStride = 0,
                    Flags = BufferSrvFlags.Raw
                }
            }
        };

        device->CreateShaderResourceView(_vertexBuffer.NativeResource.Ptr, &vertexSrvDesc, _vertexBufferDescriptor.CpuHandle);

        var indexSrvDesc = new ShaderResourceViewDescription
        {
            Format = Format.R32Typeless,
            ViewDimension = SrvDimension.Buffer,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Anonymous = new()
            {
                Buffer = new()
                {
                    FirstElement = 0,
                    NumElements = IndexCount,
                    StructureByteStride = 0,
                    Flags = BufferSrvFlags.Raw
                }
            }
        };

        device->CreateShaderResourceView(_indexBuffer.NativeResource.Ptr, &indexSrvDesc, _indexBufferDescriptor.CpuHandle);
    }


    internal void MarkNoLongerReadable()
    {
        _vertices.Dispose();
        _indices.Dispose();
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

        if (_vertexBufferDescriptor.IsValid)
        {
            RenderSystem.GraphicsEngine.DescriptorAllocator.Release(_vertexBufferDescriptor);
        }

        if (_indexBufferDescriptor.IsValid)
        {
            RenderSystem.GraphicsEngine.DescriptorAllocator.Release(_indexBufferDescriptor);
        }
    }

    public void Dispose()
    {
        _vertices.Dispose();
        _indices.Dispose();
        DisposeGpuResources();

        GC.SuppressFinalize(this);
    }
}