namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_constraint_target_list
    {
        public ufbx_constraint_target* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
