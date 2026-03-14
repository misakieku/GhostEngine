namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_camera_list
    {
        public ufbx_camera** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
