namespace Ghost.Ufbx;

public unsafe struct FaceGroup
{
    private ufbx_face_group* _ptr;

    internal FaceGroup(ufbx_face_group* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public int Id => _ptr->id;

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    internal ufbx_face_group* GetUnsafePtr() => _ptr;
}
