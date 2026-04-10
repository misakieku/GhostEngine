using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPixVariable.xml' path='doc/member[@name="IDxcPixVariable"]/*' />
[Guid("2F954B30-61A7-4348-95B1-2DB356A75CDE")]
[NativeTypeName("struct IDxcPixVariable : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcPixVariable : IDxcPixVariable.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPixVariable);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixVariable*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPixVariable*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixVariable*, uint>)(lpVtbl[1]))((IDxcPixVariable*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixVariable*, uint>)(lpVtbl[2]))((IDxcPixVariable*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPixVariable.xml' path='doc/member[@name="IDxcPixVariable.GetName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetName([NativeTypeName("BSTR *")] char** Name)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixVariable*, char**, int>)(lpVtbl[3]))((IDxcPixVariable*)Unsafe.AsPointer(ref this), Name);
    }

    /// <include file='IDxcPixVariable.xml' path='doc/member[@name="IDxcPixVariable.GetType"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetType(IDxcPixType** ppType)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixVariable*, IDxcPixType**, int>)(lpVtbl[4]))((IDxcPixVariable*)Unsafe.AsPointer(ref this), ppType);
    }

    /// <include file='IDxcPixVariable.xml' path='doc/member[@name="IDxcPixVariable.GetStorage"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetStorage(IDxcPixDxilStorage** ppStorage)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixVariable*, IDxcPixDxilStorage**, int>)(lpVtbl[5]))((IDxcPixVariable*)Unsafe.AsPointer(ref this), ppStorage);
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
        int GetStorage(IDxcPixDxilStorage** ppStorage);
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

        [NativeTypeName("HRESULT (IDxcPixDxilStorage **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcPixDxilStorage**, int> GetStorage;
    }
}
