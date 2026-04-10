using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcBlobWide.xml' path='doc/member[@name="IDxcBlobWide"]/*' />
[Guid("A3F84EAB-0FAA-497E-A39C-EE6ED60B2D84")]
[NativeTypeName("struct IDxcBlobWide : IDxcBlobEncoding")]
[NativeInheritance("IDxcBlobEncoding")]
public unsafe partial struct IDxcBlobWide : IDxcBlobWide.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcBlobWide);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobWide*, Guid*, void**, int>)(lpVtbl[0]))((IDxcBlobWide*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobWide*, uint>)(lpVtbl[1]))((IDxcBlobWide*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobWide*, uint>)(lpVtbl[2]))((IDxcBlobWide*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IDxcBlob.GetBufferPointer" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("LPVOID")]
    public void* GetBufferPointer()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobWide*, void*>)(lpVtbl[3]))((IDxcBlobWide*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IDxcBlob.GetBufferSize" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("SIZE_T")]
    public ulong GetBufferSize()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobWide*, ulong>)(lpVtbl[4]))((IDxcBlobWide*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IDxcBlobEncoding.GetEncoding" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetEncoding([NativeTypeName("BOOL *")] int* pKnown, [NativeTypeName("UINT32 *")] uint* pCodePage)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobWide*, int*, uint*, int>)(lpVtbl[5]))((IDxcBlobWide*)Unsafe.AsPointer(ref this), pKnown, pCodePage);
    }

    /// <include file='IDxcBlobWide.xml' path='doc/member[@name="IDxcBlobWide.GetStringPointer"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("LPCWSTR")]
    public char* GetStringPointer()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobWide*, char*>)(lpVtbl[6]))((IDxcBlobWide*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcBlobWide.xml' path='doc/member[@name="IDxcBlobWide.GetStringLength"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("SIZE_T")]
    public ulong GetStringLength()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobWide*, ulong>)(lpVtbl[7]))((IDxcBlobWide*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IDxcBlobEncoding.Interface
    {
        [VtblIndex(6)]
        [return: NativeTypeName("LPCWSTR")]
        char* GetStringPointer();

        [VtblIndex(7)]
        [return: NativeTypeName("SIZE_T")]
        ulong GetStringLength();
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

        [NativeTypeName("LPVOID () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void*> GetBufferPointer;

        [NativeTypeName("SIZE_T () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, ulong> GetBufferSize;

        [NativeTypeName("HRESULT (BOOL *, UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, int*, uint*, int> GetEncoding;

        [NativeTypeName("LPCWSTR () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char*> GetStringPointer;

        [NativeTypeName("SIZE_T () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, ulong> GetStringLength;
    }
}
