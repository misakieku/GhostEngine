namespace Ghost.Ufbx
{
    /// <include file='ufbx_thumbnail.xml' path='doc/member[@name="ufbx_thumbnail"]/*' />
    public partial struct ufbx_thumbnail
    {
        /// <include file='ufbx_thumbnail.xml' path='doc/member[@name="ufbx_thumbnail.props"]/*' />
        public ufbx_props props;

        /// <include file='ufbx_thumbnail.xml' path='doc/member[@name="ufbx_thumbnail.width"]/*' />
        [NativeTypeName("uint32_t")]
        public uint width;

        /// <include file='ufbx_thumbnail.xml' path='doc/member[@name="ufbx_thumbnail.height"]/*' />
        [NativeTypeName("uint32_t")]
        public uint height;

        /// <include file='ufbx_thumbnail.xml' path='doc/member[@name="ufbx_thumbnail.format"]/*' />
        public ufbx_thumbnail_format format;

        /// <include file='ufbx_thumbnail.xml' path='doc/member[@name="ufbx_thumbnail.data"]/*' />
        public ufbx_blob data;
    }
}
