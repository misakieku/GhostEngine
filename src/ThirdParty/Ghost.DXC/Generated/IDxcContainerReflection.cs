using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcContainerReflection.xml' path='doc/member[@name="IDxcContainerReflection"]/*' />
[Guid("D2C21B26-8350-4BDC-976A-331CE6F4C54C")]
[NativeTypeName("struct IDxcContainerReflection : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcContainerReflection : IDxcContainerReflection.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcContainerReflection);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcContainerReflection*, Guid*, void**, int>)(lpVtbl[0]))((IDxcContainerReflection*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcContainerReflection*, uint>)(lpVtbl[1]))((IDxcContainerReflection*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcContainerReflection*, uint>)(lpVtbl[2]))((IDxcContainerReflection*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcContainerReflection.xml' path='doc/member[@name="IDxcContainerReflection.Load"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int Load(IDxcBlob* pContainer)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcContainerReflection*, IDxcBlob*, int>)(lpVtbl[3]))((IDxcContainerReflection*)Unsafe.AsPointer(ref this), pContainer);
    }

    /// <include file='IDxcContainerReflection.xml' path='doc/member[@name="IDxcContainerReflection.GetPartCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetPartCount([NativeTypeName("UINT32 *")] uint* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcContainerReflection*, uint*, int>)(lpVtbl[4]))((IDxcContainerReflection*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcContainerReflection.xml' path='doc/member[@name="IDxcContainerReflection.GetPartKind"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetPartKind([NativeTypeName("UINT32")] uint idx, [NativeTypeName("UINT32 *")] uint* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcContainerReflection*, uint, uint*, int>)(lpVtbl[5]))((IDxcContainerReflection*)Unsafe.AsPointer(ref this), idx, pResult);
    }

    /// <include file='IDxcContainerReflection.xml' path='doc/member[@name="IDxcContainerReflection.GetPartContent"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetPartContent([NativeTypeName("UINT32")] uint idx, IDxcBlob** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcContainerReflection*, uint, IDxcBlob**, int>)(lpVtbl[6]))((IDxcContainerReflection*)Unsafe.AsPointer(ref this), idx, ppResult);
    }

    /// <include file='IDxcContainerReflection.xml' path='doc/member[@name="IDxcContainerReflection.FindFirstPartKind"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int FindFirstPartKind([NativeTypeName("UINT32")] uint kind, [NativeTypeName("UINT32 *")] uint* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcContainerReflection*, uint, uint*, int>)(lpVtbl[7]))((IDxcContainerReflection*)Unsafe.AsPointer(ref this), kind, pResult);
    }

    /// <include file='IDxcContainerReflection.xml' path='doc/member[@name="IDxcContainerReflection.GetPartReflection"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int GetPartReflection([NativeTypeName("UINT32")] uint idx, [NativeTypeName("const IID &")] Guid* iid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcContainerReflection*, uint, Guid*, void**, int>)(lpVtbl[8]))((IDxcContainerReflection*)Unsafe.AsPointer(ref this), idx, iid, ppvObject);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int Load(IDxcBlob* pContainer);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetPartCount([NativeTypeName("UINT32 *")] uint* pResult);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetPartKind([NativeTypeName("UINT32")] uint idx, [NativeTypeName("UINT32 *")] uint* pResult);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetPartContent([NativeTypeName("UINT32")] uint idx, IDxcBlob** ppResult);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int FindFirstPartKind([NativeTypeName("UINT32")] uint kind, [NativeTypeName("UINT32 *")] uint* pResult);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int GetPartReflection([NativeTypeName("UINT32")] uint idx, [NativeTypeName("const IID &")] Guid* iid, void** ppvObject);
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

        [NativeTypeName("HRESULT (IDxcBlob *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob*, int> Load;

        [NativeTypeName("HRESULT (UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetPartCount;

        [NativeTypeName("HRESULT (UINT32, UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, uint*, int> GetPartKind;

        [NativeTypeName("HRESULT (UINT32, IDxcBlob **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcBlob**, int> GetPartContent;

        [NativeTypeName("HRESULT (UINT32, UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, uint*, int> FindFirstPartKind;

        [NativeTypeName("HRESULT (UINT32, const IID &, void **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, Guid*, void**, int> GetPartReflection;
    }
}
