namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_bone_pose
    {
        public ufbx_node* bone_node;

        [NativeTypeName("ufbx_matrix")]
        public Misaki.HighPerformance.Mathematics.float3x4 bone_to_world;

        [NativeTypeName("ufbx_matrix")]
        public Misaki.HighPerformance.Mathematics.float3x4 bone_to_parent;
    }
}
