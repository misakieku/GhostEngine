using Ghost.Graphics.RHI;

namespace Ghost.UnitTest.MockingEnvironment;

internal class MockingFence : IFence
{
    private readonly AutoResetEvent _fenceEvent;

    private ulong _currentValue;

    public ulong CompletedValue => _currentValue;

    public nint WaitHandle => _fenceEvent.SafeWaitHandle.DangerousGetHandle();

    public string Name
    {
        get; set;
    } = "MockingFence";

    public MockingFence(ulong initialValue)
    {
        _fenceEvent = new AutoResetEvent(false);
        _currentValue = initialValue;
    }

    public void Signal(ulong value)
    {
        if (value > _currentValue)
        {
            _currentValue = value;
            _fenceEvent.Set();
        }
    }

    public void WaitForValue(ulong value)
    {
        if (value > _currentValue)
        {
            _fenceEvent.WaitOne();
        }
    }

    public Task WaitForValueAsync(ulong value)
    {
        return Task.Run(() => { WaitForValue(value); });
    }

    public void Dispose()
    {
        _fenceEvent.Dispose();
    }
}
