namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_blend_deformer_list
    {
        public ufbx_blend_deformer** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
