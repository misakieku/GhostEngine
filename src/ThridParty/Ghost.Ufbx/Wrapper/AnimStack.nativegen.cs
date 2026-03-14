namespace Ghost.Ufbx;

public unsafe struct AnimStack
{
    private ufbx_anim_stack* _ptr;

    internal AnimStack(ufbx_anim_stack* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public double TimeBegin => _ptr->time_begin;

    public double TimeEnd => _ptr->time_end;

    public AnimLayerList Layers => new(_ptr->layers.data, _ptr->layers.count);

    public bool HasAnim => _ptr->anim != null;
    public Anim Anim => _ptr->anim != null ? new(_ptr->anim) : throw new InvalidOperationException("Anim is null.");

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_anim_stack* GetUnsafePtr() => _ptr;
}
