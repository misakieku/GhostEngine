namespace Ghost.Ufbx;

public unsafe struct AnimLayer
{
    private ufbx_anim_layer* _ptr;

    internal AnimLayer(ufbx_anim_layer* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public AnimProp FindAnimPropLen(Element element, sbyte* prop, nuint propLen)
    {
        return new(Api.ufbx_find_anim_prop_len(_ptr, element.GetUnsafePtr(), prop, propLen));
    }

    public AnimProp FindAnimProp(Element element, sbyte* prop)
    {
        return new(Api.ufbx_find_anim_prop(_ptr, element.GetUnsafePtr(), prop));
    }

    public ufbx_anim_prop_list FindAnimProps(Element element)
    {
        return Api.ufbx_find_anim_props(_ptr, element.GetUnsafePtr());
    }

    public float Weight => _ptr->weight;

    public bool WeightIsAnimated => _ptr->weight_is_animated;

    public bool Blended => _ptr->blended;

    public bool Additive => _ptr->additive;

    public bool ComposeRotation => _ptr->compose_rotation;

    public bool ComposeScale => _ptr->compose_scale;

    public AnimValueList AnimValues => new(_ptr->anim_values.data, _ptr->anim_values.count);

    public ReadOnlySpan<ufbx_anim_prop> AnimProps => _ptr->anim_props.data == null ? ReadOnlySpan<ufbx_anim_prop>.Empty : new ReadOnlySpan<ufbx_anim_prop>(_ptr->anim_props.data, checked((int)_ptr->anim_props.count));

    public bool HasAnim => _ptr->anim != null;
    public Anim Anim => _ptr->anim != null ? new(_ptr->anim) : throw new InvalidOperationException("Anim is null.");

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_anim_layer* GetUnsafePtr() => _ptr;
}
