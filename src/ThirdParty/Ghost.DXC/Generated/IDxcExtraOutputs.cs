using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcExtraOutputs.xml' path='doc/member[@name="IDxcExtraOutputs"]/*' />
[Guid("319B37A2-A5C2-494A-A5DE-4801B2FAF989")]
[NativeTypeName("struct IDxcExtraOutputs : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcExtraOutputs : IDxcExtraOutputs.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcExtraOutputs);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcExtraOutputs*, Guid*, void**, int>)(lpVtbl[0]))((IDxcExtraOutputs*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcExtraOutputs*, uint>)(lpVtbl[1]))((IDxcExtraOutputs*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcExtraOutputs*, uint>)(lpVtbl[2]))((IDxcExtraOutputs*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcExtraOutputs.xml' path='doc/member[@name="IDxcExtraOutputs.GetOutputCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("UINT32")]
    public uint GetOutputCount()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcExtraOutputs*, uint>)(lpVtbl[3]))((IDxcExtraOutputs*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcExtraOutputs.xml' path='doc/member[@name="IDxcExtraOutputs.GetOutput"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetOutput([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("const IID &")] Guid* iid, void** ppvObject, IDxcBlobWide** ppOutputType, IDxcBlobWide** ppOutputName)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcExtraOutputs*, uint, Guid*, void**, IDxcBlobWide**, IDxcBlobWide**, int>)(lpVtbl[4]))((IDxcExtraOutputs*)Unsafe.AsPointer(ref this), uIndex, iid, ppvObject, ppOutputType, ppOutputName);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("UINT32")]
        uint GetOutputCount();

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetOutput([NativeTypeName("UINT32")] uint uIndex, [NativeTypeName("const IID &")] Guid* iid, void** ppvObject, IDxcBlobWide** ppOutputType, IDxcBlobWide** ppOutputName);
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

        [NativeTypeName("UINT32 () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> GetOutputCount;

        [NativeTypeName("HRESULT (UINT32, const IID &, void **, IDxcBlobWide **, IDxcBlobWide **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, Guid*, void**, IDxcBlobWide**, IDxcBlobWide**, int> GetOutput;
    }
}
