namespace Ghost.Ufbx
{
    /// <include file='ufbx_progress.xml' path='doc/member[@name="ufbx_progress"]/*' />
    public partial struct ufbx_progress
    {
        /// <include file='ufbx_progress.xml' path='doc/member[@name="ufbx_progress.bytes_read"]/*' />
        [NativeTypeName("uint64_t")]
        public ulong bytes_read;

        /// <include file='ufbx_progress.xml' path='doc/member[@name="ufbx_progress.bytes_total"]/*' />
        [NativeTypeName("uint64_t")]
        public ulong bytes_total;
    }
}
