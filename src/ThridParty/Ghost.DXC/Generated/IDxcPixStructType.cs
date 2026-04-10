using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPixStructType.xml' path='doc/member[@name="IDxcPixStructType"]/*' />
[Guid("24C08C44-684B-4B1C-B41B-F8772383D074")]
[NativeTypeName("struct IDxcPixStructType : IDxcPixType")]
[NativeInheritance("IDxcPixType")]
public unsafe partial struct IDxcPixStructType : IDxcPixStructType.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPixStructType);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructType*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPixStructType*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructType*, uint>)(lpVtbl[1]))((IDxcPixStructType*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructType*, uint>)(lpVtbl[2]))((IDxcPixStructType*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IDxcPixType.GetName" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetName([NativeTypeName("BSTR *")] char** Name)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructType*, char**, int>)(lpVtbl[3]))((IDxcPixStructType*)Unsafe.AsPointer(ref this), Name);
    }

    /// <inheritdoc cref="IDxcPixType.GetSizeInBits" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetSizeInBits([NativeTypeName("DWORD *")] uint* GetSizeInBits)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructType*, uint*, int>)(lpVtbl[4]))((IDxcPixStructType*)Unsafe.AsPointer(ref this), GetSizeInBits);
    }

    /// <inheritdoc cref="IDxcPixType.UnAlias" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int UnAlias(IDxcPixType** ppBaseType)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructType*, IDxcPixType**, int>)(lpVtbl[5]))((IDxcPixStructType*)Unsafe.AsPointer(ref this), ppBaseType);
    }

    /// <include file='IDxcPixStructType.xml' path='doc/member[@name="IDxcPixStructType.GetNumFields"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetNumFields([NativeTypeName("DWORD *")] uint* ppNumFields)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructType*, uint*, int>)(lpVtbl[6]))((IDxcPixStructType*)Unsafe.AsPointer(ref this), ppNumFields);
    }

    /// <include file='IDxcPixStructType.xml' path='doc/member[@name="IDxcPixStructType.GetFieldByIndex"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetFieldByIndex([NativeTypeName("DWORD")] uint dwIndex, IDxcPixStructField** ppField)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructType*, uint, IDxcPixStructField**, int>)(lpVtbl[7]))((IDxcPixStructType*)Unsafe.AsPointer(ref this), dwIndex, ppField);
    }

    /// <include file='IDxcPixStructType.xml' path='doc/member[@name="IDxcPixStructType.GetFieldByName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int GetFieldByName([NativeTypeName("LPCWSTR")] char* lpName, IDxcPixStructField** ppField)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixStructType*, char*, IDxcPixStructField**, int>)(lpVtbl[8]))((IDxcPixStructType*)Unsafe.AsPointer(ref this), lpName, ppField);
    }

    public interface Interface : IDxcPixType.Interface
    {
        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetNumFields([NativeTypeName("DWORD *")] uint* ppNumFields);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int GetFieldByIndex([NativeTypeName("DWORD")] uint dwIndex, IDxcPixStructField** ppField);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int GetFieldByName([NativeTypeName("LPCWSTR")] char* lpName, IDxcPixStructField** ppField);
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

        [NativeTypeName("HRESULT (DWORD *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetNumFields;

        [NativeTypeName("HRESULT (DWORD, IDxcPixStructField **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcPixStructField**, int> GetFieldByIndex;

        [NativeTypeName("HRESULT (LPCWSTR, IDxcPixStructField **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char*, IDxcPixStructField**, int> GetFieldByName;
    }
}
