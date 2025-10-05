using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;

namespace Ghost.Graphics.Data;

public struct Mesh : IHandleType
{
    public UnsafeList<Vertex> vertices;
    public UnsafeList<uint> indices;
    public AABB boundingBox;
    public Handle<GraphicsBuffer> vertexBuffer;
    public Handle<GraphicsBuffer> indexBuffer;

    public Mesh()
    {
        vertexBuffer = Handle<GraphicsBuffer>.Invalid;
        indexBuffer = Handle<GraphicsBuffer>.Invalid;
    }

    internal Mesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices, Handle<GraphicsBuffer> vertexBuffer, Handle<GraphicsBuffer> indexBuffer)
    {
        this.vertices = new(vertices.Length, Allocator.Persistent);
        this.indices = new(indices.Length, Allocator.Persistent);
        this.vertices.CopyFrom(vertices);
        this.indices.CopyFrom(indices);
        this.vertexBuffer = vertexBuffer;
        this.indexBuffer = indexBuffer;

        this.ComputeBounds();
    }

    public void ReleaseCpuResources()
    {
        vertices.Dispose();
        indices.Dispose();
    }
}

public static class MeshExtension
{
    /// <summary>
    /// Computes the bounding box of the mesh based on its vertices.
    /// </summary>
    public static void ComputeBounds(ref this Mesh mesh)
    {
        if (mesh.vertices.Count == 0)
        {
            return;
        }

        var min = new float3(float.MaxValue);
        var max = new float3(float.MinValue);
        foreach (var vertex in mesh.vertices)
        {
            var pos = vertex.position.xyz;
            min = math.min(min, pos);
            max = math.max(max, pos);
        }

        mesh.boundingBox = new AABB(min, max);
    }

    /// <summary>
    /// Auto-compute smooth per-vertex normals.
    /// </summary>
    /// <remarks>
    /// Call this method before vertices and indices are valid.
    /// </remarks>
    public static void ComputeNormal(ref this Mesh mesh)
    {
        if (!mesh.vertices.IsCreated || mesh.vertices.Count < 3
            || !mesh.indices.IsCreated || mesh.indices.Count < 3)
        {
            return;
        }

        for (var i = 0; i < mesh.indices.Count; i += 3)
        {
            var i0 = mesh.indices[i];
            var i1 = mesh.indices[i + 1];
            var i2 = mesh.indices[i + 2];

            var v0 = mesh.vertices[i0];
            var v1 = mesh.vertices[i1];
            var v2 = mesh.vertices[i2];

            var edge1 = v1.position - v0.position;
            var edge2 = v2.position - v0.position;
            var faceNormal = math.cross(edge1.xyz, edge2.xyz);

            mesh.vertices[i0].normal.xyz += faceNormal;
            mesh.vertices[i1].normal.xyz += faceNormal;
            mesh.vertices[i2].normal.xyz += faceNormal;
        }

        for (var i = 0; i < mesh.vertices.Count; i++)
        {
            mesh.vertices[i].normal = math.normalize(mesh.vertices[i].normal);
        }
    }

    /// <summary>
    /// Auto-compute per-vertex tangents.
    /// </summary>
    /// <remarks>
    /// Call this method before vertices, normals, and UVs are valid.
    /// </remarks>
    public static void ComputeTangents(ref this Mesh mesh)
    {
        var bitangents = new float4[mesh.vertices.Count];

        for (var i = 0; i < mesh.indices.Count; i += 3)
        {
            var i0 = mesh.indices[i];
            var i1 = mesh.indices[i + 1];
            var i2 = mesh.indices[i + 2];

            var v0 = mesh.vertices[i0];
            var v1 = mesh.vertices[i1];
            var v2 = mesh.vertices[i2];

            var uv0 = mesh.vertices[i0].uv;
            var uv1 = mesh.vertices[i1].uv;
            var uv2 = mesh.vertices[i2].uv;

            var deltaPos1 = v1.position - v0.position;
            var deltaPos2 = v2.position - v0.position;
            var deltaUV1 = uv1 - uv0;
            var deltaUV2 = uv2 - uv0;

            var r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);
            var tangent = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;
            var bitangent = (deltaPos2 * deltaUV1.x - deltaPos1 * deltaUV2.x) * r;

            for (var j = 0; j < 3; j++)
            {
                var idx = mesh.indices[i + j];
                var t = mesh.vertices[idx].tangent;
                mesh.vertices[idx].tangent.xyz = t.xyz + tangent.xyz;

                bitangents[idx] += bitangent;
            }
        }

        for (var i = 0; i < mesh.vertices.Count; i++)
        {
            var n = mesh.vertices[i].normal;
            var t = mesh.vertices[i].tangent;

            var proj = n * math.dot(n, t);
            t = math.normalize(t - proj);

            var b = bitangents[i];
            var w = math.dot(math.cross(n.xyz, t.xyz), b.xyz) < 0.0f ? -1.0f : 1.0f;

            mesh.vertices[i].tangent = new float4(t.x, t.y, t.z, w);
        }
    }
}