using Ghost.Graphics.Data;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Graphics.Utilities;

public unsafe static class MeshBuilder
{
    /// <summary>
    /// Creates a unit cube centered at the origin with size 1.
    /// </summary>
    public static void CreateCube(float size, Color128 color, out UnsafeList<Vertex> vertices, out UnsafeList<uint> indices)
    {
        var half = size * 0.5f;

        vertices = new UnsafeList<Vertex>(24, Allocator.Persistent);
        indices = new UnsafeList<uint>(36, Allocator.Persistent);

        var corners = new float4[]
        {
            new (-half, -half, -half, 1.0f), new (half, -half, -half, 1.0f),
            new (half, half, -half, 1.0f), new (-half, half, -half, 1.0f),
            new (-half, -half, half, 1.0f), new (half, -half, half, 1.0f),
            new (half, half, half, 1.0f), new (-half, half, half, 1.0f)
        };

        var faces = stackalloc int[]
        {
            0,1,2,3,
            5,4,7,6,
            4,0,3,7,
            1,5,6,2,
            3,2,6,7,
            4,5,1,0
        };

        var uvs = stackalloc float2[] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };

        for (var f = 0; f < 6; f++)
        {
            var face = &faces[f * 4];
            var baseIndex = (uint)vertices.Count;
            for (var i = 0; i < 4; i++)
            {
                var vertex = new Vertex
                {
                    position = corners[face[i]],
                    normal = float4.zero,
                    tangent = float4.zero,
                    color = color,
                    uv = new(uvs[i], 0.0f, 0.0f)
                };

                vertices.Add(vertex);
            }

            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
        }

        ComputeNormal(vertices, indices);
        ComputeTangents(vertices, indices);
    }


    /// <summary>
    /// Creates a plane on the XZ axis centered at the origin.
    /// </summary>
    public static void CreatePlane(float width, float depth, Color128 color, out UnsafeList<Vertex> vertices, out UnsafeList<uint> indices)
    {
        var hw = width * 0.5f;
        var hd = depth * 0.5f;

        vertices = new UnsafeList<Vertex>(4, Allocator.Persistent);
        indices = new UnsafeList<uint>(6, Allocator.Persistent);

        vertices.Add(new Vertex()
        {
            position = new(-hw, 0.0f, -hd, 0.0f),
            normal = float4.zero,
            tangent = float4.zero,
            color = color,
            uv = new(0.0f)
        });

        vertices.Add(new Vertex()
        {
            position = new(hw, 0.0f, -hd, 0.0f),
            normal = float4.zero,
            tangent = float4.zero,
            color = color,
            uv = new(1.0f, 0.0f, 0.0f, 0.0f)
        });

        vertices.Add(new Vertex()
        {
            position = new(hw, 0.0f, hd, 0.0f),
            normal = float4.zero,
            tangent = float4.zero,
            color = color,
            uv = new(1.0f, 1.0f, 0.0f, 0.0f)
        });

        vertices.Add(new Vertex()
        {
            position = new(-hw, 0.0f, hd, 0.0f),
            normal = float4.zero,
            tangent = float4.zero,
            color = color,
            uv = new(0.0f, 1.0f, 0.0f, 0.0f)
        });

        indices.Add(0);
        indices.Add(1);
        indices.Add(2);
        indices.Add(0);
        indices.Add(2);
        indices.Add(3);

        ComputeNormal(vertices, indices);
        ComputeTangents(vertices, indices);
    }

    /// <summary>
    /// Creates a UV sphere centered at the origin.
    /// </summary>
    public static void CreateSphere(int latitudeSegments, int longitudeSegments, float radius, Color128 color, out UnsafeList<Vertex> vertices, out UnsafeList<uint> indices)
    {
        vertices = new UnsafeList<Vertex>((latitudeSegments + 1) * (longitudeSegments + 1), Allocator.Persistent);
        indices = new UnsafeList<uint>(latitudeSegments * longitudeSegments * 6, Allocator.Persistent);

        // Vertices
        for (var lat = 0; lat <= latitudeSegments; lat++)
        {
            var theta = (float)lat / latitudeSegments * MathF.PI;
            var sinTheta = MathF.Sin(theta);
            var cosTheta = MathF.Cos(theta);

            for (var lon = 0; lon <= longitudeSegments; lon++)
            {
                var phi = (float)lon / longitudeSegments * 2 * MathF.PI;
                var sinPhi = MathF.Sin(phi);
                var cosPhi = MathF.Cos(phi);

                var x = cosPhi * sinTheta;
                var y = cosTheta;
                var z = sinPhi * sinTheta;

                vertices.Add(new Vertex
                {
                    position = new float4(x, y, z, 0.0f) * radius,
                    normal = float4.zero,
                    tangent = float4.zero,
                    color = color,
                    uv = new float4((float)lon / longitudeSegments, (float)lat / latitudeSegments, 0.0f, 0.0f)
                });
            }
        }

        // Indices
        for (var lat = 0; lat < latitudeSegments; lat++)
        {
            for (var lon = 0; lon < longitudeSegments; lon++)
            {
                var i0 = lat * (longitudeSegments + 1) + lon;
                var i1 = i0 + longitudeSegments + 1;
                var i2 = i1 + 1;
                var i3 = i0 + 1;

                indices.Add((uint)i0);
                indices.Add((uint)i1);
                indices.Add((uint)i2);
                indices.Add((uint)i0);
                indices.Add((uint)i2);
                indices.Add((uint)i3);
            }
        }

        ComputeNormal(vertices, indices);
        ComputeTangents(vertices, indices);
    }

    /// <summary>
    /// Auto-compute smooth per-vertex normals.
    /// </summary>
    /// <param name="vertices">The vertex list.</param>
    /// <param name="indices">The index list.</param>
    public static void ComputeNormal(UnsafeList<Vertex> vertices, UnsafeList<uint> indices)
    {
        if (!vertices.IsCreated || vertices.Count < 3
            || !indices.IsCreated || indices.Count < 3)
        {
            return;
        }

        for (var i = 0; i < indices.Count; i += 3)
        {
            var i0 = indices[i];
            var i1 = indices[i + 1];
            var i2 = indices[i + 2];

            var v0 = vertices[i0];
            var v1 = vertices[i1];
            var v2 = vertices[i2];

            var edge1 = v1.position - v0.position;
            var edge2 = v2.position - v0.position;
            var faceNormal = math.cross(edge1.xyz, edge2.xyz);

            vertices[i0].normal.xyz += faceNormal;
            vertices[i1].normal.xyz += faceNormal;
            vertices[i2].normal.xyz += faceNormal;
        }

        for (var i = 0; i < vertices.Count; i++)
        {
            vertices[i].normal = math.normalize(vertices[i].normal);
        }
    }

    /// <summary>
    /// Auto-compute per-vertex tangents.
    /// </summary>
    /// <param name="vertices">The vertex list.</param>
    /// <param name="indices">The index list.</param>
    public static void ComputeTangents(UnsafeList<Vertex> vertices, UnsafeList<uint> indices)
    {
        using var scope = AllocationManager.CreateStackScope();
        var bitangents = new UnsafeArray<float4>(vertices.Count, Allocator.Stack);

        for (var i = 0; i < indices.Count; i += 3)
        {
            var i0 = indices[i];
            var i1 = indices[i + 1];
            var i2 = indices[i + 2];

            var v0 = vertices[i0];
            var v1 = vertices[i1];
            var v2 = vertices[i2];

            var uv0 = vertices[i0].uv;
            var uv1 = vertices[i1].uv;
            var uv2 = vertices[i2].uv;

            var deltaPos1 = v1.position - v0.position;
            var deltaPos2 = v2.position - v0.position;
            var deltaUV1 = uv1 - uv0;
            var deltaUV2 = uv2 - uv0;

            var r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);
            var tangent = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;
            var bitangent = (deltaPos2 * deltaUV1.x - deltaPos1 * deltaUV2.x) * r;

            for (var j = 0; j < 3; j++)
            {
                var idx = indices[i + j];
                var t = vertices[idx].tangent;
                vertices[idx].tangent.xyz = t.xyz + tangent.xyz;

                bitangents[idx] += bitangent;
            }
        }

        for (var i = 0; i < vertices.Count; i++)
        {
            var n = vertices[i].normal;
            var t = vertices[i].tangent;

            var proj = n * math.dot(n, t);
            t = math.normalize(t - proj);

            var b = bitangents[i];
            var w = math.dot(math.cross(n.xyz, t.xyz), b.xyz) < 0.0f ? -1.0f : 1.0f;

            vertices[i].tangent = new float4(t.x, t.y, t.z, w);
        }
    }
}