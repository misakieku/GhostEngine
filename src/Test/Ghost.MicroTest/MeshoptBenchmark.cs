using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Ghost.MeshOptimizer;
using Ghost.Test.Core;
using Ghost.Ufbx;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ghost.MicroTest;

internal class MeshoptBenchmark : ITest
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float4 ComputeTangent(float3 t, float3 n, float3 b)
    {
        var proj = n * math.dot(n, t);
        t = math.normalize(t - proj);
        var w = math.dot(math.cross(n.xyz, t.xyz), b.xyz) < 0.0f ? -1.0f : 1.0f;
        return new float4(t.xyz, w);
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
            target_axes = ufbx_coordinate_axes.left_handed_y_up,
            obj_axes = ufbx_coordinate_axes.right_handed_y_up,
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

    private UnsafeList<Vertex> _vertices;
    private UnsafeList<uint> _indices;

    private ClodConfig _config;
    private ClodMesh _clodMesh;

    public unsafe void Setup()
    {
        var opts = new AllocationManagerInitOpts
        {
            ArenaCapacity = 1024 * 1024 * 1024, // 1GB
            StackCapacity = 1024 * 1024 * 32, // 32MB
            FreeListConcurrencyLevel = Environment.ProcessorCount,
        };

        AllocationManager.Initialize(opts);

        LoadMesh("F:/c/SimpleRayTracer/native/assets/bunny.obj", Allocator.Persistent, out _vertices, out _indices).ThrowIfFailed();

        _config = new ClodConfig
        {
            maxVertices = 64,
            minTriangles = 32,
            maxTriangles = 124,

            partitionSpatial = true,
            partitionSize = 16,

            clusterSpatial = false,
            clusterSplitFactor = 2.0f,

            optimizeClusters = true,
            optimizeClustersLevel = 1,

            simplifyRatio = 0.5f,
            simplifyThreshold = 0.85f,
            simplifyErrorMergePrevious = 1.0f,
            simplifyErrorFactorSloppy = 2.0f,
            simplifyPermissive = true,
            simplifyFallbackPermissive = false,
            simplifyFallbackSloppy = true,
        };

        // 2. Map Mesh to ClodMesh
        _clodMesh = new ClodMesh
        {
            vertexPositions = (float*)Unsafe.AsPointer(ref _vertices[0].position),
            vertexCount = (nuint)_vertices.Count,
            vertexPositionsStride = (nuint)sizeof(Vertex),
            vertexAttributes = (float*)Unsafe.AsPointer(ref _vertices[0].normal),
            vertexAttributesStride = (nuint)sizeof(Vertex),
            indices = (uint*)_indices.GetUnsafePtr(),
            indexCount = (nuint)_indices.Count,
            attributeProtectMask = 0,
        };
    }

    public unsafe void Run()
    {
        // 3. Build
        var sw = System.Diagnostics.Stopwatch.StartNew();
        MeshletUtility.Build(in _config, in _clodMesh, null, null);
        Console.WriteLine($"Meshlet build time: {sw.Elapsed.TotalSeconds:F3} seconds");
    }

    public void Cleanup()
    {
        _vertices.Dispose();
        _indices.Dispose();

        AllocationManager.Dispose();
    }
}
