namespace Ghost.Ufbx
{
    /// <include file='ufbx_pose_list.xml' path='doc/member[@name="ufbx_pose_list"]/*' />
    public unsafe partial struct ufbx_pose_list
    {
        /// <include file='ufbx_pose_list.xml' path='doc/member[@name="ufbx_pose_list.data"]/*' />
        public ufbx_pose** data;

        /// <include file='ufbx_pose_list.xml' path='doc/member[@name="ufbx_pose_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
