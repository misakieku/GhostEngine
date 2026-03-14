namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_stereo_camera_list
    {
        public ufbx_stereo_camera** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
