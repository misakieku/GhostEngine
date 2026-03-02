using Ghost.Graphics.RHI;
using System.Diagnostics.CodeAnalysis;

namespace Ghost.Graphics;

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
            int current = Volatile.Read(ref _refCount);
            if (current == 0) return false; // It's dead, let it go.

            if (Interlocked.CompareExchange(ref _refCount, current + 1, current) == current)
            {
                return true; // Successfully atomically incremented
            }
        }
    }

    public bool ReleaseRef()
    {
        while (true)
        {
            int current = Volatile.Read(ref _refCount);
            if (current == 0)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref _refCount, current - 1, current) == current)
            {
                return (current - 1) == 0;
            }
        }
    }
}

internal class SwapChainManager
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
                if (record.TryAddRef()) return record.SwapChain;

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

    public void ReleaseSwapChain(int index)
    {
        var record = Volatile.Read(ref _swapChains[index]);

        if (record != null && record.ReleaseRef())
        {
            record.SwapChain.Dispose();
            Interlocked.CompareExchange(ref _swapChains[index], null, record);
        }
    }
}
