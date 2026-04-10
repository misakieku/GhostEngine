using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcToken.xml' path='doc/member[@name="IDxcToken"]/*' />
[Guid("7F90B9FF-A275-4932-97D8-3CFD234482A2")]
[NativeTypeName("struct IDxcToken : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcToken : IDxcToken.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcToken);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcToken*, Guid*, void**, int>)(lpVtbl[0]))((IDxcToken*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcToken*, uint>)(lpVtbl[1]))((IDxcToken*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcToken*, uint>)(lpVtbl[2]))((IDxcToken*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcToken.xml' path='doc/member[@name="IDxcToken.GetKind"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetKind(DxcTokenKind* pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcToken*, DxcTokenKind*, int>)(lpVtbl[3]))((IDxcToken*)Unsafe.AsPointer(ref this), pValue);
    }

    /// <include file='IDxcToken.xml' path='doc/member[@name="IDxcToken.GetLocation"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetLocation(IDxcSourceLocation** pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcToken*, IDxcSourceLocation**, int>)(lpVtbl[4]))((IDxcToken*)Unsafe.AsPointer(ref this), pValue);
    }

    /// <include file='IDxcToken.xml' path='doc/member[@name="IDxcToken.GetExtent"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetExtent(IDxcSourceRange** pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcToken*, IDxcSourceRange**, int>)(lpVtbl[5]))((IDxcToken*)Unsafe.AsPointer(ref this), pValue);
    }

    /// <include file='IDxcToken.xml' path='doc/member[@name="IDxcToken.GetSpelling"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetSpelling([NativeTypeName("LPSTR *")] sbyte** pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcToken*, sbyte**, int>)(lpVtbl[6]))((IDxcToken*)Unsafe.AsPointer(ref this), pValue);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetKind(DxcTokenKind* pValue);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetLocation(IDxcSourceLocation** pValue);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetExtent(IDxcSourceRange** pValue);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetSpelling([NativeTypeName("LPSTR *")] sbyte** pValue);
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

        [NativeTypeName("HRESULT (DxcTokenKind *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DxcTokenKind*, int> GetKind;

        [NativeTypeName("HRESULT (IDxcSourceLocation **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcSourceLocation**, int> GetLocation;

        [NativeTypeName("HRESULT (IDxcSourceRange **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcSourceRange**, int> GetExtent;

        [NativeTypeName("HRESULT (LPSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, sbyte**, int> GetSpelling;
    }
}
