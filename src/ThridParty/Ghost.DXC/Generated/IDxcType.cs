using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcType.xml' path='doc/member[@name="IDxcType"]/*' />
[Guid("2EC912FD-B144-4A15-AD0D-1C5439C81E46")]
[NativeTypeName("struct IDxcType : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcType : IDxcType.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcType);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcType*, Guid*, void**, int>)(lpVtbl[0]))((IDxcType*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcType*, uint>)(lpVtbl[1]))((IDxcType*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcType*, uint>)(lpVtbl[2]))((IDxcType*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcType.xml' path='doc/member[@name="IDxcType.GetSpelling"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetSpelling([NativeTypeName("LPSTR *")] sbyte** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcType*, sbyte**, int>)(lpVtbl[3]))((IDxcType*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcType.xml' path='doc/member[@name="IDxcType.IsEqualTo"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int IsEqualTo(IDxcType* other, [NativeTypeName("BOOL *")] int* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcType*, IDxcType*, int*, int>)(lpVtbl[4]))((IDxcType*)Unsafe.AsPointer(ref this), other, pResult);
    }

    /// <include file='IDxcType.xml' path='doc/member[@name="IDxcType.GetKind"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetKind(DxcTypeKind* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcType*, DxcTypeKind*, int>)(lpVtbl[5]))((IDxcType*)Unsafe.AsPointer(ref this), pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetSpelling([NativeTypeName("LPSTR *")] sbyte** pResult);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int IsEqualTo(IDxcType* other, [NativeTypeName("BOOL *")] int* pResult);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetKind(DxcTypeKind* pResult);
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

        [NativeTypeName("HRESULT (LPSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, sbyte**, int> GetSpelling;

        [NativeTypeName("HRESULT (IDxcType *, BOOL *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcType*, int*, int> IsEqualTo;

        [NativeTypeName("HRESULT (DxcTypeKind *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DxcTypeKind*, int> GetKind;
    }
}
