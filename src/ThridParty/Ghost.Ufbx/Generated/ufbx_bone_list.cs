namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_bone_list
    {
        public ufbx_bone** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
