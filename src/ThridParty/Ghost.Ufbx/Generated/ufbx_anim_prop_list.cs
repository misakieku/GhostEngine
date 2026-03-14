namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_anim_prop_list
    {
        public ufbx_anim_prop* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
