namespace Ghost.Ufbx
{
    /// <include file='ufbx_face_group_list.xml' path='doc/member[@name="ufbx_face_group_list"]/*' />
    public unsafe partial struct ufbx_face_group_list
    {
        /// <include file='ufbx_face_group_list.xml' path='doc/member[@name="ufbx_face_group_list.data"]/*' />
        public ufbx_face_group* data;

        /// <include file='ufbx_face_group_list.xml' path='doc/member[@name="ufbx_face_group_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
