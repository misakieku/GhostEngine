namespace Ghost.Ufbx;

public unsafe struct BlendShape
{
    private ufbx_blend_shape* _ptr;

    internal BlendShape(ufbx_blend_shape* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint GetBlendShapeOffsetIndex(nuint vertex)
    {
        return Api.ufbx_get_blend_shape_offset_index(_ptr, vertex);
    }

    public Misaki.HighPerformance.Mathematics.float3 GetBlendShapeVertexOffset(nuint vertex)
    {
        return Api.ufbx_get_blend_shape_vertex_offset(_ptr, vertex);
    }

    public void AddBlendShapeVertexOffsets(Misaki.HighPerformance.Mathematics.float3* vertices, nuint numVertices, float weight)
    {
        Api.ufbx_add_blend_shape_vertex_offsets(_ptr, vertices, numVertices, weight);
    }

    public nuint NumOffsets => _ptr->num_offsets;

    public ReadOnlySpan<uint> OffsetVertices => _ptr->offset_vertices.data == null ? ReadOnlySpan<uint>.Empty : new ReadOnlySpan<uint>(_ptr->offset_vertices.data, checked((int)_ptr->offset_vertices.count));

    public ReadOnlySpan<Misaki.HighPerformance.Mathematics.float3> PositionOffsets => _ptr->position_offsets.data == null ? ReadOnlySpan<Misaki.HighPerformance.Mathematics.float3>.Empty : new ReadOnlySpan<Misaki.HighPerformance.Mathematics.float3>(_ptr->position_offsets.data, checked((int)_ptr->position_offsets.count));

    public ReadOnlySpan<Misaki.HighPerformance.Mathematics.float3> NormalOffsets => _ptr->normal_offsets.data == null ? ReadOnlySpan<Misaki.HighPerformance.Mathematics.float3>.Empty : new ReadOnlySpan<Misaki.HighPerformance.Mathematics.float3>(_ptr->normal_offsets.data, checked((int)_ptr->normal_offsets.count));

    public ReadOnlySpan<float> OffsetWeights => _ptr->offset_weights.data == null ? ReadOnlySpan<float>.Empty : new ReadOnlySpan<float>(_ptr->offset_weights.data, checked((int)_ptr->offset_weights.count));

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_blend_shape* GetUnsafePtr() => _ptr;
}
