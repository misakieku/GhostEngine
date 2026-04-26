using Ghost.Core;
using Ghost.Engine.Utilities;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Ghost.MeshOptimizer;
using Ghost.Ufbx;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ghost.Editor.Core.Assets;

internal readonly unsafe struct MeshParsingWorkItem : IJob
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

    private readonly string _filePath;
    private readonly AllocationHandle _allocationHandle;
    private readonly MeshAssetSettings _settings;
    private readonly TaskCompletionSource<Result<MeshNode>> _taskCompletionSource;

    public readonly Task<Result<MeshNode>> Task => _taskCompletionSource.Task;

    public MeshParsingWorkItem(string filePath, AllocationHandle allocationHandle, MeshAssetSettings settings)
    {
        _filePath = filePath;
        _allocationHandle = allocationHandle;
        _settings = settings;
        _taskCompletionSource = new TaskCompletionSource<Result<MeshNode>>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float4 ComputeTangent(float3 t, float3 n, float3 b)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float4x4 ToFloat4x4(ufbx_vec3 t, ufbx_quat q, ufbx_vec3 s)
    {
        return float4x4.TRS(
            new float3(t.x, t.y, t.z),
            new quaternion(q.x, q.y, q.z, q.w),
            new float3(s.x, s.y, s.z)
        );
    }

    private MeshNode ParseHierarchy(ufbx_node* node)
    {
        var children = new List<MeshNode>();
        var meshNode = new MeshNode
        {
            Name = node->name.ToString(),
            LocalTransform = ToFloat4x4(node->local_transform.translation, node->local_transform.rotation, node->local_transform.scale),
            Children = children
        };

        if (node->mesh != null)
        {
            var geoNode = ParseGeometry(node->mesh);
            if (geoNode != null)
            {
                children.Add(geoNode);
            }
        }

        // TODO: Handle lights, cameras, and other node types.

        for (var i = 0u; i < node->children.count; i++)
        {
            children.Add(ParseHierarchy(node->children.data[i]));
        }

        return meshNode;
    }

    private GeometryMeshNode? ParseGeometry(ufbx_mesh* pMesh)
    {
        if (pMesh->num_faces == 0)
        {
            return null;
        }

        var numMaterials = pMesh->materials.count > 0 ? (int)pMesh->materials.count : 1;

        // Bucket faces by material

        using var materialBuckets = new UnsafeArray<UnsafeList<Vertex>>(numMaterials, AllocationHandle.FreeList);
        using var missingNormalsBucket = new UnsafeArray<bool>(numMaterials, AllocationHandle.FreeList);
        using var missingTangentsBucket = new UnsafeArray<bool>(numMaterials, AllocationHandle.FreeList);

        for (var i = 0; i < numMaterials; i++)
        {
            materialBuckets[i] = new UnsafeList<Vertex>(1024, AllocationHandle.FreeList);
        }

        var maxScratchIndices = (int)(pMesh->max_face_triangles * 3u);

        using var triIndicesArray = new UnsafeArray<uint>(maxScratchIndices, AllocationHandle.FreeList);

        for (var j = 0u; j < pMesh->num_faces; j++)
        {
            var face = pMesh->faces.data[j];
            var materialIdx = pMesh->face_material.count > j ? pMesh->face_material.data[j] : 0;

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
                    vertex.tangent = ComputeTangent(t, n, b);
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

        // Per-material weld + optimize, collect intermediate results

        using var partResults = new UnsafeList<GeometryPart>(numMaterials, AllocationHandle.FreeList);

        for (var m = 0; m < numMaterials; m++)
        {
            ref var flatVertices = ref materialBuckets[m];
            if (flatVertices.Count == 0)
            {
                flatVertices.Dispose();
                continue;
            }

            var numIndices = (uint)flatVertices.Count;

            using var weldedIndices = new UnsafeArray<uint>((int)numIndices, AllocationHandle.FreeList);
            using var cachedIndices = new UnsafeArray<uint>((int)numIndices, AllocationHandle.FreeList);

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

            // Allocate temporary per-part buffers (will be merged then disposed)
            var partVertices = new UnsafeList<Vertex>((int)numUniqueVertices, AllocationHandle.FreeList);
            var partIndices = new UnsafeList<uint>((int)numIndices, AllocationHandle.FreeList);

            var finalVertexCount = MeshOptApi.OptimizeVertexFetch(partVertices.GetUnsafePtr(), (uint*)cachedIndices.GetUnsafePtr(), numIndices, flatVertices.GetUnsafePtr(), numIndices, (nuint)sizeof(Vertex));

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

        // Merge all material parts into one unified vertex/index buffer

        var totalVertexCount = 0;
        var totalIndexCount = 0;
        for (var i = 0; i < partResults.Count; i++)
        {
            totalVertexCount += partResults[i].vertices.Count;
            totalIndexCount += partResults[i].indices.Count;
        }

        var mergedVertices = new UnsafeList<Vertex>(totalVertexCount, _allocationHandle);
        var mergedIndices = new UnsafeList<uint>(totalIndexCount, _allocationHandle);
        var materialParts = new UnsafeArray<MaterialPartInfo>(partResults.Count, _allocationHandle);

        var vertexOffset = 0;
        var indexOffset = 0;

        for (var i = 0; i < partResults.Count; i++)
        {
            ref var part = ref partResults[i];

            // Compute normals/tangents per-part before merge (requires local indices)
            if (_settings.NormalDataSource == VertexDataSource.Computed || (_settings.NormalDataSource == VertexDataSource.ComputedIfMissing && part.missingNormals))
            {
                MeshBuilder.ComputeNormal(part.vertices, part.indices);
            }

            if (_settings.TangentDataSource == VertexDataSource.Computed || (_settings.TangentDataSource == VertexDataSource.ComputedIfMissing && part.missingTangents))
            {
                MeshBuilder.ComputeTangents(part.vertices, part.indices);
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

            // Rebase indices to global vertex space
            for (var j = 0; j < part.indices.Count; j++)
            {
                mergedIndices.Add(part.indices[j] + (uint)vertexOffset);
            }

            vertexOffset += part.vertices.Count;
            indexOffset += part.indices.Count;

            part.Dispose();
        }

        return new GeometryMeshNode
        {
            Name = pMesh->name.ToString(),
            LocalTransform = float4x4.identity,
            Vertices = mergedVertices,
            Indices = mergedIndices,
            MaterialParts = materialParts,
        };
    }

    public void Execute(ref readonly JobExecutionContext context)
    {
        var error = new ufbx_error();
        var load_Opts = new ufbx_load_opts
        {
            target_unit_meters = 1.0f,
            target_axes = ufbx_coordinate_axes.left_handed_y_up,
            // Force z-axis mirroring to correctly convert handedness to Left-Handed,
            // while preserving correct left/right orientation when viewed from the front.
            handedness_conversion_axis = ufbx_mirror_axis.UFBX_MIRROR_AXIS_Z,
            space_conversion = ufbx_space_conversion.UFBX_SPACE_CONVERSION_MODIFY_GEOMETRY,
        };

        if (_settings is ObjAssetSettings objSettings)
        {
            load_Opts.obj_axes = new ufbx_coordinate_axes
            {
                right = ToUfbxCoordinateAxis(objSettings.ObjectRightAxis),
                up = ToUfbxCoordinateAxis(objSettings.ObjectUpAxis),
                front = ToUfbxCoordinateAxis(objSettings.ObjectForwardAxis)
            };

            load_Opts.obj_unit_meters = objSettings.UnitMeterScale;
            load_Opts.obj_search_mtl_by_filename = true;
        }

        using var str = new UnsafeArray<byte>(Encoding.UTF8.GetByteCount(_filePath) + 1, AllocationHandle.FreeList);
        var count = Encoding.UTF8.GetBytes(_filePath, str.AsSpan());
        str[count] = 0;

        using var scene = new DisposablePtr<ufbx_scene>(ufbx_scene.LoadFile((sbyte*)str.GetUnsafePtr(), &load_Opts, &error));
        if (scene.Get() == null)
        {
            _taskCompletionSource.SetResult(Result.Failure(error.description.ToString()));
            return;
        }

        var rootNode = ParseHierarchy(scene.Get()->root_node);
        rootNode.Name = Path.GetFileNameWithoutExtension(_filePath);

        _taskCompletionSource.SetResult(Result.Success(rootNode));
    }
}

public partial class MeshProcessor
{

}
