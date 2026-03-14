namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_anim_stack_list
    {
        public ufbx_anim_stack** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
