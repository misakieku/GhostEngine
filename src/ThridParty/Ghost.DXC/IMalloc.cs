using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.DXC;

[Guid("00000002-0000-0000-C000-000000000046")]
[NativeTypeName("struct IMalloc : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IMalloc
{
    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IMalloc*, Guid*, void**, int>)(lpVtbl[0]))((IMalloc*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IMalloc*, uint>)(lpVtbl[1]))((IMalloc*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IMalloc*, uint>)(lpVtbl[2]))((IMalloc*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void* Alloc([NativeTypeName("SIZE_T")] nuint cb)
    {
        return ((delegate* unmanaged[MemberFunction]<IMalloc*, nuint, void*>)(lpVtbl[3]))((IMalloc*)Unsafe.AsPointer(ref this), cb);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public void* Realloc(void* pv, [NativeTypeName("SIZE_T")] nuint cb)
    {
        return ((delegate* unmanaged[MemberFunction]<IMalloc*, void*, nuint, void*>)(lpVtbl[4]))((IMalloc*)Unsafe.AsPointer(ref this), pv, cb);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public void Free(void* pv)
    {
        ((delegate* unmanaged[MemberFunction]<IMalloc*, void*, void>)(lpVtbl[5]))((IMalloc*)Unsafe.AsPointer(ref this), pv);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("SIZE_T")]
    public nuint GetSize(void* pv)
    {
        return ((delegate* unmanaged[MemberFunction]<IMalloc*, void*, nuint>)(lpVtbl[6]))((IMalloc*)Unsafe.AsPointer(ref this), pv);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public int DidAlloc(void* pv)
    {
        return ((delegate* unmanaged[MemberFunction]<IMalloc*, void*, int>)(lpVtbl[7]))((IMalloc*)Unsafe.AsPointer(ref this), pv);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public void HeapMinimize()
    {
        ((delegate* unmanaged[MemberFunction]<IMalloc*, void>)(lpVtbl[8]))((IMalloc*)Unsafe.AsPointer(ref this));
    }

    public partial struct Vtbl<TSelf>
        where TSelf : unmanaged
    {
        [NativeTypeName("HRESULT (const IID &, void **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, Guid*, void**, int> QueryInterface;

        [NativeTypeName("ULONG () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> AddRef;

        [NativeTypeName("ULONG () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> Release;

        [NativeTypeName("void *(SIZE_T) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, nuint, void*> Alloc;

        [NativeTypeName("void *(void *, SIZE_T) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void*, nuint, void*> Realloc;

        [NativeTypeName("void (void *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void*, void> Free;

        [NativeTypeName("SIZE_T (void *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void*, nuint> GetSize;

        [NativeTypeName("int (void *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void*, int> DidAlloc;

        [NativeTypeName("void () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void> HeapMinimize;
    }
}
