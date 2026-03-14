namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_pose_list
    {
        public ufbx_pose** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
