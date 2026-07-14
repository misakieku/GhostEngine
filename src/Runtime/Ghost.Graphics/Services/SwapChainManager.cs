using Ghost.Core;
using Ghost.Graphics.RHI;
using System.Diagnostics.CodeAnalysis;

namespace Ghost.Graphics.Services;

internal sealed class SwapChainRecord
{
    private int _refCount;

    public ISwapChain SwapChain { get; }

    public SwapChainRecord(ISwapChain swapChain)
    {
        SwapChain = swapChain;
        _refCount = 1;
    }

    public bool TryAddRef()
    {
        while (true)
        {
            var current = Volatile.Read(ref _refCount);
            if (current == 0)
            {
                return false; // It's dead, let it go.
            }

            if (Interlocked.CompareExchange(ref _refCount, current + 1, current) == current)
            {
                return true; // Successfully atomically incremented
            }
        }
    }

    public int ReleaseRef()
    {
        while (true)
        {
            var current = Volatile.Read(ref _refCount);
            if (current == 0)
            {
                return 0;
            }

            if (Interlocked.CompareExchange(ref _refCount, current - 1, current) == current)
            {
                return current - 1;
            }
        }
    }
}

public class SwapChainManager : IDisposable
{
    public const int MAX_SWAP_CHAINS = 8;
    private readonly IGraphicsEngine _graphicsEngine;
    private readonly SwapChainRecord?[] _swapChains = new SwapChainRecord?[MAX_SWAP_CHAINS];

    public SwapChainManager(IGraphicsEngine graphicsEngine)
    {
        _graphicsEngine = graphicsEngine;
    }

    public ISwapChain EnsureSwapChain(int index, SwapChainDesc desc)
    {
        while (true)
        {
            var record = Volatile.Read(ref _swapChains[index]);

            if (record != null)
            {
                if (record.TryAddRef())
                {
                    return record.SwapChain;
                }

                Thread.Yield();
                continue;
            }

            var newRecord = new SwapChainRecord(_graphicsEngine.CreateSwapChain(desc));
            var previous = Interlocked.CompareExchange(ref _swapChains[index], newRecord, null);

            if (previous == null)
            {
                return newRecord.SwapChain;
            }
            else
            {
                newRecord.SwapChain.Dispose();
            }
        }
    }

    public void CreateSwapChain(SwapChainDesc desc, out ISwapChain swapChain, out int index)
    {
        for (var i = 0; i < MAX_SWAP_CHAINS; i++)
        {
            var record = Volatile.Read(ref _swapChains[i]);
            if (record == null)
            {
                var newRecord = new SwapChainRecord(_graphicsEngine.CreateSwapChain(desc));
                var previous = Interlocked.CompareExchange(ref _swapChains[i], newRecord, null);
                if (previous == null)
                {
                    swapChain = newRecord.SwapChain;
                    index = i;
                }
                else
                {
                    newRecord.SwapChain.Dispose();
                }
            }
        }

        throw new InvalidOperationException("Maximum number of swap chains reached.");
    }

    public bool TryGetSwapChain(int index, [MaybeNullWhen(false)] out ISwapChain swapChain)
    {
        var record = Volatile.Read(ref _swapChains[index]);
        if (record != null && record.TryAddRef())
        {
            swapChain = record.SwapChain;
            return true;
        }

        swapChain = null;
        return false;
    }

    public int ReleaseSwapChain(int index)
    {
        var record = Volatile.Read(ref _swapChains[index]);

        if (record != null)
        {
            var refCount = record.ReleaseRef();
            if (refCount == 0)
            {
                record.SwapChain.Dispose();
                Interlocked.CompareExchange(ref _swapChains[index], null, record);
            }

            return refCount;
        }

        return -1;
    }

    public void TransitionToPresent(int index, ICommandBuffer commandBuffer)
    {
        var record = Volatile.Read(ref _swapChains[index]);
        if (record == null)
        {
            Logger.Debug($"No swap chain at index {index} to transition to present.");
            return;
        }

        commandBuffer.Barrier(BarrierDesc.Texture(record.SwapChain.GetCurrentBackBuffer(),
            BarrierSync.None,
            BarrierAccess.NoAccess,
            BarrierLayout.Present));
    }

    public void TransitionAllToPresent(ICommandBuffer commandBuffer)
    {
        for (var i = 0; i < MAX_SWAP_CHAINS; i++)
        {
            var record = Volatile.Read(ref _swapChains[i]);
            if (record == null)
            {
                continue;
            }

            commandBuffer.Barrier(BarrierDesc.Texture(record.SwapChain.GetCurrentBackBuffer(),
                BarrierSync.None,
                BarrierAccess.NoAccess,
                BarrierLayout.Present));
        }
    }

    public void Present(int index)
    {
        var record = Volatile.Read(ref _swapChains[index]);
        if (record == null)
        {
            Logger.Debug($"No swap chain at index {index} to present.");
            return;
        }

        record.SwapChain.Present();
    }

    public void PresentAll()
    {
        for (var i = 0; i < MAX_SWAP_CHAINS; i++)
        {
            var record = Volatile.Read(ref _swapChains[i]);
            if (record == null)
            {
                continue;
            }

            record?.SwapChain.Present();
        }
    }

    public void Dispose()
    {
        for (var i = 0; i < MAX_SWAP_CHAINS; i++)
        {
            var record = Volatile.Read(ref _swapChains[i]);
            if (record != null)
            {
                record.SwapChain.Dispose();
                Interlocked.CompareExchange(ref _swapChains[i], null, record);
            }
        }

        GC.SuppressFinalize(this);
    }
}
