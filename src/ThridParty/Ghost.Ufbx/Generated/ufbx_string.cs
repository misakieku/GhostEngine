namespace Ghost.Ufbx
{
    /// <include file='ufbx_string.xml' path='doc/member[@name="ufbx_string"]/*' />
    public unsafe partial struct ufbx_string
    {
        /// <include file='ufbx_string.xml' path='doc/member[@name="ufbx_string.data"]/*' />
        [NativeTypeName("const char *")]
        public sbyte* data;

        /// <include file='ufbx_string.xml' path='doc/member[@name="ufbx_string.length"]/*' />
        [NativeTypeName("size_t")]
        public nuint length;
    }
}
