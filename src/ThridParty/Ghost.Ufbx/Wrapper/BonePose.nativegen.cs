namespace Ghost.Ufbx;

public unsafe struct BonePose
{
    private ufbx_bone_pose* _ptr;

    internal BonePose(ufbx_bone_pose* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public bool HasBoneNode => _ptr->bone_node != null;
    public Node BoneNode => _ptr->bone_node != null ? new(_ptr->bone_node) : throw new InvalidOperationException("BoneNode is null.");

    public Misaki.HighPerformance.Mathematics.float3x4 BoneToWorld => _ptr->bone_to_world;

    public Misaki.HighPerformance.Mathematics.float3x4 BoneToParent => _ptr->bone_to_parent;

    internal ufbx_bone_pose* GetUnsafePtr() => _ptr;
}
