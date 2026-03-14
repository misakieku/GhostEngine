namespace Ghost.Ufbx;

public unsafe struct BakedVec3
{
    private ufbx_baked_vec3* _ptr;

    internal BakedVec3(ufbx_baked_vec3* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public double Time => _ptr->time;

    public Misaki.HighPerformance.Mathematics.float3 Value => _ptr->value;

    public ufbx_baked_key_flags Flags => _ptr->flags;

    internal ufbx_baked_vec3* GetUnsafePtr() => _ptr;
}
