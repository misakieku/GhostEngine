namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_light_list
    {
        public ufbx_light** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
