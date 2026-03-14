namespace Ghost.Ufbx;

public unsafe struct InflateInput
{
    private ufbx_inflate_input* _ptr;

    internal InflateInput(ufbx_inflate_input* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public nuint TotalSize => _ptr->total_size;

    public void* Data => _ptr->data;

    public nuint DataSize => _ptr->data_size;

    public void* Buffer => _ptr->buffer;

    public nuint BufferSize => _ptr->buffer_size;

    public void* ReadUser => _ptr->read_user;

    public ProgressCb ProgressCb => new((ufbx_progress_cb*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->progress_cb));

    public ulong ProgressIntervalHint => _ptr->progress_interval_hint;

    public ulong ProgressSizeBefore => _ptr->progress_size_before;

    public ulong ProgressSizeAfter => _ptr->progress_size_after;

    public bool NoHeader => _ptr->no_header;

    public bool NoChecksum => _ptr->no_checksum;

    public nuint InternalFastBits => _ptr->internal_fast_bits;

    internal ufbx_inflate_input* GetUnsafePtr() => _ptr;
}
