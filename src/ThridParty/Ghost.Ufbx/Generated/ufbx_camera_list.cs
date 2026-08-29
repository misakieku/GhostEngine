namespace Ghost.Ufbx
{
    /// <include file='ufbx_camera_list.xml' path='doc/member[@name="ufbx_camera_list"]/*' />
    public unsafe partial struct ufbx_camera_list
    {
        /// <include file='ufbx_camera_list.xml' path='doc/member[@name="ufbx_camera_list.data"]/*' />
        public ufbx_camera** data;

        /// <include file='ufbx_camera_list.xml' path='doc/member[@name="ufbx_camera_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
