using Ghost.Graphics.Data;
using System.Numerics;

namespace Ghost.Graphics.Utilities;

public static class MeshBuilder
{
    /// <summary>
    /// Creates a unit cube centered at the origin with size 1.
    /// </summary>
    public static Mesh CreateCube(float size = 1.0f, Color32 color = default)
    {
        var half = size * 0.5f;
        var mesh = new Mesh(24, 36);

        var corners = new Vector3[]
        {
            new(-half, -half, -half), new( half, -half, -half), new( half,  half, -half), new(-half,  half, -half),
            new(-half, -half,  half), new( half, -half,  half), new( half,  half,  half), new(-half,  half,  half)
        };

        int[][] faces =
        [
            [0,1,2,3],
            [5,4,7,6],
            [4,0,3,7],
            [1,5,6,2],
            [3,2,6,7],
            [4,5,1,0]
        ];

        var uvs = new Vector2[] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };

        foreach (var face in faces)
        {
            var baseIndex = mesh.VertexCount;
            for (var i = 0; i < 4; i++)
            {
                mesh.AddVertex(corners[face[i]], Vector3.Zero, Vector4.Zero, color, uvs[i]);
            }

            mesh.AddTriangle(baseIndex + 0, baseIndex + 1, baseIndex + 2);
            mesh.AddTriangle(baseIndex + 0, baseIndex + 2, baseIndex + 3);
        }

        mesh.ComputeNormal();
        mesh.ComputeTangents();
        return mesh;
    }

    /// <summary>
    /// Creates a plane on the XZ axis centered at the origin.
    /// </summary>
    public static Mesh CreatePlane(float width = 1.0f, float depth = 1.0f, Color32 color = default)
    {
        var hw = width * 0.5f;
        var hd = depth * 0.5f;
        var mesh = new Mesh(4, 6);

        mesh.AddVertex(new(-hw, 0, -hd), Vector3.Zero, Vector4.Zero, color, new(0, 0));
        mesh.AddVertex(new(hw, 0, -hd), Vector3.Zero, Vector4.Zero, color, new(1, 0));
        mesh.AddVertex(new(hw, 0, hd), Vector3.Zero, Vector4.Zero, color, new(1, 1));
        mesh.AddVertex(new(-hw, 0, hd), Vector3.Zero, Vector4.Zero, color, new(0, 1));

        mesh.AddTriangle(0, 1, 2);
        mesh.AddTriangle(0, 2, 3);

        mesh.ComputeNormal();
        mesh.ComputeTangents();
        return mesh;
    }

    /// <summary>
    /// Creates a UV sphere centered at the origin.
    /// </summary>
    public static Mesh CreateSphere(int latitudeSegments = 16, int longitudeSegments = 24, float radius = 0.5f, Color32 color = default)
    {
        var mesh = new Mesh((latitudeSegments + 1) * (longitudeSegments + 1), latitudeSegments * longitudeSegments * 6);

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

                mesh.AddVertex(
                    position: new Vector3(x, y, z) * radius,
                    normal: Vector3.Zero,
                    tangent: Vector4.Zero,
                    color: color,
                    uv: new Vector2((float)lon / longitudeSegments, (float)lat / latitudeSegments)
                );
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

                mesh.AddTriangle(i0, i1, i2);
                mesh.AddTriangle(i0, i2, i3);
            }
        }

        mesh.ComputeNormal();
        mesh.ComputeTangents();
        return mesh;
    }
}