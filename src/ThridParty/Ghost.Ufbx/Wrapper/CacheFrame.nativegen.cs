namespace Ghost.Ufbx;

public unsafe struct CacheFrame
{
    private ufbx_cache_frame* _ptr;

    internal CacheFrame(ufbx_cache_frame* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public nuint ReadGeometryCacheReal(float* data, nuint numData, GeometryCacheDataOpts opts)
    {
        return Api.ufbx_read_geometry_cache_real(_ptr, data, numData, opts.GetUnsafePtr());
    }

    public nuint ReadGeometryCacheVec3(Misaki.HighPerformance.Mathematics.float3* data, nuint numData, GeometryCacheDataOpts opts)
    {
        return Api.ufbx_read_geometry_cache_vec3(_ptr, data, numData, opts.GetUnsafePtr());
    }

    public ReadOnlySpan<byte> ChannelBytes => NativeWrapperHelpers.AsByteSpan(_ptr->channel);
    public string Channel => NativeWrapperHelpers.GetString(_ptr->channel);

    public double Time => _ptr->time;

    public ReadOnlySpan<byte> FilenameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->filename);
    public string Filename => NativeWrapperHelpers.GetString(_ptr->filename);

    public ufbx_cache_file_format FileFormat => _ptr->file_format;

    public ufbx_mirror_axis MirrorAxis => _ptr->mirror_axis;

    public float ScaleFactor => _ptr->scale_factor;

    public ufbx_cache_data_format DataFormat => _ptr->data_format;

    public ufbx_cache_data_encoding DataEncoding => _ptr->data_encoding;

    public ulong DataOffset => _ptr->data_offset;

    public uint DataCount => _ptr->data_count;

    public uint DataElementBytes => _ptr->data_element_bytes;

    public ulong DataTotalBytes => _ptr->data_total_bytes;

    internal ufbx_cache_frame* GetUnsafePtr() => _ptr;
}
