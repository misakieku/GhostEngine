namespace Ghost.Ufbx
{
    /// <include file='ufbx_light_list.xml' path='doc/member[@name="ufbx_light_list"]/*' />
    public unsafe partial struct ufbx_light_list
    {
        /// <include file='ufbx_light_list.xml' path='doc/member[@name="ufbx_light_list.data"]/*' />
        public ufbx_light** data;

        /// <include file='ufbx_light_list.xml' path='doc/member[@name="ufbx_light_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
