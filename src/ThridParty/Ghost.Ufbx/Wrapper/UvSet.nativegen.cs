namespace Ghost.Ufbx;

public unsafe struct UvSet
{
    private ufbx_uv_set* _ptr;

    internal UvSet(ufbx_uv_set* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public uint Index => _ptr->index;

    public VertexVec2 VertexUv => new((ufbx_vertex_vec2*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->vertex_uv));

    public VertexVec3 VertexTangent => new((ufbx_vertex_vec3*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->vertex_tangent));

    public VertexVec3 VertexBitangent => new((ufbx_vertex_vec3*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->vertex_bitangent));

    internal ufbx_uv_set* GetUnsafePtr() => _ptr;
}
