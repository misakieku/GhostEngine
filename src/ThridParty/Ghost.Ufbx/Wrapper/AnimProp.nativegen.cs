namespace Ghost.Ufbx;

public unsafe struct AnimProp
{
    private ufbx_anim_prop* _ptr;

    internal AnimProp(ufbx_anim_prop* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public bool HasElement => _ptr->element != null;
    public Element Element => _ptr->element != null ? new(_ptr->element) : throw new InvalidOperationException("Element is null.");

    public ReadOnlySpan<byte> PropNameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->prop_name);
    public string PropName => NativeWrapperHelpers.GetString(_ptr->prop_name);

    public bool HasAnimValue => _ptr->anim_value != null;
    public AnimValue AnimValue => _ptr->anim_value != null ? new(_ptr->anim_value) : throw new InvalidOperationException("AnimValue is null.");

    internal ufbx_anim_prop* GetUnsafePtr() => _ptr;
}
