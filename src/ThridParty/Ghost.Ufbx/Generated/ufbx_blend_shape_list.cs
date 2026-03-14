namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_blend_shape_list
    {
        public ufbx_blend_shape** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
