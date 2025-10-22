using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Zeux.MeshOptimizer
{
    public unsafe partial struct meshopt_Allocator
    {
        private static readonly Storage s_storage;

        [NativeTypeName("void *[24]")]
        private _blocks_e__FixedBuffer _blocks;

        [NativeTypeName("size_t")]
        private nuint _count;

        static meshopt_Allocator()
        {
            s_storage = new Storage
            {
                // Use .NET's unmanaged memory functions.
                allocate = &NativeMemory_Alloc,
                deallocate = &NativeMemory_Free,
            };
        }

        public meshopt_Allocator()
        {
            _count = 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void* NativeMemory_Alloc(nuint size)
        {
            return NativeMemory.Alloc(size);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void NativeMemory_Free(void* ptr)
        {
            NativeMemory.Free(ptr);
        }

        public T* allocate<T>(nuint size) where T : unmanaged
        {
            Debug.Assert(_count < 24, "Allocator block limit reached");

            // Calculate total bytes needed
            var bytes = size * (nuint)sizeof(T);
            var result = (T*)s_storage.allocate(bytes);

            _blocks[_count++] = result;
            return result;
        }

        public void deallocate(void* ptr)
        {
            Debug.Assert(_count > 0 && _blocks[_count - 1] == ptr, "Deallocation is not in LIFO order");
            s_storage.deallocate(ptr);
            _count--;
        }

        public unsafe partial struct Storage
        {
            [NativeTypeName("void *(*)(size_t) __attribute__((cdecl))")]
            public delegate* unmanaged[Cdecl]<nuint, void*> allocate;

            [NativeTypeName("void (*)(void *) __attribute__((cdecl))")]
            public delegate* unmanaged[Cdecl]<void*, void> deallocate;
        }

        public void Dispose()
        {
            for (var i = _count; i > 0; --i)
            {
                s_storage.deallocate(_blocks[i - 1]);
            }
        }

        private unsafe partial struct _blocks_e__FixedBuffer
        {
            private void* e0;
            private void* e1;
            private void* e2;
            private void* e3;
            private void* e4;
            private void* e5;
            private void* e6;
            private void* e7;
            private void* e8;
            private void* e9;
            private void* e10;
            private void* e11;
            private void* e12;
            private void* e13;
            private void* e14;
            private void* e15;
            private void* e16;
            private void* e17;
            private void* e18;
            private void* e19;
            private void* e20;
            private void* e21;
            private void* e22;
            private void* e23;

            public ref void* this[nuint index]
            {
                get
                {
                    fixed (void** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }
}
