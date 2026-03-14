namespace Ghost.Ufbx;

public unsafe struct CacheDeformer
{
    private ufbx_cache_deformer* _ptr;

    internal CacheDeformer(ufbx_cache_deformer* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ReadOnlySpan<byte> ChannelBytes => NativeWrapperHelpers.AsByteSpan(_ptr->channel);
    public string Channel => NativeWrapperHelpers.GetString(_ptr->channel);

    public bool HasFile => _ptr->file != null;
    public CacheFile File => _ptr->file != null ? new(_ptr->file) : throw new InvalidOperationException("File is null.");

    public bool HasExternalCache => _ptr->external_cache != null;
    public GeometryCache ExternalCache => _ptr->external_cache != null ? new(_ptr->external_cache) : throw new InvalidOperationException("ExternalCache is null.");

    public bool HasExternalChannel => _ptr->external_channel != null;
    public CacheChannel ExternalChannel => _ptr->external_channel != null ? new(_ptr->external_channel) : throw new InvalidOperationException("ExternalChannel is null.");

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_cache_deformer* GetUnsafePtr() => _ptr;
}
