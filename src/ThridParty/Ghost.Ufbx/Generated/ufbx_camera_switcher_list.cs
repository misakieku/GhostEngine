namespace Ghost.Ufbx
{
    /// <include file='ufbx_camera_switcher_list.xml' path='doc/member[@name="ufbx_camera_switcher_list"]/*' />
    public unsafe partial struct ufbx_camera_switcher_list
    {
        /// <include file='ufbx_camera_switcher_list.xml' path='doc/member[@name="ufbx_camera_switcher_list.data"]/*' />
        public ufbx_camera_switcher** data;

        /// <include file='ufbx_camera_switcher_list.xml' path='doc/member[@name="ufbx_camera_switcher_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
