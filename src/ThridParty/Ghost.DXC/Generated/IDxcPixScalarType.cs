using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPixScalarType.xml' path='doc/member[@name="IDxcPixScalarType"]/*' />
[Guid("246E1652-ED2A-4FFC-A949-43BF63750EE5")]
[NativeTypeName("struct IDxcPixScalarType : IDxcPixType")]
[NativeInheritance("IDxcPixType")]
public unsafe partial struct IDxcPixScalarType : IDxcPixScalarType.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPixScalarType);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixScalarType*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPixScalarType*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixScalarType*, uint>)(lpVtbl[1]))((IDxcPixScalarType*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixScalarType*, uint>)(lpVtbl[2]))((IDxcPixScalarType*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IDxcPixType.GetName" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetName([NativeTypeName("BSTR *")] char** Name)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixScalarType*, char**, int>)(lpVtbl[3]))((IDxcPixScalarType*)Unsafe.AsPointer(ref this), Name);
    }

    /// <inheritdoc cref="IDxcPixType.GetSizeInBits" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetSizeInBits([NativeTypeName("DWORD *")] uint* GetSizeInBits)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixScalarType*, uint*, int>)(lpVtbl[4]))((IDxcPixScalarType*)Unsafe.AsPointer(ref this), GetSizeInBits);
    }

    /// <inheritdoc cref="IDxcPixType.UnAlias" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int UnAlias(IDxcPixType** ppBaseType)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixScalarType*, IDxcPixType**, int>)(lpVtbl[5]))((IDxcPixScalarType*)Unsafe.AsPointer(ref this), ppBaseType);
    }

    public interface Interface : IDxcPixType.Interface
    {
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

        [NativeTypeName("HRESULT (DWORD *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetSizeInBits;

        [NativeTypeName("HRESULT (IDxcPixType **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcPixType**, int> UnAlias;
    }
}
