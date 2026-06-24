using Ghost.Graphics.RHI;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12Fence : D3D12Object<ID3D12Fence>, IFence
{
    [ThreadStatic]
    private static AutoResetEvent? t_waitEvent;

    private static AutoResetEvent GetThreadEvent()
    {
        t_waitEvent ??= new AutoResetEvent(false);
        return t_waitEvent;
    }

    private static ID3D12Fence* CreateFence(D3D12RenderDevice device, ulong initialValue)
    {
        ID3D12Fence* pFence = default;
        ThrowIfFailed(device.NativeObject.Get()->CreateFence(initialValue, D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE, __uuidof(pFence), (void**)&pFence));
        return pFence;
    }

    public D3D12Fence(D3D12RenderDevice device, ulong initialValue = 0)
        : base(CreateFence(device, initialValue))
    {
    }

    public ulong CompletedValue => pNativeObject->GetCompletedValue();

    public void WaitForValue(ulong value)
    {
        AssertNotDisposed();

        if (pNativeObject->GetCompletedValue() < value)
        {
            var waitEvent = GetThreadEvent();
            var handle = new HANDLE((void*)waitEvent.SafeWaitHandle.DangerousGetHandle());
            if (pNativeObject->SetEventOnCompletion(value, handle).SUCCEEDED)
            {
                waitEvent.WaitOne();
            }
        }
    }

    private class AsyncWaitState
    {
        public readonly TaskCompletionSource Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly AutoResetEvent Event = new(false);
        public RegisteredWaitHandle? RegisteredWait;
        private int _cleanedUp;

        public void Cleanup()
        {
            if (Interlocked.Exchange(ref _cleanedUp, 1) == 0)
            {
                RegisteredWait?.Unregister(null);
                Event.Dispose();
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

        var state = new AsyncWaitState();
        var handle = new HANDLE((void*)state.Event.SafeWaitHandle.DangerousGetHandle());

        if (pNativeObject->SetEventOnCompletion(value, handle).FAILED)
        {
            state.Cleanup();
            throw new InvalidOperationException("Failed to set event on completion.");
        }

        lock (state)
        {
            state.RegisteredWait = ThreadPool.RegisterWaitForSingleObject(
                state.Event,
                (s, timedOut) =>
                {
                    var waitState = (AsyncWaitState)s!;
                    waitState.Tcs.TrySetResult();
                    lock (waitState)
                    {
                        waitState.Cleanup();
                    }
                },
                state,
                Timeout.Infinite,
                executeOnlyOnce: true
            );
        }

        return state.Tcs.Task;
    }
}
