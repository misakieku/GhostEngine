using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcSourceLocation.xml' path='doc/member[@name="IDxcSourceLocation"]/*' />
[Guid("8E7DDF1C-D7D3-4D69-B286-85FCCBA1E0CF")]
[NativeTypeName("struct IDxcSourceLocation : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcSourceLocation : IDxcSourceLocation.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcSourceLocation);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceLocation*, Guid*, void**, int>)(lpVtbl[0]))((IDxcSourceLocation*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceLocation*, uint>)(lpVtbl[1]))((IDxcSourceLocation*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceLocation*, uint>)(lpVtbl[2]))((IDxcSourceLocation*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcSourceLocation.xml' path='doc/member[@name="IDxcSourceLocation.IsEqualTo"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int IsEqualTo(IDxcSourceLocation* other, [NativeTypeName("BOOL *")] int* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceLocation*, IDxcSourceLocation*, int*, int>)(lpVtbl[3]))((IDxcSourceLocation*)Unsafe.AsPointer(ref this), other, pResult);
    }

    /// <include file='IDxcSourceLocation.xml' path='doc/member[@name="IDxcSourceLocation.GetSpellingLocation"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetSpellingLocation(IDxcFile** pFile, [NativeTypeName("unsigned int *")] uint* pLine, [NativeTypeName("unsigned int *")] uint* pCol, [NativeTypeName("unsigned int *")] uint* pOffset)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceLocation*, IDxcFile**, uint*, uint*, uint*, int>)(lpVtbl[4]))((IDxcSourceLocation*)Unsafe.AsPointer(ref this), pFile, pLine, pCol, pOffset);
    }

    /// <include file='IDxcSourceLocation.xml' path='doc/member[@name="IDxcSourceLocation.IsNull"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int IsNull([NativeTypeName("BOOL *")] int* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceLocation*, int*, int>)(lpVtbl[5]))((IDxcSourceLocation*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcSourceLocation.xml' path='doc/member[@name="IDxcSourceLocation.GetPresumedLocation"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetPresumedLocation([NativeTypeName("LPSTR *")] sbyte** pFilename, [NativeTypeName("unsigned int *")] uint* pLine, [NativeTypeName("unsigned int *")] uint* pCol)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceLocation*, sbyte**, uint*, uint*, int>)(lpVtbl[6]))((IDxcSourceLocation*)Unsafe.AsPointer(ref this), pFilename, pLine, pCol);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int IsEqualTo(IDxcSourceLocation* other, [NativeTypeName("BOOL *")] int* pResult);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetSpellingLocation(IDxcFile** pFile, [NativeTypeName("unsigned int *")] uint* pLine, [NativeTypeName("unsigned int *")] uint* pCol, [NativeTypeName("unsigned int *")] uint* pOffset);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int IsNull([NativeTypeName("BOOL *")] int* pResult);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetPresumedLocation([NativeTypeName("LPSTR *")] sbyte** pFilename, [NativeTypeName("unsigned int *")] uint* pLine, [NativeTypeName("unsigned int *")] uint* pCol);
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

        [NativeTypeName("HRESULT (IDxcSourceLocation *, BOOL *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcSourceLocation*, int*, int> IsEqualTo;

        [NativeTypeName("HRESULT (IDxcFile **, unsigned int *, unsigned int *, unsigned int *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcFile**, uint*, uint*, uint*, int> GetSpellingLocation;

        [NativeTypeName("HRESULT (BOOL *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, int*, int> IsNull;

        [NativeTypeName("HRESULT (LPSTR *, unsigned int *, unsigned int *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, sbyte**, uint*, uint*, int> GetPresumedLocation;
    }
}
