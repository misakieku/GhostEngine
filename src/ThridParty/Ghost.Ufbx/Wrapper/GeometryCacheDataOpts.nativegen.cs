namespace Ghost.Ufbx;

public unsafe struct GeometryCacheDataOpts
{
    private ufbx_geometry_cache_data_opts* _ptr;

    internal GeometryCacheDataOpts(ufbx_geometry_cache_data_opts* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public OpenFileCb OpenFileCb => new((ufbx_open_file_cb*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->open_file_cb));

    public bool Additive => _ptr->additive;

    public bool UseWeight => _ptr->use_weight;

    public float Weight => _ptr->weight;

    public bool IgnoreTransform => _ptr->ignore_transform;

    internal ufbx_geometry_cache_data_opts* GetUnsafePtr() => _ptr;
}
