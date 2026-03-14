namespace Ghost.Ufbx
{
    public partial struct ufbx_baked_anim_metadata
    {
        [NativeTypeName("size_t")]
        public nuint result_memory_used;

        [NativeTypeName("size_t")]
        public nuint temp_memory_used;

        [NativeTypeName("size_t")]
        public nuint result_allocs;

        [NativeTypeName("size_t")]
        public nuint temp_allocs;
    }
}
