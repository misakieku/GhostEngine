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
    private static TempJobAllocator* s_pAllocator;

    public static AllocationHandle AllocationHandle => s_pAllocator->Handle;

    internal static void Initialize(nuint capacity)
    {
        Logger.DebugAssert(s_pAllocator == null, "TempJobAllocator is already initialized.");
        s_pAllocator = (TempJobAllocator*)NativeMemory.Alloc((nuint)sizeof(TempJobAllocator));
        *s_pAllocator = new TempJobAllocator(s_pAllocator, capacity);
    }

    internal static void Dispose()
    {
        if (s_pAllocator == null)
        {
            return;
        }

        for (var i = 0; i < _FRAME_LATENCY; i++)
        {
            s_pAllocator->_pArena[i].Dispose();
        }

        NativeMemory.Free(s_pAllocator->_pArena);
        NativeMemory.Free(s_pAllocator);

        s_pAllocator = null;
    }
}

public unsafe partial struct TempJobAllocator : IAllocator
{
    private const int _FRAME_LATENCY = 4;

    private VirtualArena* _pArena;
    private AllocationHandle.Allocator* _pAllocator;
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

        _pAllocator = (AllocationHandle.Allocator*)NativeMemory.Alloc((nuint)sizeof(AllocationHandle.Allocator));
        *_pAllocator = new AllocationHandle.Allocator
        {
            state = pSelf,
            alloc = &Allocate,
            realloc = &Reallocate,
            free = &Free,
        };

        _handle = new AllocationHandle(_pAllocator);
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

    public void AdvanceFrame()
    {
        _currentFrameCount++;
        _currentFrameIndex = _currentFrameCount % _FRAME_LATENCY;

        (_pArena + _currentFrameIndex)->Reset();

#if MHP_ENABLE_SAFETY_CHECKS
        var allocations = Interlocked.Exchange(ref _allocationsPerFrame[_currentFrameIndex], 0);
        if (allocations != 0)
        {
            Logger.Error($"TempJobAllocator: Detected {allocations} leaked allocations from frame {_currentFrameCount - _FRAME_LATENCY}.");
        }
#endif
    }
}