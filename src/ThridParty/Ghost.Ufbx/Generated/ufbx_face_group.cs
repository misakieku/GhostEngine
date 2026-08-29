namespace Ghost.Ufbx
{
    /// <include file='ufbx_face_group.xml' path='doc/member[@name="ufbx_face_group"]/*' />
    public partial struct ufbx_face_group
    {
        /// <include file='ufbx_face_group.xml' path='doc/member[@name="ufbx_face_group.id"]/*' />
        [NativeTypeName("int32_t")]
        public int id;

        /// <include file='ufbx_face_group.xml' path='doc/member[@name="ufbx_face_group.name"]/*' />
        public ufbx_string name;
    }
}
