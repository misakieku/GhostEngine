namespace Ghost.Ufbx;

public unsafe struct GeometryCache
{
    private ufbx_geometry_cache* _ptr;

    internal GeometryCache(ufbx_geometry_cache* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public static GeometryCache LoadGeometryCache(sbyte* filename, GeometryCacheOpts opts, Error error)
    {
        return new(Api.ufbx_load_geometry_cache(filename, opts.GetUnsafePtr(), error.GetUnsafePtr()));
    }

    public static GeometryCache LoadGeometryCacheLen(sbyte* filename, nuint filenameLen, GeometryCacheOpts opts, Error error)
    {
        return new(Api.ufbx_load_geometry_cache_len(filename, filenameLen, opts.GetUnsafePtr(), error.GetUnsafePtr()));
    }

    public void FreeGeometryCache()
    {
        Api.ufbx_free_geometry_cache(_ptr);
    }

    public void RetainGeometryCache()
    {
        Api.ufbx_retain_geometry_cache(_ptr);
    }

    public ReadOnlySpan<byte> RootFilenameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->root_filename);
    public string RootFilename => NativeWrapperHelpers.GetString(_ptr->root_filename);

    public ReadOnlySpan<ufbx_cache_channel> Channels => _ptr->channels.data == null ? ReadOnlySpan<ufbx_cache_channel>.Empty : new ReadOnlySpan<ufbx_cache_channel>(_ptr->channels.data, checked((int)_ptr->channels.count));

    public ReadOnlySpan<ufbx_cache_frame> Frames => _ptr->frames.data == null ? ReadOnlySpan<ufbx_cache_frame>.Empty : new ReadOnlySpan<ufbx_cache_frame>(_ptr->frames.data, checked((int)_ptr->frames.count));

    public ReadOnlySpan<string> ExtraInfo => _ptr->extra_info.data == null ? ReadOnlySpan<string>.Empty : new ReadOnlySpan<string>(_ptr->extra_info.data, checked((int)_ptr->extra_info.count));

    internal ufbx_geometry_cache* GetUnsafePtr() => _ptr;
}
