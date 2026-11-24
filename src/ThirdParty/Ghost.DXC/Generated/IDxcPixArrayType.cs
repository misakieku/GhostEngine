using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPixArrayType.xml' path='doc/member[@name="IDxcPixArrayType"]/*' />
[Guid("9BA0D9D3-457B-426F-8019-9F3849982AA2")]
[NativeTypeName("struct IDxcPixArrayType : IDxcPixType")]
[NativeInheritance("IDxcPixType")]
public unsafe partial struct IDxcPixArrayType : IDxcPixArrayType.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPixArrayType);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
        public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixArrayType*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPixArrayType*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixArrayType*, uint>)(lpVtbl[1]))((IDxcPixArrayType*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixArrayType*, uint>)(lpVtbl[2]))((IDxcPixArrayType*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IDxcPixType.GetName" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
        public HRESULT GetName([NativeTypeName("BSTR *")] char** Name)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixArrayType*, char**, int>)(lpVtbl[3]))((IDxcPixArrayType*)Unsafe.AsPointer(ref this), Name);
    }

    /// <inheritdoc cref="IDxcPixType.GetSizeInBits" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
        public HRESULT GetSizeInBits([NativeTypeName("DWORD *")] uint* GetSizeInBits)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixArrayType*, uint*, int>)(lpVtbl[4]))((IDxcPixArrayType*)Unsafe.AsPointer(ref this), GetSizeInBits);
    }

    /// <inheritdoc cref="IDxcPixType.UnAlias" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
        public HRESULT UnAlias(IDxcPixType** ppBaseType)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixArrayType*, IDxcPixType**, int>)(lpVtbl[5]))((IDxcPixArrayType*)Unsafe.AsPointer(ref this), ppBaseType);
    }

    /// <include file='IDxcPixArrayType.xml' path='doc/member[@name="IDxcPixArrayType.GetNumElements"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
        public HRESULT GetNumElements([NativeTypeName("DWORD *")] uint* ppNumElements)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixArrayType*, uint*, int>)(lpVtbl[6]))((IDxcPixArrayType*)Unsafe.AsPointer(ref this), ppNumElements);
    }

    /// <include file='IDxcPixArrayType.xml' path='doc/member[@name="IDxcPixArrayType.GetIndexedType"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
        public HRESULT GetIndexedType(IDxcPixType** ppElementType)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixArrayType*, IDxcPixType**, int>)(lpVtbl[7]))((IDxcPixArrayType*)Unsafe.AsPointer(ref this), ppElementType);
    }

    /// <include file='IDxcPixArrayType.xml' path='doc/member[@name="IDxcPixArrayType.GetElementType"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
        public HRESULT GetElementType(IDxcPixType** ppElementType)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixArrayType*, IDxcPixType**, int>)(lpVtbl[8]))((IDxcPixArrayType*)Unsafe.AsPointer(ref this), ppElementType);
    }

    public interface Interface : IDxcPixType.Interface
    {
        [VtblIndex(6)]
                HRESULT GetNumElements([NativeTypeName("DWORD *")] uint* ppNumElements);

        [VtblIndex(7)]
                HRESULT GetIndexedType(IDxcPixType** ppElementType);

        [VtblIndex(8)]
                HRESULT GetElementType(IDxcPixType** ppElementType);
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
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetNumElements;

        [NativeTypeName("HRESULT (IDxcPixType **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcPixType**, int> GetIndexedType;

        [NativeTypeName("HRESULT (IDxcPixType **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcPixType**, int> GetElementType;
    }
}
