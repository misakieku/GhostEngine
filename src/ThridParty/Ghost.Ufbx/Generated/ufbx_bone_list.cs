namespace Ghost.Ufbx
{
    /// <include file='ufbx_bone_list.xml' path='doc/member[@name="ufbx_bone_list"]/*' />
    public unsafe partial struct ufbx_bone_list
    {
        /// <include file='ufbx_bone_list.xml' path='doc/member[@name="ufbx_bone_list.data"]/*' />
        public ufbx_bone** data;

        /// <include file='ufbx_bone_list.xml' path='doc/member[@name="ufbx_bone_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
