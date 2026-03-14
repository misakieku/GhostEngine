namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_anim_value_list
    {
        public ufbx_anim_value** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
