namespace Ghost.Ufbx;

public unsafe struct CacheChannel
{
    private ufbx_cache_channel* _ptr;

    internal CacheChannel(ufbx_cache_channel* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public nuint SampleGeometryCacheReal(double time, float* data, nuint numData, GeometryCacheDataOpts opts)
    {
        return Api.ufbx_sample_geometry_cache_real(_ptr, time, data, numData, opts.GetUnsafePtr());
    }

    public nuint SampleGeometryCacheVec3(double time, Misaki.HighPerformance.Mathematics.float3* data, nuint numData, GeometryCacheDataOpts opts)
    {
        return Api.ufbx_sample_geometry_cache_vec3(_ptr, time, data, numData, opts.GetUnsafePtr());
    }

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public ufbx_cache_interpretation Interpretation => _ptr->interpretation;

    public ReadOnlySpan<byte> InterpretationNameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->interpretation_name);
    public string InterpretationName => NativeWrapperHelpers.GetString(_ptr->interpretation_name);

    public ReadOnlySpan<ufbx_cache_frame> Frames => _ptr->frames.data == null ? ReadOnlySpan<ufbx_cache_frame>.Empty : new ReadOnlySpan<ufbx_cache_frame>(_ptr->frames.data, checked((int)_ptr->frames.count));

    public ufbx_mirror_axis MirrorAxis => _ptr->mirror_axis;

    public float ScaleFactor => _ptr->scale_factor;

    internal ufbx_cache_channel* GetUnsafePtr() => _ptr;
}
