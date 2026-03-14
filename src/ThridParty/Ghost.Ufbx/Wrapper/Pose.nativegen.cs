namespace Ghost.Ufbx;

public unsafe struct Pose
{
    private ufbx_pose* _ptr;

    internal Pose(ufbx_pose* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public BonePose GetBonePose(Node node)
    {
        return new(Api.ufbx_get_bone_pose(_ptr, node.GetUnsafePtr()));
    }

    public bool IsBindPose => _ptr->is_bind_pose;

    public ReadOnlySpan<ufbx_bone_pose> BonePoses => _ptr->bone_poses.data == null ? ReadOnlySpan<ufbx_bone_pose>.Empty : new ReadOnlySpan<ufbx_bone_pose>(_ptr->bone_poses.data, checked((int)_ptr->bone_poses.count));

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_pose* GetUnsafePtr() => _ptr;
}
