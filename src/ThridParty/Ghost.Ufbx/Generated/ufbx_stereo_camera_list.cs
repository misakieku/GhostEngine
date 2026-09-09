namespace Ghost.Ufbx
{
    /// <include file='ufbx_stereo_camera_list.xml' path='doc/member[@name="ufbx_stereo_camera_list"]/*' />
    public unsafe partial struct ufbx_stereo_camera_list
    {
        /// <include file='ufbx_stereo_camera_list.xml' path='doc/member[@name="ufbx_stereo_camera_list.data"]/*' />
        public ufbx_stereo_camera** data;

        /// <include file='ufbx_stereo_camera_list.xml' path='doc/member[@name="ufbx_stereo_camera_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
