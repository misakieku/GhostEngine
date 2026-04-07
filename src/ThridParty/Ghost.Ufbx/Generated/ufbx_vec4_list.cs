namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_vec4_list
    {
        public ufbx_vec4* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
