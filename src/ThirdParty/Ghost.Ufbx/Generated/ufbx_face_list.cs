namespace Ghost.Ufbx
{
    /// <include file='ufbx_face_list.xml' path='doc/member[@name="ufbx_face_list"]/*' />
    public unsafe partial struct ufbx_face_list
    {
        /// <include file='ufbx_face_list.xml' path='doc/member[@name="ufbx_face_list.data"]/*' />
        public ufbx_face* data;

        /// <include file='ufbx_face_list.xml' path='doc/member[@name="ufbx_face_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
