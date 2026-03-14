namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_skin_cluster_list
    {
        public ufbx_skin_cluster** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
