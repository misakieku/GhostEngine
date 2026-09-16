using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.InteropServices;

namespace Ghost.Core;

public static unsafe class TempJobAllocatorManager
{
    private partial struct TempJobAllocator : IAllocator, IDisposable
    {
        public const int FRAME_LATENCY = 4;

        private readonly VirtualArena* _pArena;
        private readonly AllocationHandle.Allocator* _pAllocator;
        private int _currentFrameCount;
        private int _currentFrameIndex;

        private readonly AllocationHandle _handle;

        public readonly AllocationHandle Handle => _handle;

        internal TempJobAllocator(void* pSelf, nuint capacity)
        {
            _pArena = (VirtualArena*)NativeMemory.Alloc((nuint)(sizeof(VirtualArena) * FRAME_LATENCY));
            _currentFrameCount = 0;
            _currentFrameIndex = 0;

            for (var i = 0; i < FRAME_LATENCY; i++)
            {
                _pArena[i] = new VirtualArena(capacity);
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

            MemoryUtility.MemCpy(newPtr, ptr, Math.Min(oldSize, newSize));

            return newPtr;
        }

        private static void Free(void* instance, void* ptr)
        {
            var pSelf = (TempJobAllocator*)instance;
        }

        public void AdvanceFrame()
        {
            _currentFrameCount++;
            _currentFrameIndex = _currentFrameCount % FRAME_LATENCY;

            (_pArena + _currentFrameIndex)->Reset();
        }

        public readonly void Dispose()
        {
            for (var i = 0; i < FRAME_LATENCY; i++)
            {
                s_pAllocator->_pArena[i].Dispose();
            }

            NativeMemory.Free(s_pAllocator->_pArena);
        }
    }

    private static TempJobAllocator* s_pAllocator;

    static TempJobAllocatorManager()
    {
        s_pAllocator = (TempJobAllocator*)NativeMemory.Alloc((nuint)sizeof(TempJobAllocator));
        *s_pAllocator = new TempJobAllocator(s_pAllocator, 64 * 1024 * 1024); // 64 MB
    }

    internal static void AdvanceFrame()
    {
        if (s_pAllocator == null)
        {
            return;
        }

        s_pAllocator->AdvanceFrame();
    }

    internal static void Dispose()
    {
        if (s_pAllocator == null)
        {
            return;
        }

        s_pAllocator->Dispose();
        NativeMemory.Free(s_pAllocator);

        s_pAllocator = null;
    }

    extension(AllocationHandle)
    {
        /// <summary>
        /// Gets the allocation handle for the TempJobAllocator, which is designed for temporary allocations within jobs. This allocator provides a simple interface for allocating and freeing memory that is automatically reset after a certain number of frames, making it ideal for use in job systems where temporary data is needed.
        /// </summary>
        public static AllocationHandle TempJob => s_pAllocator->Handle;
    }
}