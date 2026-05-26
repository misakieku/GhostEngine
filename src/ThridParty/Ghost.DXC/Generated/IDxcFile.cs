using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcFile.xml' path='doc/member[@name="IDxcFile"]/*' />
[Guid("BB2FCA9E-1478-47BA-B08C-2C502ADA4895")]
[NativeTypeName("struct IDxcFile : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcFile : IDxcFile.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcFile);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcFile*, Guid*, void**, int>)(lpVtbl[0]))((IDxcFile*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcFile*, uint>)(lpVtbl[1]))((IDxcFile*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcFile*, uint>)(lpVtbl[2]))((IDxcFile*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcFile.xml' path='doc/member[@name="IDxcFile.GetName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetName([NativeTypeName("LPSTR *")] sbyte** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcFile*, sbyte**, int>)(lpVtbl[3]))((IDxcFile*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcFile.xml' path='doc/member[@name="IDxcFile.IsEqualTo"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int IsEqualTo(IDxcFile* other, [NativeTypeName("BOOL *")] int* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcFile*, IDxcFile*, int*, int>)(lpVtbl[4]))((IDxcFile*)Unsafe.AsPointer(ref this), other, pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetName([NativeTypeName("LPSTR *")] sbyte** pResult);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int IsEqualTo(IDxcFile* other, [NativeTypeName("BOOL *")] int* pResult);
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
        public delegate* unmanaged[MemberFunction]<TSelf*, sbyte**, int> GetName;

        [NativeTypeName("HRESULT (IDxcFile *, BOOL *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcFile*, int*, int> IsEqualTo;
    }
}
