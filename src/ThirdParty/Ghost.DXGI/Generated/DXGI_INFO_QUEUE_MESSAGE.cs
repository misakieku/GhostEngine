using System;

namespace Ghost.DXGI;

public unsafe partial struct DXGI_INFO_QUEUE_MESSAGE
{
    [NativeTypeName("DXGI_DEBUG_ID")]
    public Guid Producer;

    public DXGI_INFO_QUEUE_MESSAGE_CATEGORY Category;

    public DXGI_INFO_QUEUE_MESSAGE_SEVERITY Severity;

    [NativeTypeName("DXGI_INFO_QUEUE_MESSAGE_ID")]
    public int ID;

    [NativeTypeName("const char *")]
    public sbyte* pDescription;

    [NativeTypeName("SIZE_T")]
    public nuint DescriptionByteLength;
}
