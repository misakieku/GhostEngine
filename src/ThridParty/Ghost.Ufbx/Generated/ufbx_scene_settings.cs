namespace Ghost.Ufbx
{
    public partial struct ufbx_scene_settings
    {
        public ufbx_props props;

        public ufbx_coordinate_axes axes;

        [NativeTypeName("ufbx_real")]
        public float unit_meters;

        public double frames_per_second;

        [NativeTypeName("ufbx_vec3")]
        public Misaki.HighPerformance.Mathematics.float3 ambient_color;

        public ufbx_string default_camera;

        public ufbx_time_mode time_mode;

        public ufbx_time_protocol time_protocol;

        public ufbx_snap_mode snap_mode;

        public ufbx_coordinate_axis original_axis_up;

        [NativeTypeName("ufbx_real")]
        public float original_unit_meters;
    }
}
