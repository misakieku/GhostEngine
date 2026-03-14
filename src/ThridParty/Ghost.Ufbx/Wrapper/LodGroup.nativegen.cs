namespace Ghost.Ufbx;

public unsafe struct LodGroup
{
    private ufbx_lod_group* _ptr;

    internal LodGroup(ufbx_lod_group* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public bool RelativeDistances => _ptr->relative_distances;

    public ReadOnlySpan<ufbx_lod_level> LodLevels => _ptr->lod_levels.data == null ? ReadOnlySpan<ufbx_lod_level>.Empty : new ReadOnlySpan<ufbx_lod_level>(_ptr->lod_levels.data, checked((int)_ptr->lod_levels.count));

    public bool IgnoreParentTransform => _ptr->ignore_parent_transform;

    public bool UseDistanceLimit => _ptr->use_distance_limit;

    public float DistanceLimitMin => _ptr->distance_limit_min;

    public float DistanceLimitMax => _ptr->distance_limit_max;

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    public NodeList Instances => new(_ptr->instances.data, _ptr->instances.count);

    internal ufbx_lod_group* GetUnsafePtr() => _ptr;
}
