namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_vec3_list
    {
        public ufbx_vec3* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
