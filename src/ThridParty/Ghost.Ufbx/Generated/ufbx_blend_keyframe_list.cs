namespace Ghost.Ufbx
{
    /// <include file='ufbx_blend_keyframe_list.xml' path='doc/member[@name="ufbx_blend_keyframe_list"]/*' />
    public unsafe partial struct ufbx_blend_keyframe_list
    {
        /// <include file='ufbx_blend_keyframe_list.xml' path='doc/member[@name="ufbx_blend_keyframe_list.data"]/*' />
        public ufbx_blend_keyframe* data;

        /// <include file='ufbx_blend_keyframe_list.xml' path='doc/member[@name="ufbx_blend_keyframe_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
