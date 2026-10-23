namespace Ghost.Ufbx
{
    /// <include file='ufbx_skin_cluster_list.xml' path='doc/member[@name="ufbx_skin_cluster_list"]/*' />
    public unsafe partial struct ufbx_skin_cluster_list
    {
        /// <include file='ufbx_skin_cluster_list.xml' path='doc/member[@name="ufbx_skin_cluster_list.data"]/*' />
        public ufbx_skin_cluster** data;

        /// <include file='ufbx_skin_cluster_list.xml' path='doc/member[@name="ufbx_skin_cluster_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
