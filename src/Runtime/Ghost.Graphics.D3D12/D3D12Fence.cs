using Ghost.Graphics.RHI;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12Fence : D3D12Object<ID3D12Fence>, IFence
{
    private readonly AutoResetEvent _fenceEvent;

    public ulong CompletedValue => pNativeObject->GetCompletedValue();
    public nint WaitHandle => _fenceEvent.SafeWaitHandle.DangerousGetHandle();

    private static ID3D12Fence* CreateFence(D3D12RenderDevice device, ulong initialValue)
    {
        ID3D12Fence* pFence = default;
        ThrowIfFailed(device.NativeObject.Get()->CreateFence(initialValue, D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE, __uuidof(pFence), (void**)&pFence));
        return pFence;
    }

    public D3D12Fence(D3D12RenderDevice device, ulong initialValue = 0)
        : base(CreateFence(device, initialValue))
    {
        _fenceEvent = new AutoResetEvent(false);
    }

    public void WaitForValue(ulong value)
    {
        AssertNotDisposed();

        if (pNativeObject->GetCompletedValue() < value)
        {
            var handle = new HANDLE((void*)WaitHandle);
            if (pNativeObject->SetEventOnCompletion(value, handle).SUCCEEDED)
            {
                _fenceEvent.WaitOne();
            }
        }
    }

    public Task WaitForValueAsync(ulong value)
    {
        AssertNotDisposed();

        if (pNativeObject->GetCompletedValue() >= value)
        {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource();
        var handle = new HANDLE((void*)_fenceEvent.SafeWaitHandle.DangerousGetHandle());

        if (pNativeObject->SetEventOnCompletion(value, handle).FAILED)
        {
            throw new InvalidOperationException("Failed to set event on completion.");
        }

        var registeredWait = ThreadPool.RegisterWaitForSingleObject(
            _fenceEvent,
            (state, timedOut) =>
            {
                var capturedTcs = (TaskCompletionSource)state!;
                capturedTcs.SetResult();
                _fenceEvent.Dispose();
            },
            tcs,
            Timeout.Infinite,
            executeOnlyOnce: true
        );

        tcs.Task.ContinueWith(_ => registeredWait.Unregister(null));

        return tcs.Task;
    }
}
