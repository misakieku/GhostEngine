namespace Ghost.Ufbx;

public unsafe ref struct Mesh
{
    private ufbx_mesh* _ptr;

    internal Mesh(ufbx_mesh* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint FindFaceIndex(nuint index)
    {
        return Api.ufbx_find_face_index(_ptr, index);
    }

    public void ComputeTopology(TopoEdge topo, nuint numTopo)
    {
        Api.ufbx_compute_topology(_ptr, topo.GetUnsafePtr(), numTopo);
    }

    public nuint GenerateNormalMapping(TopoEdge topo, nuint numTopo, uint* normalIndices, nuint numNormalIndices, bool assumeSmooth)
    {
        return Api.ufbx_generate_normal_mapping(_ptr, topo.GetUnsafePtr(), numTopo, normalIndices, numNormalIndices, assumeSmooth);
    }

    public void ComputeNormals(VertexVec3 positions, uint* normalIndices, nuint numNormalIndices, Misaki.HighPerformance.Mathematics.float3* normals, nuint numNormals)
    {
        Api.ufbx_compute_normals(_ptr, positions.GetUnsafePtr(), normalIndices, numNormalIndices, normals, numNormals);
    }

    public Mesh SubdivideMesh(nuint level, SubdivideOpts opts, Error error)
    {
        return new(Api.ufbx_subdivide_mesh(_ptr, level, opts.GetUnsafePtr(), error.GetUnsafePtr()));
    }

    public void FreeMesh()
    {
        Api.ufbx_free_mesh(_ptr);
    }

    public void RetainMesh()
    {
        Api.ufbx_retain_mesh(_ptr);
    }

    public nuint NumVertices => _ptr->num_vertices;

    public nuint NumIndices => _ptr->num_indices;

    public nuint NumFaces => _ptr->num_faces;

    public nuint NumTriangles => _ptr->num_triangles;

    public nuint NumEdges => _ptr->num_edges;

    public nuint MaxFaceTriangles => _ptr->max_face_triangles;

    public nuint NumEmptyFaces => _ptr->num_empty_faces;

    public nuint NumPointFaces => _ptr->num_point_faces;

    public nuint NumLineFaces => _ptr->num_line_faces;

    public ReadOnlySpan<ufbx_face> Faces => _ptr->faces.data == null ? ReadOnlySpan<ufbx_face>.Empty : new ReadOnlySpan<ufbx_face>(_ptr->faces.data, checked((int)_ptr->faces.count));

    public ReadOnlySpan<bool> FaceSmoothing => _ptr->face_smoothing.data == null ? ReadOnlySpan<bool>.Empty : new ReadOnlySpan<bool>(_ptr->face_smoothing.data, checked((int)_ptr->face_smoothing.count));

    public ReadOnlySpan<uint> FaceMaterial => _ptr->face_material.data == null ? ReadOnlySpan<uint>.Empty : new ReadOnlySpan<uint>(_ptr->face_material.data, checked((int)_ptr->face_material.count));

    public ReadOnlySpan<uint> FaceGroup => _ptr->face_group.data == null ? ReadOnlySpan<uint>.Empty : new ReadOnlySpan<uint>(_ptr->face_group.data, checked((int)_ptr->face_group.count));

    public ReadOnlySpan<bool> FaceHole => _ptr->face_hole.data == null ? ReadOnlySpan<bool>.Empty : new ReadOnlySpan<bool>(_ptr->face_hole.data, checked((int)_ptr->face_hole.count));

    public ReadOnlySpan<ufbx_edge> Edges => _ptr->edges.data == null ? ReadOnlySpan<ufbx_edge>.Empty : new ReadOnlySpan<ufbx_edge>(_ptr->edges.data, checked((int)_ptr->edges.count));

    public ReadOnlySpan<bool> EdgeSmoothing => _ptr->edge_smoothing.data == null ? ReadOnlySpan<bool>.Empty : new ReadOnlySpan<bool>(_ptr->edge_smoothing.data, checked((int)_ptr->edge_smoothing.count));

    public ReadOnlySpan<float> EdgeCrease => _ptr->edge_crease.data == null ? ReadOnlySpan<float>.Empty : new ReadOnlySpan<float>(_ptr->edge_crease.data, checked((int)_ptr->edge_crease.count));

    public ReadOnlySpan<bool> EdgeVisibility => _ptr->edge_visibility.data == null ? ReadOnlySpan<bool>.Empty : new ReadOnlySpan<bool>(_ptr->edge_visibility.data, checked((int)_ptr->edge_visibility.count));

    public ReadOnlySpan<uint> VertexIndices => _ptr->vertex_indices.data == null ? ReadOnlySpan<uint>.Empty : new ReadOnlySpan<uint>(_ptr->vertex_indices.data, checked((int)_ptr->vertex_indices.count));

    public ReadOnlySpan<Misaki.HighPerformance.Mathematics.float3> Vertices => _ptr->vertices.data == null ? ReadOnlySpan<Misaki.HighPerformance.Mathematics.float3>.Empty : new ReadOnlySpan<Misaki.HighPerformance.Mathematics.float3>(_ptr->vertices.data, checked((int)_ptr->vertices.count));

    public ReadOnlySpan<uint> VertexFirstIndex => _ptr->vertex_first_index.data == null ? ReadOnlySpan<uint>.Empty : new ReadOnlySpan<uint>(_ptr->vertex_first_index.data, checked((int)_ptr->vertex_first_index.count));

    public VertexVec3 VertexPosition => new((ufbx_vertex_vec3*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->vertex_position));

    public VertexVec3 VertexNormal => new((ufbx_vertex_vec3*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->vertex_normal));

    public VertexVec2 VertexUv => new((ufbx_vertex_vec2*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->vertex_uv));

    public VertexVec3 VertexTangent => new((ufbx_vertex_vec3*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->vertex_tangent));

    public VertexVec3 VertexBitangent => new((ufbx_vertex_vec3*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->vertex_bitangent));

    public VertexVec4 VertexColor => new((ufbx_vertex_vec4*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->vertex_color));

    public VertexReal VertexCrease => new((ufbx_vertex_real*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->vertex_crease));

    public ReadOnlySpan<ufbx_uv_set> UvSets => _ptr->uv_sets.data == null ? ReadOnlySpan<ufbx_uv_set>.Empty : new ReadOnlySpan<ufbx_uv_set>(_ptr->uv_sets.data, checked((int)_ptr->uv_sets.count));

    public ReadOnlySpan<ufbx_color_set> ColorSets => _ptr->color_sets.data == null ? ReadOnlySpan<ufbx_color_set>.Empty : new ReadOnlySpan<ufbx_color_set>(_ptr->color_sets.data, checked((int)_ptr->color_sets.count));

    public MaterialList Materials => new(_ptr->materials.data, _ptr->materials.count);

    public ReadOnlySpan<ufbx_face_group> FaceGroups => _ptr->face_groups.data == null ? ReadOnlySpan<ufbx_face_group>.Empty : new ReadOnlySpan<ufbx_face_group>(_ptr->face_groups.data, checked((int)_ptr->face_groups.count));

    public ReadOnlySpan<ufbx_mesh_part> MaterialParts => _ptr->material_parts.data == null ? ReadOnlySpan<ufbx_mesh_part>.Empty : new ReadOnlySpan<ufbx_mesh_part>(_ptr->material_parts.data, checked((int)_ptr->material_parts.count));

    public ReadOnlySpan<ufbx_mesh_part> FaceGroupParts => _ptr->face_group_parts.data == null ? ReadOnlySpan<ufbx_mesh_part>.Empty : new ReadOnlySpan<ufbx_mesh_part>(_ptr->face_group_parts.data, checked((int)_ptr->face_group_parts.count));

    public ReadOnlySpan<uint> MaterialPartUsageOrder => _ptr->material_part_usage_order.data == null ? ReadOnlySpan<uint>.Empty : new ReadOnlySpan<uint>(_ptr->material_part_usage_order.data, checked((int)_ptr->material_part_usage_order.count));

    public bool SkinnedIsLocal => _ptr->skinned_is_local;

    public VertexVec3 SkinnedPosition => new((ufbx_vertex_vec3*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->skinned_position));

    public VertexVec3 SkinnedNormal => new((ufbx_vertex_vec3*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->skinned_normal));

    public SkinDeformerList SkinDeformers => new(_ptr->skin_deformers.data, _ptr->skin_deformers.count);

    public BlendDeformerList BlendDeformers => new(_ptr->blend_deformers.data, _ptr->blend_deformers.count);

    public CacheDeformerList CacheDeformers => new(_ptr->cache_deformers.data, _ptr->cache_deformers.count);

    public ElementList AllDeformers => new(_ptr->all_deformers.data, _ptr->all_deformers.count);

    public uint SubdivisionPreviewLevels => _ptr->subdivision_preview_levels;

    public uint SubdivisionRenderLevels => _ptr->subdivision_render_levels;

    public ufbx_subdivision_display_mode SubdivisionDisplayMode => _ptr->subdivision_display_mode;

    public ufbx_subdivision_boundary SubdivisionBoundary => _ptr->subdivision_boundary;

    public ufbx_subdivision_boundary SubdivisionUvBoundary => _ptr->subdivision_uv_boundary;

    public bool ReversedWinding => _ptr->reversed_winding;

    public bool GeneratedNormals => _ptr->generated_normals;

    public bool SubdivisionEvaluated => _ptr->subdivision_evaluated;

    public bool HasSubdivisionResult => _ptr->subdivision_result != null;
    public SubdivisionResult SubdivisionResult => _ptr->subdivision_result != null ? new(_ptr->subdivision_result) : throw new InvalidOperationException("SubdivisionResult is null.");

    public bool FromTessellatedNurbs => _ptr->from_tessellated_nurbs;

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    public NodeList Instances => new(_ptr->instances.data, _ptr->instances.count);

    internal ufbx_mesh* GetUnsafePtr() => _ptr;
}
