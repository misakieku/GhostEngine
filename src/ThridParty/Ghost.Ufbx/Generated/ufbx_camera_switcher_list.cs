namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_camera_switcher_list
    {
        public ufbx_camera_switcher** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
