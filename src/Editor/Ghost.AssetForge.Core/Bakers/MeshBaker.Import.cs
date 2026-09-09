using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.MeshOptimizer;
using Ghost.Ufbx;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ghost.AssetForge.Core.Bakers;

internal sealed class ParsedMesh : IDisposable
{
    public string Name { get; set; } = string.Empty;
    public UnsafeList<Vertex> Vertices;
    public UnsafeList<uint> Indices;
    public UnsafeArray<MaterialPartInfo> MaterialParts;
    public AABB BoundingBox;

    public void Dispose()
    {
        Vertices.Dispose();
        Indices.Dispose();
        MaterialParts.Dispose();
    }
}

internal static unsafe partial class MeshProcessor
{
    private struct GeometryPart : IDisposable
    {
        public UnsafeList<Vertex> vertices;
        public UnsafeList<uint> indices;
        public int materialIndex;
        public bool missingNormals;
        public bool missingTangents;

        public void Dispose()
        {
            vertices.Dispose();
            indices.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float4 ComputeTangentVector(float3 t, float3 n, float3 b)
    {
        var proj = n * math.dot(n, t);
        t = math.normalize(t - proj);
        var w = math.dot(math.cross(n, t), b) < 0.0f ? -1.0f : 1.0f;
        return new float4(t.xyz, w);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ufbx_coordinate_axis ToUfbxCoordinateAxis(CoordinateAxis axis)
    {
        return axis switch
        {
            CoordinateAxis.PositiveX => ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_X,
            CoordinateAxis.PositiveY => ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_Y,
            CoordinateAxis.PositiveZ => ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_Z,
            CoordinateAxis.NegativeX => ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_NEGATIVE_X,
            CoordinateAxis.NegativeY => ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_NEGATIVE_Y,
            CoordinateAxis.NegativeZ => ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_NEGATIVE_Z,
            _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
        };
    }

    public static void ComputeNormals(Span<Vertex> vertices, Span<uint> indices)
    {
        if (vertices.Length < 3 || indices.Length < 3)
        {
            return;
        }

        for (var i = 0; i < vertices.Length; i++)
        {
            vertices[i].normal = float3.zero;
        }

        for (var i = 0; i < indices.Length; i += 3)
        {
            var i0 = (int)indices[i];
            var i1 = (int)indices[i + 1];
            var i2 = (int)indices[i + 2];

            var p0 = vertices[i0].position;
            var p1 = vertices[i1].position;
            var p2 = vertices[i2].position;

            var e1 = p1 - p0;
            var e2 = p2 - p0;
            var faceNormal = math.cross(e1, e2);

            vertices[i0].normal += faceNormal;
            vertices[i1].normal += faceNormal;
            vertices[i2].normal += faceNormal;
        }

        for (var i = 0; i < vertices.Length; i++)
        {
            var n = vertices[i].normal;
            if (math.lengthsq(n) > 1e-10f)
            {
                vertices[i].normal = math.normalize(n);
            }
            else
            {
                vertices[i].normal = new float3(0, 1, 0);
            }
        }
    }

    public static void ComputeTangents(Span<Vertex> vertices, Span<uint> indices, AllocationHandle allocationHandle)
    {
        if (vertices.Length < 3 || indices.Length < 3)
        {
            return;
        }

        using var tan1 = new UnsafeArray<float3>(vertices.Length, allocationHandle, AllocationOption.Clear);
        using var tan2 = new UnsafeArray<float3>(vertices.Length, allocationHandle, AllocationOption.Clear);

        var pTan1 = (float3*)tan1.GetUnsafePtr();
        var pTan2 = (float3*)tan2.GetUnsafePtr();

        for (var i = 0; i < indices.Length; i += 3)
        {
            var i0 = (int)indices[i];
            var i1 = (int)indices[i + 1];
            var i2 = (int)indices[i + 2];

            var v0 = vertices[i0];
            var v1 = vertices[i1];
            var v2 = vertices[i2];

            var p0 = v0.position;
            var p1 = v1.position;
            var p2 = v2.position;

            var w0 = v0.uv;
            var w1 = v1.uv;
            var w2 = v2.uv;

            var x1 = p1.x - p0.x;
            var x2 = p2.x - p0.x;
            var y1 = p1.y - p0.y;
            var y2 = p2.y - p0.y;
            var z1 = p1.z - p0.z;
            var z2 = p2.z - p0.z;

            var s1 = w1.x - w0.x;
            var s2 = w2.x - w0.x;
            var t1 = w1.y - w0.y;
            var t2 = w2.y - w0.y;

            var r = (s1 * t2 - s2 * t1);
            var invR = MathF.Abs(r) > 1e-10f ? 1.0f / r : 0.0f;

            var sdir = new float3((t2 * x1 - t1 * x2) * invR, (t2 * y1 - t1 * y2) * invR, (t2 * z1 - t1 * z2) * invR);
            var tdir = new float3((s1 * x2 - s2 * x1) * invR, (s1 * y2 - s2 * y1) * invR, (s1 * z2 - s2 * z1) * invR);

            pTan1[i0] += sdir;
            pTan1[i1] += sdir;
            pTan1[i2] += sdir;

            pTan2[i0] += tdir;
            pTan2[i1] += tdir;
            pTan2[i2] += tdir;
        }

        for (var i = 0; i < vertices.Length; i++)
        {
            var n = vertices[i].normal;
            var t = pTan1[i];

            if (math.lengthsq(t) > 1e-10f)
            {
                var tangent = math.normalize(t - n * math.dot(n, t));
                var w = (math.dot(math.cross(n, t), pTan2[i]) < 0.0f) ? -1.0f : 1.0f;
                vertices[i].tangent = new float4(tangent.xyz, w);
            }
            else
            {
                var fallbackT = MathF.Abs(n.x) < 0.9f ? new float3(1, 0, 0) : new float3(0, 1, 0);
                var tangent = math.normalize(fallbackT - n * math.dot(n, fallbackT));
                vertices[i].tangent = new float4(tangent.xyz, 1.0f);
            }
        }
    }

    private static ParsedMesh? ParseMeshGeometry(ufbx_mesh* pMesh, MeshBakeSettings settings, AllocationHandle allocationHandle)
    {
        if (pMesh->num_faces == 0)
        {
            return null;
        }

        var numMaterials = pMesh->materials.count > 0 ? (int)pMesh->materials.count : 1;

        using var materialBuckets = new UnsafeArray<UnsafeList<Vertex>>(numMaterials, allocationHandle);
        using var missingNormalsBucket = new UnsafeArray<bool>(numMaterials, allocationHandle);
        using var missingTangentsBucket = new UnsafeArray<bool>(numMaterials, allocationHandle);

        for (var i = 0; i < numMaterials; i++)
        {
            materialBuckets[i] = new UnsafeList<Vertex>(10240, allocationHandle);
        }

        var maxScratchIndices = (int)(pMesh->max_face_triangles * 3u);
        using var triIndicesArray = new UnsafeArray<uint>(maxScratchIndices, allocationHandle);

        for (var j = 0u; j < pMesh->num_faces; j++)
        {
            var face = pMesh->faces.data[j];
            var materialIdx = pMesh->face_material.count > j ? pMesh->face_material.data[j] : 0;
            if (materialIdx >= numMaterials)
            {
                materialIdx = 0;
            }

            var numTris = UfbxApi.TriangulateFace(triIndicesArray.AsSpan(0, maxScratchIndices), pMesh, face);
            var totalIndices = numTris * 3;

            for (var k = 0; k < totalIndices; k++)
            {
                var ufbxTopologyIndex = triIndicesArray[k];

                var posIdx = pMesh->vertex_position.indices.data[ufbxTopologyIndex];
                var normIdx = pMesh->vertex_normal.exists ? pMesh->vertex_normal.indices.data[ufbxTopologyIndex] : uint.MaxValue;
                var tanIdx = pMesh->vertex_tangent.exists ? pMesh->vertex_tangent.indices.data[ufbxTopologyIndex] : uint.MaxValue;
                var uvIdx = pMesh->vertex_uv.exists ? pMesh->vertex_uv.indices.data[ufbxTopologyIndex] : uint.MaxValue;
                var colIdx = pMesh->vertex_color.exists ? pMesh->vertex_color.indices.data[ufbxTopologyIndex] : uint.MaxValue;
                var btanIdx = pMesh->vertex_bitangent.exists ? pMesh->vertex_bitangent.indices.data[ufbxTopologyIndex] : uint.MaxValue;

                var position = pMesh->vertex_position.values.data[posIdx];
                var normal = normIdx != uint.MaxValue ? pMesh->vertex_normal.values.data[normIdx] : default;
                var uv = uvIdx != uint.MaxValue ? pMesh->vertex_uv.values.data[uvIdx] : default;
                var color = colIdx != uint.MaxValue ? pMesh->vertex_color.values.data[colIdx] : default;

                var vertex = new Vertex
                {
                    position = new float3(position.x, position.y, position.z),
                    normal = new float3(normal.x, normal.y, normal.z),
                    uv = new float2(uv.x, uv.y),
                    color = new Color128(color.x, color.y, color.z, color.w)
                };

                if (tanIdx != uint.MaxValue)
                {
                    var mt = pMesh->vertex_tangent.values.data[tanIdx];
                    var mb = btanIdx != uint.MaxValue ? pMesh->vertex_bitangent.values.data[btanIdx] : default;

                    var t = new float3(mt.x, mt.y, mt.z);
                    var n = vertex.normal;
                    var b = btanIdx != uint.MaxValue ? new float3(mb.x, mb.y, mb.z) : math.cross(n, t);
                    vertex.tangent = ComputeTangentVector(t, n, b);
                }

                materialBuckets[materialIdx].Add(vertex);

                if (!missingNormalsBucket[materialIdx])
                {
                    missingNormalsBucket[materialIdx] = normIdx == uint.MaxValue;
                }

                if (!missingTangentsBucket[materialIdx])
                {
                    missingTangentsBucket[materialIdx] = tanIdx == uint.MaxValue || btanIdx == uint.MaxValue;
                }
            }
        }

        using var partResults = new UnsafeList<GeometryPart>(numMaterials, allocationHandle);

        for (var m = 0; m < numMaterials; m++)
        {
            ref var flatVertices = ref materialBuckets[m];
            if (flatVertices.Count == 0)
            {
                flatVertices.Dispose();
                continue;
            }

            var numIndices = (uint)flatVertices.Count;
            using var weldedIndices = new UnsafeArray<uint>((int)numIndices, allocationHandle);
            using var cachedIndices = new UnsafeArray<uint>((int)numIndices, allocationHandle);

            var stream = new ufbx_vertex_stream
            {
                data = flatVertices.GetUnsafePtr(),
                vertex_count = numIndices,
                vertex_size = (nuint)sizeof(Vertex)
            };

            var error = new ufbx_error();
            var numUniqueVertices = UfbxApi.GenerateIndices([stream], weldedIndices, null, &error);
            if (numUniqueVertices == 0 && error.type != ufbx_error_type.UFBX_ERROR_NONE)
            {
                flatVertices.Dispose();
                continue;
            }

            MeshOptApi.OptimizeVertexCache((uint*)cachedIndices.GetUnsafePtr(), (uint*)weldedIndices.GetUnsafePtr(), numIndices, numUniqueVertices);

            var partVertices = new UnsafeList<Vertex>((int)numUniqueVertices, allocationHandle);
            var partIndices = new UnsafeList<uint>((int)numIndices, allocationHandle);

            var finalVertexCount = MeshOptApi.OptimizeVertexFetch(
                partVertices.GetUnsafePtr(),
                (uint*)cachedIndices.GetUnsafePtr(),
                numIndices,
                flatVertices.GetUnsafePtr(),
                numIndices,
                (nuint)sizeof(Vertex));

            partVertices.UnsafeSetCount((int)finalVertexCount);
            MemoryUtility.MemCpy(partIndices.GetUnsafePtr(), cachedIndices.GetUnsafePtr(), numIndices * sizeof(uint));
            partIndices.UnsafeSetCount((int)numIndices);

            var part = new GeometryPart
            {
                vertices = partVertices,
                indices = partIndices,
                materialIndex = m,
                missingNormals = missingNormalsBucket[m],
                missingTangents = missingTangentsBucket[m]
            };

            partResults.Add(part);
            flatVertices.Dispose();
        }

        if (partResults.Count == 0)
        {
            return null;
        }

        var totalVertexCount = 0;
        var totalIndexCount = 0;
        for (var i = 0; i < partResults.Count; i++)
        {
            totalVertexCount += partResults[i].vertices.Count;
            totalIndexCount += partResults[i].indices.Count;
        }

        var mergedVertices = new UnsafeList<Vertex>(totalVertexCount, allocationHandle);
        var mergedIndices = new UnsafeList<uint>(totalIndexCount, allocationHandle);
        var materialParts = new UnsafeArray<MaterialPartInfo>(partResults.Count, allocationHandle);

        var vertexOffset = 0;
        var indexOffset = 0;

        for (var i = 0; i < partResults.Count; i++)
        {
            ref var part = ref partResults[i];

            if (settings.NormalDataSource == VertexDataSource.Computed || (settings.NormalDataSource == VertexDataSource.ComputedIfMissing && part.missingNormals))
            {
                ComputeNormals(part.vertices.AsSpan(), part.indices.AsSpan());
            }

            if (settings.TangentDataSource == VertexDataSource.Computed || (settings.TangentDataSource == VertexDataSource.ComputedIfMissing && part.missingTangents))
            {
                ComputeTangents(part.vertices.AsSpan(), part.indices.AsSpan(), allocationHandle);
            }

            materialParts[i] = new MaterialPartInfo
            {
                materialIndex = part.materialIndex,
                vertexStart = vertexOffset,
                vertexCount = part.vertices.Count,
                indexStart = indexOffset,
                indexCount = part.indices.Count,
            };

            mergedVertices.AddRange(part.vertices.AsSpan());

            for (var j = 0; j < part.indices.Count; j++)
            {
                mergedIndices.Add(part.indices[j] + (uint)vertexOffset);
            }

            vertexOffset += part.vertices.Count;
            indexOffset += part.indices.Count;

            part.Dispose();
        }

        var min = new float3(float.MaxValue);
        var max = new float3(float.MinValue);
        for (var i = 0; i < mergedVertices.Count; i++)
        {
            var p = mergedVertices[i].position;
            min = math.min(min, p);
            max = math.max(max, p);
        }

        return new ParsedMesh
        {
            Name = pMesh->name.ToString(),
            Vertices = mergedVertices,
            Indices = mergedIndices,
            MaterialParts = materialParts,
            BoundingBox = new AABB(min, max)
        };
    }

    public static Result<List<ParsedMesh>> ParseModel(string filePath, MeshBakeSettings settings, AllocationHandle allocationHandle)
    {
        var error = new ufbx_error();
        var loadOpts = new ufbx_load_opts
        {
            target_unit_meters = settings.UnitMeterScale,
            target_axes = ufbx_coordinate_axes.left_handed_y_up,
            handedness_conversion_axis = ufbx_mirror_axis.UFBX_MIRROR_AXIS_Z,
            space_conversion = ufbx_space_conversion.UFBX_SPACE_CONVERSION_MODIFY_GEOMETRY,
        };

        var ext = Path.GetExtension(filePath);
        if (ext.Equals(".obj", StringComparison.OrdinalIgnoreCase))
        {
            loadOpts.obj_axes = new ufbx_coordinate_axes
            {
                right = ToUfbxCoordinateAxis(settings.ObjectRightAxis),
                up = ToUfbxCoordinateAxis(settings.ObjectUpAxis),
                front = ToUfbxCoordinateAxis(settings.ObjectForwardAxis)
            };
            loadOpts.obj_unit_meters = settings.UnitMeterScale;
            loadOpts.obj_search_mtl_by_filename = true;
        }

        using var str = new UnsafeArray<byte>(Encoding.UTF8.GetByteCount(filePath) + 1, allocationHandle);
        var count = Encoding.UTF8.GetBytes(filePath, str.AsSpan());
        str[count] = 0;

        using var scene = new DisposablePtr<ufbx_scene>(ufbx_scene.LoadFile((sbyte*)str.GetUnsafePtr(), &loadOpts, &error));
        if (scene.Get() == null)
        {
            return Result.Failure<List<ParsedMesh>>(error.description.ToString());
        }

        var results = new List<ParsedMesh>();

        for (var i = 0u; i < scene.Get()->meshes.count; i++)
        {
            var pMesh = scene.Get()->meshes.data[i];
            var parsed = ParseMeshGeometry(pMesh, settings, allocationHandle);
            if (parsed != null)
            {
                results.Add(parsed);
            }
        }

        if (results.Count == 0)
        {
            return Result.Failure<List<ParsedMesh>>("No valid geometry found in the model file.");
        }

        return Result.Success(results);
    }
}
