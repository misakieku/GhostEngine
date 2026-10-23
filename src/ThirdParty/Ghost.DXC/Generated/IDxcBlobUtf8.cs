using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcBlobUtf8.xml' path='doc/member[@name="IDxcBlobUtf8"]/*' />
[Guid("3DA636C9-BA71-4024-A301-30CBF125305B")]
[NativeTypeName("struct IDxcBlobUtf8 : IDxcBlobEncoding")]
[NativeInheritance("IDxcBlobEncoding")]
public unsafe partial struct IDxcBlobUtf8 : IDxcBlobUtf8.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcBlobUtf8);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobUtf8*, Guid*, void**, int>)(lpVtbl[0]))((IDxcBlobUtf8*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobUtf8*, uint>)(lpVtbl[1]))((IDxcBlobUtf8*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobUtf8*, uint>)(lpVtbl[2]))((IDxcBlobUtf8*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IDxcBlob.GetBufferPointer" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("LPVOID")]
    public void* GetBufferPointer()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobUtf8*, void*>)(lpVtbl[3]))((IDxcBlobUtf8*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IDxcBlob.GetBufferSize" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("SIZE_T")]
    public ulong GetBufferSize()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobUtf8*, ulong>)(lpVtbl[4]))((IDxcBlobUtf8*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IDxcBlobEncoding.GetEncoding" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetEncoding([NativeTypeName("BOOL *")] int* pKnown, [NativeTypeName("UINT32 *")] uint* pCodePage)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobUtf8*, int*, uint*, int>)(lpVtbl[5]))((IDxcBlobUtf8*)Unsafe.AsPointer(ref this), pKnown, pCodePage);
    }

    /// <include file='IDxcBlobUtf8.xml' path='doc/member[@name="IDxcBlobUtf8.GetStringPointer"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("LPCSTR")]
    public sbyte* GetStringPointer()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobUtf8*, sbyte*>)(lpVtbl[6]))((IDxcBlobUtf8*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcBlobUtf8.xml' path='doc/member[@name="IDxcBlobUtf8.GetStringLength"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("SIZE_T")]
    public ulong GetStringLength()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlobUtf8*, ulong>)(lpVtbl[7]))((IDxcBlobUtf8*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IDxcBlobEncoding.Interface
    {
        [VtblIndex(6)]
        [return: NativeTypeName("LPCSTR")]
        sbyte* GetStringPointer();

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

        [NativeTypeName("LPCSTR () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, sbyte*> GetStringPointer;

        [NativeTypeName("SIZE_T () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, ulong> GetStringLength;
    }
}
