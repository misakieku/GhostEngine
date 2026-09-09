namespace Ghost.Ufbx
{
    /// <include file='ufbx_bone_pose.xml' path='doc/member[@name="ufbx_bone_pose"]/*' />
    public unsafe partial struct ufbx_bone_pose
    {
        /// <include file='ufbx_bone_pose.xml' path='doc/member[@name="ufbx_bone_pose.bone_node"]/*' />
        public ufbx_node* bone_node;

        /// <include file='ufbx_bone_pose.xml' path='doc/member[@name="ufbx_bone_pose.bone_to_world"]/*' />
        public ufbx_matrix bone_to_world;

        /// <include file='ufbx_bone_pose.xml' path='doc/member[@name="ufbx_bone_pose.bone_to_parent"]/*' />
        public ufbx_matrix bone_to_parent;
    }
}
