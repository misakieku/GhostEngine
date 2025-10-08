using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Zeux.MeshOptimizer;

public unsafe partial struct meshopt_Stream
{
    [NativeTypeName("const void *")]
    public void* data;

    [NativeTypeName("size_t")]
    public nuint size;

    [NativeTypeName("size_t")]
    public nuint stride;
}

public enum meshopt_EncodeExpMode
{
    meshopt_EncodeExpSeparate,
    meshopt_EncodeExpSharedVector,
    meshopt_EncodeExpSharedComponent,
    meshopt_EncodeExpClamped,
}

public enum meshopt_SimplifyOptions
{
    meshopt_SimplifyLockBorder = 1 << 0,
    meshopt_SimplifySparse = 1 << 1,
    meshopt_SimplifyErrorAbsolute = 1 << 2,
    meshopt_SimplifyPrune = 1 << 3,
    meshopt_SimplifyRegularize = 1 << 4,
    meshopt_SimplifyPermissive = 1 << 5,
}

public enum meshopt_SimplifyVertexFlags
{
    meshopt_SimplifyVertex_Lock = 1 << 0,
    meshopt_SimplifyVertex_Protect = 1 << 1,
}

public partial struct meshopt_VertexCacheStatistics
{
    [NativeTypeName("unsigned int")]
    public uint vertices_transformed;

    [NativeTypeName("unsigned int")]
    public uint warps_executed;

    public float acmr;

    public float atvr;
}

public partial struct meshopt_VertexFetchStatistics
{
    [NativeTypeName("unsigned int")]
    public uint bytes_fetched;

    public float overfetch;
}

public partial struct meshopt_OverdrawStatistics
{
    [NativeTypeName("unsigned int")]
    public uint pixels_covered;

    [NativeTypeName("unsigned int")]
    public uint pixels_shaded;

    public float overdraw;
}

public partial struct meshopt_CoverageStatistics
{
    [NativeTypeName("float[3]")]
    public _coverage_e__FixedBuffer coverage;

    public float extent;

    [InlineArray(3)]
    public partial struct _coverage_e__FixedBuffer
    {
        public float e0;
    }
}

public partial struct meshopt_Meshlet
{
    [NativeTypeName("unsigned int")]
    public uint vertex_offset;

    [NativeTypeName("unsigned int")]
    public uint triangle_offset;

    [NativeTypeName("unsigned int")]
    public uint vertex_count;

    [NativeTypeName("unsigned int")]
    public uint triangle_count;
}

public partial struct meshopt_Bounds
{
    [NativeTypeName("float[3]")]
    public _center_e__FixedBuffer center;

    public float radius;

    [NativeTypeName("float[3]")]
    public _cone_apex_e__FixedBuffer cone_apex;

    [NativeTypeName("float[3]")]
    public _cone_axis_e__FixedBuffer cone_axis;

    public float cone_cutoff;

    [NativeTypeName("signed char[3]")]
    public _cone_axis_s8_e__FixedBuffer cone_axis_s8;

    [NativeTypeName("signed char")]
    public sbyte cone_cutoff_s8;

    [InlineArray(3)]
    public partial struct _center_e__FixedBuffer
    {
        public float e0;
    }

    [InlineArray(3)]
    public partial struct _cone_apex_e__FixedBuffer
    {
        public float e0;
    }

    [InlineArray(3)]
    public partial struct _cone_axis_e__FixedBuffer
    {
        public float e0;
    }

    [InlineArray(3)]
    public partial struct _cone_axis_s8_e__FixedBuffer
    {
        public sbyte e0;
    }
}

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
