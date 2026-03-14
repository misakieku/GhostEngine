namespace Ghost.Ufbx;

public unsafe struct BakedAnimMetadata
{
    private ufbx_baked_anim_metadata* _ptr;

    internal BakedAnimMetadata(ufbx_baked_anim_metadata* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public nuint ResultMemoryUsed => _ptr->result_memory_used;

    public nuint TempMemoryUsed => _ptr->temp_memory_used;

    public nuint ResultAllocs => _ptr->result_allocs;

    public nuint TempAllocs => _ptr->temp_allocs;

    internal ufbx_baked_anim_metadata* GetUnsafePtr() => _ptr;
}
