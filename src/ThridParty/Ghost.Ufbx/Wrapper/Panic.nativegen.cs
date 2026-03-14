namespace Ghost.Ufbx;

public unsafe struct Panic
{
    private ufbx_panic* _ptr;

    internal Panic(ufbx_panic* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public Misaki.HighPerformance.Mathematics.float3x4 CatchGetSkinVertexMatrix(SkinDeformer skin, nuint vertex, Misaki.HighPerformance.Mathematics.float3x4* fallback)
    {
        return Api.ufbx_catch_get_skin_vertex_matrix(_ptr, skin.GetUnsafePtr(), vertex, fallback);
    }

    public uint CatchTriangulateFace(uint* indices, nuint numIndices, Mesh mesh, ufbx_face face)
    {
        return Api.ufbx_catch_triangulate_face(_ptr, indices, numIndices, mesh.GetUnsafePtr(), face);
    }

    public void CatchComputeTopology(Mesh mesh, TopoEdge topo, nuint numTopo)
    {
        Api.ufbx_catch_compute_topology(_ptr, mesh.GetUnsafePtr(), topo.GetUnsafePtr(), numTopo);
    }

    public uint CatchTopoNextVertexEdge(TopoEdge topo, nuint numTopo, uint index)
    {
        return Api.ufbx_catch_topo_next_vertex_edge(_ptr, topo.GetUnsafePtr(), numTopo, index);
    }

    public uint CatchTopoPrevVertexEdge(TopoEdge topo, nuint numTopo, uint index)
    {
        return Api.ufbx_catch_topo_prev_vertex_edge(_ptr, topo.GetUnsafePtr(), numTopo, index);
    }

    public Misaki.HighPerformance.Mathematics.float3 CatchGetWeightedFaceNormal(VertexVec3 positions, ufbx_face face)
    {
        return Api.ufbx_catch_get_weighted_face_normal(_ptr, positions.GetUnsafePtr(), face);
    }

    public nuint CatchGenerateNormalMapping(Mesh mesh, TopoEdge topo, nuint numTopo, uint* normalIndices, nuint numNormalIndices, bool assumeSmooth)
    {
        return Api.ufbx_catch_generate_normal_mapping(_ptr, mesh.GetUnsafePtr(), topo.GetUnsafePtr(), numTopo, normalIndices, numNormalIndices, assumeSmooth);
    }

    public void CatchComputeNormals(Mesh mesh, VertexVec3 positions, uint* normalIndices, nuint numNormalIndices, Misaki.HighPerformance.Mathematics.float3* normals, nuint numNormals)
    {
        Api.ufbx_catch_compute_normals(_ptr, mesh.GetUnsafePtr(), positions.GetUnsafePtr(), normalIndices, numNormalIndices, normals, numNormals);
    }

    public float CatchGetVertexReal(VertexReal v, nuint index)
    {
        return Api.ufbx_catch_get_vertex_real(_ptr, v.GetUnsafePtr(), index);
    }

    public Misaki.HighPerformance.Mathematics.float2 CatchGetVertexVec2(VertexVec2 v, nuint index)
    {
        return Api.ufbx_catch_get_vertex_vec2(_ptr, v.GetUnsafePtr(), index);
    }

    public Misaki.HighPerformance.Mathematics.float3 CatchGetVertexVec3(VertexVec3 v, nuint index)
    {
        return Api.ufbx_catch_get_vertex_vec3(_ptr, v.GetUnsafePtr(), index);
    }

    public Misaki.HighPerformance.Mathematics.float4 CatchGetVertexVec4(VertexVec4 v, nuint index)
    {
        return Api.ufbx_catch_get_vertex_vec4(_ptr, v.GetUnsafePtr(), index);
    }

    public float CatchGetVertexWVec3(VertexVec3 v, nuint index)
    {
        return Api.ufbx_catch_get_vertex_w_vec3(_ptr, v.GetUnsafePtr(), index);
    }

    public bool DidPanic => _ptr->did_panic;

    public nuint MessageLength => _ptr->message_length;

    internal ufbx_panic* GetUnsafePtr() => _ptr;
}
