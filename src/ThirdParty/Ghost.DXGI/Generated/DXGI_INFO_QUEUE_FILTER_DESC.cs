namespace Ghost.DXGI;

public unsafe partial struct DXGI_INFO_QUEUE_FILTER_DESC
{
    public uint NumCategories;

    public DXGI_INFO_QUEUE_MESSAGE_CATEGORY* pCategoryList;

    public uint NumSeverities;

    public DXGI_INFO_QUEUE_MESSAGE_SEVERITY* pSeverityList;

    public uint NumIDs;

    [NativeTypeName("DXGI_INFO_QUEUE_MESSAGE_ID *")]
    public int* pIDList;
}
