namespace Ghost.Ufbx
{
    /// <include file='ufbx_props.xml' path='doc/member[@name="ufbx_props"]/*' />
    public unsafe partial struct ufbx_props
    {
        /// <include file='ufbx_props.xml' path='doc/member[@name="ufbx_props.props"]/*' />
        public ufbx_prop_list props;

        /// <include file='ufbx_props.xml' path='doc/member[@name="ufbx_props.num_animated"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_animated;

        /// <include file='ufbx_props.xml' path='doc/member[@name="ufbx_props.defaults"]/*' />
        public ufbx_props* defaults;
    }
}
