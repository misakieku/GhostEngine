namespace Ghost.DXGI;

public unsafe partial struct DXGI_SHARED_RESOURCE
{
    [NativeTypeName("HANDLE")]
    public void* Handle;
}
