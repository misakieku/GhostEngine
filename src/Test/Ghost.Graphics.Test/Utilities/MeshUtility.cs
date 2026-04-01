using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.MeshOptimizer;
using Ghost.Ufbx;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ghost.Graphics.Test.Utilities;

internal static class MeshUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float4 ComputeTangent(float3 t, float3 n, float3 b)
    {
        var proj = n * math.dot(n, t);
        t = math.normalize(t - proj);
        var w = math.dot(math.cross(n.xyz, t.xyz), b.xyz) < 0.0f ? -1.0f : 1.0f;
        return new float4(t.xyz, w);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float3 RightHandToLeft(float3 p)
    {
        return new float3(p.x, p.y, -p.z);
    }

    public static unsafe Result LoadMesh(string filePath, Allocator allocator, out UnsafeList<Vertex> vertices, out UnsafeList<uint> indices)
    {
        vertices = default;
        indices = default;

        if (!File.Exists(filePath))
        {
            return Result.Failure("Invalid file path.");
        }

        if (!Path.GetExtension(filePath).Equals(".obj", StringComparison.OrdinalIgnoreCase)
            && !Path.GetExtension(filePath).Equals(".fbx", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure("Unsupported file format. Only .obj and .fbx are supported.");
        }

        var load_Opts = new ufbx_load_opts
        {
            target_axes = new ufbx_coordinate_axes
            {
                right = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_X,
                up = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_Y,
                front = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_Z
            },
            obj_axes = new ufbx_coordinate_axes
            {
                right = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_X,
                up = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_Y,
                front = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_NEGATIVE_Z
            },
            // Force X-axis mirroring to correctly convert handedness to Left-Handed,
            // while preserving correct left/right orientation when viewed from the front.
            handedness_conversion_axis = ufbx_mirror_axis.UFBX_MIRROR_AXIS_X,
            space_conversion = ufbx_space_conversion.UFBX_SPACE_CONVERSION_MODIFY_GEOMETRY,
        };
        var error = new ufbx_error();

        using var pool = new MemoryPool<VirtualStack, VirtualStack.CreationOpts>(new VirtualStack.CreationOpts
        {
            reserveCapacity = 256 * 1024 * 1024 // 256 MB should be enough for most models, adjust as needed. Note that this use virtual memory and does not actually consume physical memory until allocations are made.
        });

        using var scope0 = pool.Allocator.CreateScope(pool.AllocationHandle);
        using var str = new UnsafeArray<byte>(Encoding.UTF8.GetByteCount(filePath) + 1, scope0.AllocationHandle);
        var count = Encoding.UTF8.GetBytes(filePath, str.AsSpan());
        str[count] = 0;

        using var scene = new DisposablePtr<ufbx_scene>(ufbx_scene.LoadFile((sbyte*)str.GetUnsafePtr(), &load_Opts, &error));
        if (scene.Get() == null)
        {
            return Result.Failure(error.description.ToString());
        }

        using var flatVertices = new UnsafeList<Vertex>(1024, scope0.AllocationHandle);
        //using var flatIndices = new UnsafeList<uint>(1024, scope0.AllocationHandle);

        var needComputeNormals = false;

        for (var i = 0u; i < scene.Get()->nodes.count; i++)
        {
            var node = scene.Get()->nodes.data[i];
            if (node->is_root)
            {
                continue;
            }

            using var scope1 = pool.Allocator.CreateScope(pool.AllocationHandle);

            if (node->mesh != null)
            {
                var pMesh = node->mesh;
                if (pMesh->num_faces == 0)
                {
                    continue;
                }

                var maxScratchIndices = (int)(pMesh->max_face_triangles * 3u);

                using var triIndicesArray = new UnsafeArray<uint>(maxScratchIndices, scope1.AllocationHandle);

                for (var j = 0u; j < pMesh->num_faces; j++)
                {
                    var face = pMesh->faces.data[j];

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

                        var vertex = new Vertex
                        {
                            position = pMesh->vertex_position.values.data[posIdx],
                            normal = normIdx != uint.MaxValue ? pMesh->vertex_normal.values.data[normIdx] : default,
                            uv = uvIdx != uint.MaxValue ? pMesh->vertex_uv.values.data[uvIdx] : default,
                            color = colIdx != uint.MaxValue ? new Color128(pMesh->vertex_color.values.data[colIdx]) : default,
                        };

                        if (tanIdx != uint.MaxValue)
                        {
                            var t = pMesh->vertex_tangent.values.data[tanIdx];
                            var n = vertex.normal;
                            var b = btanIdx != uint.MaxValue ? pMesh->vertex_bitangent.values.data[btanIdx] : math.cross(n, t);
                            vertex.tangent = ComputeTangent(t, n, b);
                        }

                        var newIndex = (uint)flatVertices.Count;

                        flatVertices.Add(vertex);

                        if (!needComputeNormals)
                        {
                            needComputeNormals = normIdx == uint.MaxValue || tanIdx == uint.MaxValue;
                        }
                    }
                }
            }
        }

        var numIndices = (uint)flatVertices.Count;

        using var weldedIndices = new UnsafeArray<uint>((int)numIndices, scope0.AllocationHandle);
        using var cachedIndices = new UnsafeArray<uint>((int)numIndices, scope0.AllocationHandle);

        var stream = new ufbx_vertex_stream
        {
            data = flatVertices.GetUnsafePtr(),
            vertex_count = numIndices,
            vertex_size = (nuint)sizeof(Vertex)
        };

        var numUniqueVertices = UfbxApi.GenerateIndices([stream], weldedIndices, null, &error);
        if (numUniqueVertices == 0 && error.type != ufbx_error_type.UFBX_ERROR_NONE)
        {
            return Result.Failure($"Welding failed: {error.description}");
        }

        MeshOptApi.OptimizeVertexCache((uint*)cachedIndices.GetUnsafePtr(), (uint*)weldedIndices.GetUnsafePtr(), numIndices, numUniqueVertices);

        vertices = new UnsafeList<Vertex>((int)numUniqueVertices, allocator);
        indices = new UnsafeList<uint>((int)numIndices, allocator);

        var finalVertexCount = MeshOptApi.OptimizeVertexFetch(vertices.GetUnsafePtr(), (uint*)cachedIndices.GetUnsafePtr(), numIndices, flatVertices.GetUnsafePtr(), numIndices, (nuint)sizeof(Vertex));

        vertices.UnsafeSetCount((int)finalVertexCount);

        MemoryUtility.MemCpy(indices.GetUnsafePtr(), cachedIndices.GetUnsafePtr(), numIndices * sizeof(uint));
        indices.UnsafeSetCount((int)numIndices);

        //if (needComputeNormals)
        //{
        //    MeshBuilder.ComputeNormal(vertices, indices);
        //    MeshBuilder.ComputeTangents(vertices, indices);
        //}

        return Result.Success();
    }
}
