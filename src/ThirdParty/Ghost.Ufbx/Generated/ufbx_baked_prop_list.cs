namespace Ghost.Ufbx
{
    /// <include file='ufbx_baked_prop_list.xml' path='doc/member[@name="ufbx_baked_prop_list"]/*' />
    public unsafe partial struct ufbx_baked_prop_list
    {
        /// <include file='ufbx_baked_prop_list.xml' path='doc/member[@name="ufbx_baked_prop_list.data"]/*' />
        public ufbx_baked_prop* data;

        /// <include file='ufbx_baked_prop_list.xml' path='doc/member[@name="ufbx_baked_prop_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
