using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.InteropServices;

namespace Ghost.Core;

public static class TempJobAllocatorHandle
{
    extension(AllocationHandle)
    {
        /// <summary>
        /// Gets the allocation handle for the TempJobAllocator, which is designed for temporary allocations within jobs. This allocator provides a simple interface for allocating and freeing memory that is automatically reset after a certain number of frames, making it ideal for use in job systems where temporary data is needed.
        /// </summary>
        public static AllocationHandle TempJob => TempJobAllocator.AllocationHandle;
    }
}

public unsafe partial struct TempJobAllocator
{
    private static TempJobAllocator* _pAllocator;

    public static AllocationHandle AllocationHandle => _pAllocator->Handle;

    internal static void Initialize(nuint capacity)
    {
        Logger.DebugAssert(_pAllocator == null, "TempJobAllocator is already initialized.");
        _pAllocator = (TempJobAllocator*)NativeMemory.Alloc((nuint)sizeof(TempJobAllocator));
        *_pAllocator = new TempJobAllocator(_pAllocator, capacity);
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

        NativeMemory.Free(_pAllocator->_pArena);
        NativeMemory.Free(_pAllocator);

        _pAllocator = null;
    }
}

public unsafe partial struct TempJobAllocator : IAllocator
{
    private const int _FRAME_LATENCY = 4;

    private VirtualArena* _pArena;
    private int _currentFrameCount;
    private int _currentFrameIndex;
#if MHP_ENABLE_SAFETY_CHECKS
    private fixed int _allocationsPerFrame[_FRAME_LATENCY];
#endif

    private readonly AllocationHandle _handle;

    public readonly AllocationHandle Handle => _handle;

    internal TempJobAllocator(void* pSelf, nuint capacity)
    {
        _pArena = (VirtualArena*)NativeMemory.Alloc((nuint)(sizeof(VirtualArena) * _FRAME_LATENCY));
        _currentFrameCount = 0;
        _currentFrameIndex = 0;

        for (var i = 0; i < _FRAME_LATENCY; i++)
        {
            _pArena[i] = new VirtualArena(capacity);
#if MHP_ENABLE_SAFETY_CHECKS
            _allocationsPerFrame[i] = 0;
#endif
        }

        _handle = new AllocationHandle(pSelf, &Allocate, &Reallocate, &Free);
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

#if MHP_ENABLE_SAFETY_CHECKS
        Interlocked.Increment(ref pSelf->_allocationsPerFrame[pSelf->_currentFrameIndex]);
#endif
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

        MemoryUtility.MemCpy(ptr, newPtr, Math.Min(oldSize, newSize));

        return newPtr;
    }

    private static void Free(void* instance, void* ptr)
    {
        var pSelf = (TempJobAllocator*)instance;
#if MHP_ENABLE_SAFETY_CHECKS
        Interlocked.Decrement(ref pSelf->_allocationsPerFrame[pSelf->_currentFrameIndex]);
#endif
    }

    public int AdvanceFrame()
    {
        var allocations = Interlocked.Exchange(ref _allocationsPerFrame[_currentFrameIndex], 0);

        _currentFrameCount++;
        _currentFrameIndex = _currentFrameCount % _FRAME_LATENCY;

        (_pArena + _currentFrameIndex)->Reset();

#if MHP_ENABLE_SAFETY_CHECKS
        if (_allocationsPerFrame[_currentFrameIndex] != 0)
        {
            Logger.Error($"TempJobAllocator: Detected {_allocationsPerFrame[_currentFrameIndex]} leaked allocations from frame {_currentFrameCount - _FRAME_LATENCY}.");
        }
#endif

        return allocations;
    }
}