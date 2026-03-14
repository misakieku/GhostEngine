namespace Ghost.Ufbx;

public unsafe struct DisplayLayer
{
    private ufbx_display_layer* _ptr;

    internal DisplayLayer(ufbx_display_layer* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public NodeList Nodes => new(_ptr->nodes.data, _ptr->nodes.count);

    public bool Visible => _ptr->visible;

    public bool Frozen => _ptr->frozen;

    public Misaki.HighPerformance.Mathematics.float3 UiColor => _ptr->ui_color;

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_display_layer* GetUnsafePtr() => _ptr;
}
