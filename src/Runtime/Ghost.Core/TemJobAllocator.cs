using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Ghost.Core;

public unsafe partial struct TempJobAllocator
{
    private static TempJobAllocator* _pAllocator;

    public static AllocationHandle AllocationHandle => _pAllocator->Handle;

    internal static void Initialize(nuint capacity)
    {
        Logger.DebugAssert(_pAllocator == null, "TempJobAllocator is already initialized.");
        _pAllocator = (TempJobAllocator*)Malloc((nuint)sizeof(TempJobAllocator));
    }

    internal static void Dispose()
    {
        if (_pAllocator == null)
        {
            return;
        }

        for (var i = 0; i < _FRAME_LATENCY; i++)
        {
            _pAllocator->_pArena[i].Dispose();
        }

        MemoryUtility.Free(_pAllocator->_pArena);
        MemoryUtility.Free(_pAllocator);

        _pAllocator = null;
    }
}

public unsafe partial struct TempJobAllocator : IAllocator
{
    private const int _FRAME_LATENCY = 4;
    private const int _MAGIC_ID = -559038737;

    private VirtualArena* _pArena;
    private int _currentFrameCount;
    private int _currentFrameIndex;
    private fixed int _allocationsPerFrame[_FRAME_LATENCY];

    private MemoryHandle _memoryHandle;
    private AllocationHandle _handle;

    public readonly AllocationHandle Handle => _handle;

    internal TempJobAllocator(void* pSelf, nuint capacity)
    {
        var memoryHandle = default(MemoryHandle);

        _pArena = (VirtualArena*)Malloc((nuint)(sizeof(VirtualArena) * _FRAME_LATENCY));
        _currentFrameCount = 0;
        _currentFrameIndex = 0;
        _memoryHandle = memoryHandle;

        for (var i = 0; i < _FRAME_LATENCY; i++)
        {
            _pArena[i] = new VirtualArena(capacity);
            _allocationsPerFrame[i] = 0;
        }

        _handle = new AllocationHandle
        {
            State = Unsafe.AsPointer(ref this),
            Alloc = &Allocate,
            Realloc = &Reallocate,
            Free = &Free
        };
    }

    private static void* Allocate(void* instance, nuint size, nuint alignment, AllocationOption allocationOption)
    {
        var pSelf = (TempJobAllocator*)instance;
        var pCurrentArena = pSelf->_pArena + pSelf->_currentFrameIndex;
        var ptr = pCurrentArena->Allocate(size, alignment, allocationOption);
        if (ptr == null)
        {
            return null;
        }

        Interlocked.Increment(ref pSelf->_allocationsPerFrame[pSelf->_currentFrameIndex]);
        return ptr;
    }

    private static void* Reallocate(void* instance, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption allocationOption)
    {
        if (ptr == null)
        {
            return Allocate(instance, newSize, alignment, allocationOption);
        }

        var pSelf = (TempJobAllocator*)instance;
        var pCurrentArena = pSelf->_pArena + pSelf->_currentFrameIndex;
        var newPtr = pCurrentArena->Allocate(newSize, alignment, allocationOption);
        if (newPtr == null)
        {
            return null;
        }

        MemCpy(ptr, newPtr, Math.Min(oldSize, newSize));

        return newPtr;
    }

    private static void Free(void* instance, void* ptr)
    {
        var pSelf = (TempJobAllocator*)instance;
        Interlocked.Decrement(ref pSelf->_allocationsPerFrame[pSelf->_currentFrameIndex]);
    }

    public int AdvanceFrame()
    {
        var allocations = Interlocked.Exchange(ref _allocationsPerFrame[_currentFrameIndex], 0);

        _currentFrameCount++;
        _currentFrameIndex = _currentFrameCount % _FRAME_LATENCY;

        (_pArena + _currentFrameIndex)->Reset();

        return allocations;
    }
}
