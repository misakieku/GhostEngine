using Ghost.Core;
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
using System.Xml.Linq;

namespace Ghost.Editor.Core.Assets;

internal unsafe class MeshParsingWorkItem : IJob
{
    private readonly string _filePath;
    private readonly AllocationHandle _allocationHandle;
    private readonly MeshAssetSettings _settings;
    private readonly TaskCompletionSource<Result<MeshNode>> _taskCompletionSource;

    public UnsafeList<Vertex> vertices;
    public UnsafeList<uint> indices;

    public Task<Result<MeshNode>> Task => _taskCompletionSource.Task;

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

    private GeometryMeshNode ParseGeometry(ufbx_mesh* pMesh)
    {
        var meshNode = new GeometryMeshNode
        {
            Name = pMesh->name.ToString(),
            Children = Array.Empty<MeshNode>(),
        };

        if (pMesh->num_faces == 0)
        {
            return meshNode;
        }

        var missingNormals = false;
        var missingTangents = false;

        using var flatVertices = new UnsafeList<Vertex>(1024, AllocationHandle.FreeList);

        var maxScratchIndices = (int)(pMesh->max_face_triangles * 3u);

        using var triIndicesArray = new UnsafeArray<uint>(maxScratchIndices, AllocationHandle.FreeList);

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

                var newIndex = (uint)flatVertices.Count;

                flatVertices.Add(vertex);

                if (!missingNormals)
                {
                    missingNormals = normIdx == uint.MaxValue;
                }

                if (!missingTangents)
                {
                    missingTangents = tanIdx == uint.MaxValue || btanIdx == uint.MaxValue;
                }
            }
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
            return;
        }

        MeshOptApi.OptimizeVertexCache((uint*)cachedIndices.GetUnsafePtr(), (uint*)weldedIndices.GetUnsafePtr(), numIndices, numUniqueVertices);

        vertices = new UnsafeList<Vertex>((int)numUniqueVertices, _allocationHandle);
        indices = new UnsafeList<uint>((int)numIndices, _allocationHandle);

        var finalVertexCount = MeshOptApi.OptimizeVertexFetch(vertices.GetUnsafePtr(), (uint*)cachedIndices.GetUnsafePtr(), numIndices, flatVertices.GetUnsafePtr(), numIndices, (nuint)sizeof(Vertex));

        vertices.UnsafeSetCount((int)finalVertexCount);

        MemoryUtility.MemCpy(indices.GetUnsafePtr(), cachedIndices.GetUnsafePtr(), numIndices * sizeof(uint));
        indices.UnsafeSetCount((int)numIndices);

        if (_settings.NormalDataSource == VertexDataSource.Computed || (_settings.NormalDataSource == VertexDataSource.ComputedIfMissing && missingNormals))
        {
            MeshBuilder.ComputeNormal(vertices, indices);
        }

        if (_settings.TangentDataSource == VertexDataSource.Computed || (_settings.TangentDataSource == VertexDataSource.ComputedIfMissing && missingTangents))
        {
            MeshBuilder.ComputeTangents(vertices, indices);
        }
    }

    public void Execute(ref readonly JobExecutionContext context)
    {
        if (!File.Exists(_filePath))
        {
            _taskCompletionSource.SetResult(Result.Failure("Invalid file path."));
            return;
        }

        if (!Path.GetExtension(_filePath).Equals(".obj", StringComparison.OrdinalIgnoreCase)
            && !Path.GetExtension(_filePath).Equals(".fbx", StringComparison.OrdinalIgnoreCase))
        {
            _taskCompletionSource.SetResult(Result.Failure("Unsupported file format. Only .obj and .fbx are supported."));
            return;
        }

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

        using var flatVertices = new UnsafeList<Vertex>(1024, AllocationHandle.FreeList);

        var missingNormals = false;
        var missingTangents = false;

        for (var i = 0u; i < scene.Get()->nodes.count; i++)
        {
            var data = scene.Get()->nodes.data;
            var node = scene.Get()->nodes.data[i];
            if (node->is_root)
            {
                continue;
            }

            if (node->mesh != null)
            {

            }
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

        var numUniqueVertices = UfbxApi.GenerateIndices([stream], weldedIndices, null, &error);
        if (numUniqueVertices == 0 && error.type != ufbx_error_type.UFBX_ERROR_NONE)
        {
            _taskCompletionSource.SetResult(Result.Failure($"Welding failed: {error.description}"));
            return;
        }

        MeshOptApi.OptimizeVertexCache((uint*)cachedIndices.GetUnsafePtr(), (uint*)weldedIndices.GetUnsafePtr(), numIndices, numUniqueVertices);

        vertices = new UnsafeList<Vertex>((int)numUniqueVertices, _allocationHandle);
        indices = new UnsafeList<uint>((int)numIndices, _allocationHandle);

        var finalVertexCount = MeshOptApi.OptimizeVertexFetch(vertices.GetUnsafePtr(), (uint*)cachedIndices.GetUnsafePtr(), numIndices, flatVertices.GetUnsafePtr(), numIndices, (nuint)sizeof(Vertex));

        vertices.UnsafeSetCount((int)finalVertexCount);

        MemoryUtility.MemCpy(indices.GetUnsafePtr(), cachedIndices.GetUnsafePtr(), numIndices * sizeof(uint));
        indices.UnsafeSetCount((int)numIndices);

        if (_settings.NormalDataSource == VertexDataSource.Computed || (_settings.NormalDataSource == VertexDataSource.ComputedIfMissing && missingNormals))
        {
            MeshBuilder.ComputeNormal(vertices, indices);
        }

        if (_settings.TangentDataSource == VertexDataSource.Computed || (_settings.TangentDataSource == VertexDataSource.ComputedIfMissing && missingTangents))
        {
            MeshBuilder.ComputeTangents(vertices, indices);
        }

        _taskCompletionSource.SetResult(Result.Success());
    }
}

public partial class MeshProcessor
{

}
