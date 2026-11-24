namespace Ghost.DXGI;

public partial struct DXGI_SAMPLE_DESC
{
    public DXGI_SAMPLE_DESC(uint count, uint quality)
    {
        Count = count;
        Quality = quality;
    }
}
