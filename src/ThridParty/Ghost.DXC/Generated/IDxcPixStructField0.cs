using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPixStructField0.xml' path='doc/member[@name="IDxcPixStructField0"]/*' />
[Guid("6C707D08-7995-4A84-BAE5-E6D8291F3B78")]
[NativeTypeName("struct IDxcPixStructField0 : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcPixStructField0 : IDxcPixStructField0.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPixStructField0);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructField0*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPixStructField0*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructField0*, uint>)(lpVtbl[1]))((IDxcPixStructField0*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructField0*, uint>)(lpVtbl[2]))((IDxcPixStructField0*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPixStructField0.xml' path='doc/member[@name="IDxcPixStructField0.GetName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetName([NativeTypeName("BSTR *")] char** Name)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructField0*, char**, int>)(lpVtbl[3]))((IDxcPixStructField0*)Unsafe.AsPointer(ref this), Name);
    }

    /// <include file='IDxcPixStructField0.xml' path='doc/member[@name="IDxcPixStructField0.GetType"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetType(IDxcPixType** ppType)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructField0*, IDxcPixType**, int>)(lpVtbl[4]))((IDxcPixStructField0*)Unsafe.AsPointer(ref this), ppType);
    }

    /// <include file='IDxcPixStructField0.xml' path='doc/member[@name="IDxcPixStructField0.GetOffsetInBits"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetOffsetInBits([NativeTypeName("DWORD *")] uint* pOffsetInBits)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructField0*, uint*, int>)(lpVtbl[5]))((IDxcPixStructField0*)Unsafe.AsPointer(ref this), pOffsetInBits);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetName([NativeTypeName("BSTR *")] char** Name);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetType(IDxcPixType** ppType);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetOffsetInBits([NativeTypeName("DWORD *")] uint* pOffsetInBits);
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

        [NativeTypeName("HRESULT (BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char**, int> GetName;

        [NativeTypeName("HRESULT (IDxcPixType **) __attribute__((stdcall))")]
        public new delegate* unmanaged[MemberFunction]<TSelf*, IDxcPixType**, int> GetType;

        [NativeTypeName("HRESULT (DWORD *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetOffsetInBits;
    }
}
