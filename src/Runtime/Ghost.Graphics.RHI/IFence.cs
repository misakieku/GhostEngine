namespace Ghost.Graphics.RHI;

public interface IFence : IRHIObject
{
    ulong CompletedValue
    {
        get;
    }


    void WaitForValue(ulong value);

    Task WaitForValueAsync(ulong value);
}