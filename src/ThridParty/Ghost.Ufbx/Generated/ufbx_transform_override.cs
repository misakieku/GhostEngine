namespace Ghost.Ufbx
{
    /// <include file='ufbx_transform_override.xml' path='doc/member[@name="ufbx_transform_override"]/*' />
    public partial struct ufbx_transform_override
    {
        /// <include file='ufbx_transform_override.xml' path='doc/member[@name="ufbx_transform_override.node_id"]/*' />
        [NativeTypeName("uint32_t")]
        public uint node_id;

        /// <include file='ufbx_transform_override.xml' path='doc/member[@name="ufbx_transform_override.transform"]/*' />
        public ufbx_transform transform;
    }
}
