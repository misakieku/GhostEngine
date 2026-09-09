namespace Ghost.Ufbx
{
    /// <include file='ufbx_error_frame.xml' path='doc/member[@name="ufbx_error_frame"]/*' />
    public partial struct ufbx_error_frame
    {
        /// <include file='ufbx_error_frame.xml' path='doc/member[@name="ufbx_error_frame.source_line"]/*' />
        [NativeTypeName("uint32_t")]
        public uint source_line;

        /// <include file='ufbx_error_frame.xml' path='doc/member[@name="ufbx_error_frame.function"]/*' />
        public ufbx_string function;

        /// <include file='ufbx_error_frame.xml' path='doc/member[@name="ufbx_error_frame.description"]/*' />
        public ufbx_string description;
    }
}
