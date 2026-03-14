namespace Ghost.Ufbx;

public unsafe struct BlendChannel
{
    private ufbx_blend_channel* _ptr;

    internal BlendChannel(ufbx_blend_channel* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public float Weight => _ptr->weight;

    public ReadOnlySpan<ufbx_blend_keyframe> Keyframes => _ptr->keyframes.data == null ? ReadOnlySpan<ufbx_blend_keyframe>.Empty : new ReadOnlySpan<ufbx_blend_keyframe>(_ptr->keyframes.data, checked((int)_ptr->keyframes.count));

    public bool HasTargetShape => _ptr->target_shape != null;
    public BlendShape TargetShape => _ptr->target_shape != null ? new(_ptr->target_shape) : throw new InvalidOperationException("TargetShape is null.");

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_blend_channel* GetUnsafePtr() => _ptr;
}
