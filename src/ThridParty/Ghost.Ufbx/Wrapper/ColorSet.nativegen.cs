namespace Ghost.Ufbx;

public unsafe struct ColorSet
{
    private ufbx_color_set* _ptr;

    internal ColorSet(ufbx_color_set* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public uint Index => _ptr->index;

    public VertexVec4 VertexColor => new((ufbx_vertex_vec4*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->vertex_color));

    internal ufbx_color_set* GetUnsafePtr() => _ptr;
}
