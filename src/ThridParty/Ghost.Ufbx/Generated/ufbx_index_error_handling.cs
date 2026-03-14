namespace Ghost.Ufbx
{
    public enum ufbx_index_error_handling
    {
        UFBX_INDEX_ERROR_HANDLING_CLAMP,
        UFBX_INDEX_ERROR_HANDLING_NO_INDEX,
        UFBX_INDEX_ERROR_HANDLING_ABORT_LOADING,
        UFBX_INDEX_ERROR_HANDLING_UNSAFE_IGNORE,
        UFBX_INDEX_ERROR_HANDLING_FORCE_32BIT = 0x7fffffff,
    }
}
