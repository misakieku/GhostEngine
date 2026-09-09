namespace Ghost.Ufbx
{
    /// <include file='ufbx_texture_file.xml' path='doc/member[@name="ufbx_texture_file"]/*' />
    public partial struct ufbx_texture_file
    {
        /// <include file='ufbx_texture_file.xml' path='doc/member[@name="ufbx_texture_file.index"]/*' />
        [NativeTypeName("uint32_t")]
        public uint index;

        /// <include file='ufbx_texture_file.xml' path='doc/member[@name="ufbx_texture_file.filename"]/*' />
        public ufbx_string filename;

        /// <include file='ufbx_texture_file.xml' path='doc/member[@name="ufbx_texture_file.absolute_filename"]/*' />
        public ufbx_string absolute_filename;

        /// <include file='ufbx_texture_file.xml' path='doc/member[@name="ufbx_texture_file.relative_filename"]/*' />
        public ufbx_string relative_filename;

        /// <include file='ufbx_texture_file.xml' path='doc/member[@name="ufbx_texture_file.raw_filename"]/*' />
        public ufbx_blob raw_filename;

        /// <include file='ufbx_texture_file.xml' path='doc/member[@name="ufbx_texture_file.raw_absolute_filename"]/*' />
        public ufbx_blob raw_absolute_filename;

        /// <include file='ufbx_texture_file.xml' path='doc/member[@name="ufbx_texture_file.raw_relative_filename"]/*' />
        public ufbx_blob raw_relative_filename;

        /// <include file='ufbx_texture_file.xml' path='doc/member[@name="ufbx_texture_file.content"]/*' />
        public ufbx_blob content;
    }
}
