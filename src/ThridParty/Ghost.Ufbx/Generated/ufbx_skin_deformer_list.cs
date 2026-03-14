namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_skin_deformer_list
    {
        public ufbx_skin_deformer** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
