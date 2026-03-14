namespace Ghost.Ufbx;

public unsafe struct Stream
{
    private ufbx_stream* _ptr;

    internal Stream(ufbx_stream* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public Scene LoadStream(LoadOpts opts, Error error)
    {
        return new(Api.ufbx_load_stream(_ptr, opts.GetUnsafePtr(), error.GetUnsafePtr()));
    }

    public Scene LoadStreamPrefix(void* prefix, nuint prefixSize, LoadOpts opts, Error error)
    {
        return new(Api.ufbx_load_stream_prefix(_ptr, prefix, prefixSize, opts.GetUnsafePtr(), error.GetUnsafePtr()));
    }

    public bool OpenFile(sbyte* path, nuint pathLen, OpenFileOpts opts, Error error)
    {
        return Api.ufbx_open_file(_ptr, path, pathLen, opts.GetUnsafePtr(), error.GetUnsafePtr());
    }

    public bool OpenFileCtx(nuint ctx, sbyte* path, nuint pathLen, OpenFileOpts opts, Error error)
    {
        return Api.ufbx_open_file_ctx(_ptr, ctx, path, pathLen, opts.GetUnsafePtr(), error.GetUnsafePtr());
    }

    public bool OpenMemory(void* data, nuint dataSize, OpenMemoryOpts opts, Error error)
    {
        return Api.ufbx_open_memory(_ptr, data, dataSize, opts.GetUnsafePtr(), error.GetUnsafePtr());
    }

    public bool OpenMemoryCtx(nuint ctx, void* data, nuint dataSize, OpenMemoryOpts opts, Error error)
    {
        return Api.ufbx_open_memory_ctx(_ptr, ctx, data, dataSize, opts.GetUnsafePtr(), error.GetUnsafePtr());
    }

    public void* User => _ptr->user;

    internal ufbx_stream* GetUnsafePtr() => _ptr;
}
