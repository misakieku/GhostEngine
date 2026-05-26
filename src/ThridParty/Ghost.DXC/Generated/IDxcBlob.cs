using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcBlob.xml' path='doc/member[@name="IDxcBlob"]/*' />
[Guid("8BA5FB08-5195-40E2-AC58-0D989C3A0102")]
[NativeTypeName("struct IDxcBlob : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcBlob : IDxcBlob.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcBlob);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlob*, Guid*, void**, int>)(lpVtbl[0]))((IDxcBlob*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlob*, uint>)(lpVtbl[1]))((IDxcBlob*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlob*, uint>)(lpVtbl[2]))((IDxcBlob*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcBlob.xml' path='doc/member[@name="IDxcBlob.GetBufferPointer"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("LPVOID")]
    public void* GetBufferPointer()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlob*, void*>)(lpVtbl[3]))((IDxcBlob*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcBlob.xml' path='doc/member[@name="IDxcBlob.GetBufferSize"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("SIZE_T")]
    public ulong GetBufferSize()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcBlob*, ulong>)(lpVtbl[4]))((IDxcBlob*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("LPVOID")]
        void* GetBufferPointer();

        [VtblIndex(4)]
        [return: NativeTypeName("SIZE_T")]
        ulong GetBufferSize();
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
    }
}
