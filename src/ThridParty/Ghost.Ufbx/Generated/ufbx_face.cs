namespace Ghost.Ufbx
{
    /// <include file='ufbx_face.xml' path='doc/member[@name="ufbx_face"]/*' />
    public partial struct ufbx_face
    {
        /// <include file='ufbx_face.xml' path='doc/member[@name="ufbx_face.index_begin"]/*' />
        [NativeTypeName("uint32_t")]
        public uint index_begin;

        /// <include file='ufbx_face.xml' path='doc/member[@name="ufbx_face.num_indices"]/*' />
        [NativeTypeName("uint32_t")]
        public uint num_indices;
    }
}
