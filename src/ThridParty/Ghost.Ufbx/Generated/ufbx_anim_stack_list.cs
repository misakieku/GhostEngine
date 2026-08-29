namespace Ghost.Ufbx
{
    /// <include file='ufbx_anim_stack_list.xml' path='doc/member[@name="ufbx_anim_stack_list"]/*' />
    public unsafe partial struct ufbx_anim_stack_list
    {
        /// <include file='ufbx_anim_stack_list.xml' path='doc/member[@name="ufbx_anim_stack_list.data"]/*' />
        public ufbx_anim_stack** data;

        /// <include file='ufbx_anim_stack_list.xml' path='doc/member[@name="ufbx_anim_stack_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
