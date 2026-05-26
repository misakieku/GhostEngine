using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPixStructField.xml' path='doc/member[@name="IDxcPixStructField"]/*' />
[Guid("DE45597C-5869-4F97-A77B-D6650B9A16CF")]
[NativeTypeName("struct IDxcPixStructField : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcPixStructField : IDxcPixStructField.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPixStructField);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructField*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPixStructField*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructField*, uint>)(lpVtbl[1]))((IDxcPixStructField*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructField*, uint>)(lpVtbl[2]))((IDxcPixStructField*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPixStructField.xml' path='doc/member[@name="IDxcPixStructField.GetName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetName([NativeTypeName("BSTR *")] char** Name)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructField*, char**, int>)(lpVtbl[3]))((IDxcPixStructField*)Unsafe.AsPointer(ref this), Name);
    }

    /// <include file='IDxcPixStructField.xml' path='doc/member[@name="IDxcPixStructField.GetType"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetType(IDxcPixType** ppType)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructField*, IDxcPixType**, int>)(lpVtbl[4]))((IDxcPixStructField*)Unsafe.AsPointer(ref this), ppType);
    }

    /// <include file='IDxcPixStructField.xml' path='doc/member[@name="IDxcPixStructField.GetOffsetInBits"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetOffsetInBits([NativeTypeName("DWORD *")] uint* pOffsetInBits)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructField*, uint*, int>)(lpVtbl[5]))((IDxcPixStructField*)Unsafe.AsPointer(ref this), pOffsetInBits);
    }

    /// <include file='IDxcPixStructField.xml' path='doc/member[@name="IDxcPixStructField.GetFieldSizeInBits"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetFieldSizeInBits([NativeTypeName("DWORD *")] uint* pFieldSizeInBits)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructField*, uint*, int>)(lpVtbl[6]))((IDxcPixStructField*)Unsafe.AsPointer(ref this), pFieldSizeInBits);
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

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetFieldSizeInBits([NativeTypeName("DWORD *")] uint* pFieldSizeInBits);
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

        [NativeTypeName("HRESULT (DWORD *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetFieldSizeInBits;
    }
}
