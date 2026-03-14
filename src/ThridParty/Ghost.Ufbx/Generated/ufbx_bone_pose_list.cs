namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_bone_pose_list
    {
        public ufbx_bone_pose* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
