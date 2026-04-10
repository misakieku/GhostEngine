using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcInclusion.xml' path='doc/member[@name="IDxcInclusion"]/*' />
[Guid("0C364D65-DF44-4412-888E-4E552FC5E3D6")]
[NativeTypeName("struct IDxcInclusion : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcInclusion : IDxcInclusion.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcInclusion);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcInclusion*, Guid*, void**, int>)(lpVtbl[0]))((IDxcInclusion*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcInclusion*, uint>)(lpVtbl[1]))((IDxcInclusion*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcInclusion*, uint>)(lpVtbl[2]))((IDxcInclusion*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcInclusion.xml' path='doc/member[@name="IDxcInclusion.GetIncludedFile"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetIncludedFile(IDxcFile** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcInclusion*, IDxcFile**, int>)(lpVtbl[3]))((IDxcInclusion*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcInclusion.xml' path='doc/member[@name="IDxcInclusion.GetStackLength"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetStackLength([NativeTypeName("unsigned int *")] uint* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcInclusion*, uint*, int>)(lpVtbl[4]))((IDxcInclusion*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcInclusion.xml' path='doc/member[@name="IDxcInclusion.GetStackItem"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetStackItem([NativeTypeName("unsigned int")] uint index, IDxcSourceLocation** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcInclusion*, uint, IDxcSourceLocation**, int>)(lpVtbl[5]))((IDxcInclusion*)Unsafe.AsPointer(ref this), index, pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetIncludedFile(IDxcFile** pResult);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetStackLength([NativeTypeName("unsigned int *")] uint* pResult);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetStackItem([NativeTypeName("unsigned int")] uint index, IDxcSourceLocation** pResult);
    }

    public partial struct Vtbl<TSelf>
        where TSelf : unmanaged, Interface
    {
        [NativeTypeName("HRESULT (const IID &, void **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, Guid*, void**, int> QueryInterface;

        [NativeTypeName("ULONG () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> AddRef;

        [NativeTypeName("ULONG () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> Release;

        [NativeTypeName("HRESULT (IDxcFile **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcFile**, int> GetIncludedFile;

        [NativeTypeName("HRESULT (unsigned int *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetStackLength;

        [NativeTypeName("HRESULT (unsigned int, IDxcSourceLocation **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcSourceLocation**, int> GetStackItem;
    }
}
