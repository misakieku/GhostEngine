namespace Ghost.Ufbx;

public unsafe struct Application
{
    private ufbx_application* _ptr;

    internal Application(ufbx_application* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ReadOnlySpan<byte> VendorBytes => NativeWrapperHelpers.AsByteSpan(_ptr->vendor);
    public string Vendor => NativeWrapperHelpers.GetString(_ptr->vendor);

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public ReadOnlySpan<byte> VersionBytes => NativeWrapperHelpers.AsByteSpan(_ptr->version);
    public string Version => NativeWrapperHelpers.GetString(_ptr->version);

    internal ufbx_application* GetUnsafePtr() => _ptr;
}
