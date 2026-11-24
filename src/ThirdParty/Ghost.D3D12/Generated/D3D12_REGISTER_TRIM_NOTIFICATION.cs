namespace Ghost.D3D12;

public unsafe partial struct D3D12_REGISTER_TRIM_NOTIFICATION
{
    [NativeTypeName("D3D12_PFN_TRIM_NOTIFICATION_CALLBACK")]
    public delegate* unmanaged[Stdcall]<D3D12_TRIM_NOTIFICATION*, void> pfnCallback;

    public void* pContext;

    [NativeTypeName("DWORD")]
    public uint CallbackCookie;
}
