using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Utilities;

public static unsafe class RenderThreadTempAllocatorManager
{
    private partial struct RenderThreadTempAllocator : IDisposable
    {
        private MemoryPool<VirtualArena, VirtualArena.CreationOptions> _arena;

        public readonly AllocationHandle AllocationHandle => _arena.AllocationHandle;

        public RenderThreadTempAllocator()
        {
            _arena = new MemoryPool<VirtualArena, VirtualArena.CreationOptions>(new VirtualArena.CreationOptions
            {
                reserveCapacity = 64 * 1024 * 1024, // 64 MB
            });
        }

        public readonly void Reset()
        {
            _arena.Allocator.Reset();
        }

        public void Dispose()
        {
            _arena.Dispose();
        }
    }

    private static readonly RenderThreadTempAllocator* s_pAllocator;

    static RenderThreadTempAllocatorManager()
    {
        s_pAllocator = (RenderThreadTempAllocator*)NativeMemory.Alloc((nuint)sizeof(RenderThreadTempAllocator));
        *s_pAllocator = new RenderThreadTempAllocator();
    }

    internal static void Reset()
    {
        s_pAllocator->Reset();
    }

    internal static void Dispose()
    {
        s_pAllocator->Dispose();
        NativeMemory.Free(s_pAllocator);
    }

    extension(AllocationHandle)
    {
        public static AllocationHandle TempRender => s_pAllocator->AllocationHandle;
    }
}
