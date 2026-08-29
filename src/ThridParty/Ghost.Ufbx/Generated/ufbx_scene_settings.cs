namespace Ghost.Ufbx
{
    /// <include file='ufbx_scene_settings.xml' path='doc/member[@name="ufbx_scene_settings"]/*' />
    public partial struct ufbx_scene_settings
    {
        /// <include file='ufbx_scene_settings.xml' path='doc/member[@name="ufbx_scene_settings.props"]/*' />
        public ufbx_props props;

        /// <include file='ufbx_scene_settings.xml' path='doc/member[@name="ufbx_scene_settings.axes"]/*' />
        public ufbx_coordinate_axes axes;

        /// <include file='ufbx_scene_settings.xml' path='doc/member[@name="ufbx_scene_settings.unit_meters"]/*' />
        [NativeTypeName("ufbx_real")]
        public float unit_meters;

        /// <include file='ufbx_scene_settings.xml' path='doc/member[@name="ufbx_scene_settings.frames_per_second"]/*' />
        public double frames_per_second;

        /// <include file='ufbx_scene_settings.xml' path='doc/member[@name="ufbx_scene_settings.ambient_color"]/*' />
        public ufbx_vec3 ambient_color;

        /// <include file='ufbx_scene_settings.xml' path='doc/member[@name="ufbx_scene_settings.default_camera"]/*' />
        public ufbx_string default_camera;

        /// <include file='ufbx_scene_settings.xml' path='doc/member[@name="ufbx_scene_settings.time_mode"]/*' />
        public ufbx_time_mode time_mode;

        /// <include file='ufbx_scene_settings.xml' path='doc/member[@name="ufbx_scene_settings.time_protocol"]/*' />
        public ufbx_time_protocol time_protocol;

        /// <include file='ufbx_scene_settings.xml' path='doc/member[@name="ufbx_scene_settings.snap_mode"]/*' />
        public ufbx_snap_mode snap_mode;

        /// <include file='ufbx_scene_settings.xml' path='doc/member[@name="ufbx_scene_settings.original_axis_up"]/*' />
        public ufbx_coordinate_axis original_axis_up;

        /// <include file='ufbx_scene_settings.xml' path='doc/member[@name="ufbx_scene_settings.original_unit_meters"]/*' />
        [NativeTypeName("ufbx_real")]
        public float original_unit_meters;
    }
}
