namespace Ghost.Ufbx
{
    public partial struct ufbx_progress
    {
        [NativeTypeName("uint64_t")]
        public ulong bytes_read;

        [NativeTypeName("uint64_t")]
        public ulong bytes_total;
    }
}
