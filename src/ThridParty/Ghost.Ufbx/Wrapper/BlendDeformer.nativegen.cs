namespace Ghost.Ufbx;

public unsafe struct BlendDeformer
{
    private ufbx_blend_deformer* _ptr;

    internal BlendDeformer(ufbx_blend_deformer* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public Misaki.HighPerformance.Mathematics.float3 GetBlendVertexOffset(nuint vertex)
    {
        return Api.ufbx_get_blend_vertex_offset(_ptr, vertex);
    }

    public void AddBlendVertexOffsets(Misaki.HighPerformance.Mathematics.float3* vertices, nuint numVertices, float weight)
    {
        Api.ufbx_add_blend_vertex_offsets(_ptr, vertices, numVertices, weight);
    }

    public BlendChannelList Channels => new(_ptr->channels.data, _ptr->channels.count);

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_blend_deformer* GetUnsafePtr() => _ptr;
}
