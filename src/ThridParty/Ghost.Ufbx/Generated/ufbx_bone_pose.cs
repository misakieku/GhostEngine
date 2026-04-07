namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_bone_pose
    {
        public ufbx_node* bone_node;

        public ufbx_matrix bone_to_world;

        public ufbx_matrix bone_to_parent;
    }
}
