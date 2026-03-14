namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_constraint_list
    {
        public ufbx_constraint** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
