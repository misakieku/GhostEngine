namespace Ghost.Ufbx;

public unsafe struct StereoCamera
{
    private ufbx_stereo_camera* _ptr;

    internal StereoCamera(ufbx_stereo_camera* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public bool HasLeft => _ptr->left != null;
    public Camera Left => _ptr->left != null ? new(_ptr->left) : throw new InvalidOperationException("Left is null.");

    public bool HasRight => _ptr->right != null;
    public Camera Right => _ptr->right != null ? new(_ptr->right) : throw new InvalidOperationException("Right is null.");

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    public NodeList Instances => new(_ptr->instances.data, _ptr->instances.count);

    internal ufbx_stereo_camera* GetUnsafePtr() => _ptr;
}
