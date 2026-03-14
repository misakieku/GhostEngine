namespace Ghost.Ufbx;

public unsafe struct OpenFileOpts
{
    private ufbx_open_file_opts* _ptr;

    internal OpenFileOpts(ufbx_open_file_opts* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public AllocatorOpts Allocator => new((ufbx_allocator_opts*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->allocator));

    public bool FilenameNullTerminated => _ptr->filename_null_terminated;

    internal ufbx_open_file_opts* GetUnsafePtr() => _ptr;
}
