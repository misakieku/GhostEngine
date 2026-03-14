namespace Ghost.Ufbx;

public unsafe struct BakedQuat
{
    private ufbx_baked_quat* _ptr;

    internal BakedQuat(ufbx_baked_quat* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public double Time => _ptr->time;

    public Misaki.HighPerformance.Mathematics.quaternion Value => _ptr->value;

    public ufbx_baked_key_flags Flags => _ptr->flags;

    internal ufbx_baked_quat* GetUnsafePtr() => _ptr;
}
