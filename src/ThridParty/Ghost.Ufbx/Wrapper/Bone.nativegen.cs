namespace Ghost.Ufbx;

public unsafe struct Bone
{
    private ufbx_bone* _ptr;

    internal Bone(ufbx_bone* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public float Radius => _ptr->radius;

    public float RelativeLength => _ptr->relative_length;

    public bool IsRoot => _ptr->is_root;

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    public NodeList Instances => new(_ptr->instances.data, _ptr->instances.count);

    internal ufbx_bone* GetUnsafePtr() => _ptr;
}
