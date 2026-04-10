using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPixDxilSourceLocations.xml' path='doc/member[@name="IDxcPixDxilSourceLocations"]/*' />
[Guid("761C833D-E7B8-4624-80F8-3A3FB4146342")]
[NativeTypeName("struct IDxcPixDxilSourceLocations : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcPixDxilSourceLocations : IDxcPixDxilSourceLocations.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPixDxilSourceLocations);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilSourceLocations*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPixDxilSourceLocations*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilSourceLocations*, uint>)(lpVtbl[1]))((IDxcPixDxilSourceLocations*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilSourceLocations*, uint>)(lpVtbl[2]))((IDxcPixDxilSourceLocations*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPixDxilSourceLocations.xml' path='doc/member[@name="IDxcPixDxilSourceLocations.GetCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("DWORD")]
    public uint GetCount()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilSourceLocations*, uint>)(lpVtbl[3]))((IDxcPixDxilSourceLocations*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPixDxilSourceLocations.xml' path='doc/member[@name="IDxcPixDxilSourceLocations.GetLineNumberByIndex"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("DWORD")]
    public uint GetLineNumberByIndex([NativeTypeName("DWORD")] uint Index)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilSourceLocations*, uint, uint>)(lpVtbl[4]))((IDxcPixDxilSourceLocations*)Unsafe.AsPointer(ref this), Index);
    }

    /// <include file='IDxcPixDxilSourceLocations.xml' path='doc/member[@name="IDxcPixDxilSourceLocations.GetColumnByIndex"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("DWORD")]
    public uint GetColumnByIndex([NativeTypeName("DWORD")] uint Index)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilSourceLocations*, uint, uint>)(lpVtbl[5]))((IDxcPixDxilSourceLocations*)Unsafe.AsPointer(ref this), Index);
    }

    /// <include file='IDxcPixDxilSourceLocations.xml' path='doc/member[@name="IDxcPixDxilSourceLocations.GetFileNameByIndex"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetFileNameByIndex([NativeTypeName("DWORD")] uint Index, [NativeTypeName("BSTR *")] char** Name)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilSourceLocations*, uint, char**, int>)(lpVtbl[6]))((IDxcPixDxilSourceLocations*)Unsafe.AsPointer(ref this), Index, Name);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("DWORD")]
        uint GetCount();

        [VtblIndex(4)]
        [return: NativeTypeName("DWORD")]
        uint GetLineNumberByIndex([NativeTypeName("DWORD")] uint Index);

        [VtblIndex(5)]
        [return: NativeTypeName("DWORD")]
        uint GetColumnByIndex([NativeTypeName("DWORD")] uint Index);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetFileNameByIndex([NativeTypeName("DWORD")] uint Index, [NativeTypeName("BSTR *")] char** Name);
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

        [NativeTypeName("DWORD () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> GetCount;

        [NativeTypeName("DWORD (DWORD) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, uint> GetLineNumberByIndex;

        [NativeTypeName("DWORD (DWORD) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, uint> GetColumnByIndex;

        [NativeTypeName("HRESULT (DWORD, BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, char**, int> GetFileNameByIndex;
    }
}
