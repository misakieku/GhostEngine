using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPixType.xml' path='doc/member[@name="IDxcPixType"]/*' />
[Guid("199D8C13-D312-4197-A2C1-07A532999727")]
[NativeTypeName("struct IDxcPixType : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcPixType : IDxcPixType.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPixType);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixType*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPixType*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixType*, uint>)(lpVtbl[1]))((IDxcPixType*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixType*, uint>)(lpVtbl[2]))((IDxcPixType*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPixType.xml' path='doc/member[@name="IDxcPixType.GetName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetName([NativeTypeName("BSTR *")] char** Name)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixType*, char**, int>)(lpVtbl[3]))((IDxcPixType*)Unsafe.AsPointer(ref this), Name);
    }

    /// <include file='IDxcPixType.xml' path='doc/member[@name="IDxcPixType.GetSizeInBits"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetSizeInBits([NativeTypeName("DWORD *")] uint* GetSizeInBits)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixType*, uint*, int>)(lpVtbl[4]))((IDxcPixType*)Unsafe.AsPointer(ref this), GetSizeInBits);
    }

    /// <include file='IDxcPixType.xml' path='doc/member[@name="IDxcPixType.UnAlias"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int UnAlias(IDxcPixType** ppBaseType)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixType*, IDxcPixType**, int>)(lpVtbl[5]))((IDxcPixType*)Unsafe.AsPointer(ref this), ppBaseType);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetName([NativeTypeName("BSTR *")] char** Name);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetSizeInBits([NativeTypeName("DWORD *")] uint* GetSizeInBits);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int UnAlias(IDxcPixType** ppBaseType);
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
