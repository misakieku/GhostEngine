namespace Ghost.Ufbx
{
    /// <include file='ufbx_skin_deformer_list.xml' path='doc/member[@name="ufbx_skin_deformer_list"]/*' />
    public unsafe partial struct ufbx_skin_deformer_list
    {
        /// <include file='ufbx_skin_deformer_list.xml' path='doc/member[@name="ufbx_skin_deformer_list.data"]/*' />
        public ufbx_skin_deformer** data;

        /// <include file='ufbx_skin_deformer_list.xml' path='doc/member[@name="ufbx_skin_deformer_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
