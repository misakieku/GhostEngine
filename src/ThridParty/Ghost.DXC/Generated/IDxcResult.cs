using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcResult.xml' path='doc/member[@name="IDxcResult"]/*' />
[Guid("58346CDA-DDE7-4497-9461-6F87AF5E0659")]
[NativeTypeName("struct IDxcResult : IDxcOperationResult")]
[NativeInheritance("IDxcOperationResult")]
public unsafe partial struct IDxcResult : IDxcResult.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcResult);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcResult*, Guid*, void**, int>)(lpVtbl[0]))((IDxcResult*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcResult*, uint>)(lpVtbl[1]))((IDxcResult*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcResult*, uint>)(lpVtbl[2]))((IDxcResult*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IDxcOperationResult.GetStatus" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetStatus([NativeTypeName("HRESULT *")] int* pStatus)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcResult*, int*, int>)(lpVtbl[3]))((IDxcResult*)Unsafe.AsPointer(ref this), pStatus);
    }

    /// <inheritdoc cref="IDxcOperationResult.GetResult" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetResult(IDxcBlob** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcResult*, IDxcBlob**, int>)(lpVtbl[4]))((IDxcResult*)Unsafe.AsPointer(ref this), ppResult);
    }

    /// <inheritdoc cref="IDxcOperationResult.GetErrorBuffer" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetErrorBuffer(IDxcBlobEncoding** ppErrors)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcResult*, IDxcBlobEncoding**, int>)(lpVtbl[5]))((IDxcResult*)Unsafe.AsPointer(ref this), ppErrors);
    }

    /// <include file='IDxcResult.xml' path='doc/member[@name="IDxcResult.HasOutput"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("BOOL")]
    public int HasOutput(DXC_OUT_KIND dxcOutKind)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcResult*, DXC_OUT_KIND, int>)(lpVtbl[6]))((IDxcResult*)Unsafe.AsPointer(ref this), dxcOutKind);
    }

    /// <include file='IDxcResult.xml' path='doc/member[@name="IDxcResult.GetOutput"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetOutput(DXC_OUT_KIND dxcOutKind, [NativeTypeName("const IID &")] Guid* iid, void** ppvObject, IDxcBlobWide** ppOutputName)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcResult*, DXC_OUT_KIND, Guid*, void**, IDxcBlobWide**, int>)(lpVtbl[7]))((IDxcResult*)Unsafe.AsPointer(ref this), dxcOutKind, iid, ppvObject, ppOutputName);
    }

    /// <include file='IDxcResult.xml' path='doc/member[@name="IDxcResult.GetNumOutputs"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("UINT32")]
    public uint GetNumOutputs()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcResult*, uint>)(lpVtbl[8]))((IDxcResult*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcResult.xml' path='doc/member[@name="IDxcResult.GetOutputByIndex"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public DXC_OUT_KIND GetOutputByIndex([NativeTypeName("UINT32")] uint Index)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcResult*, uint, DXC_OUT_KIND>)(lpVtbl[9]))((IDxcResult*)Unsafe.AsPointer(ref this), Index);
    }

    /// <include file='IDxcResult.xml' path='doc/member[@name="IDxcResult.PrimaryOutput"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public DXC_OUT_KIND PrimaryOutput()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcResult*, DXC_OUT_KIND>)(lpVtbl[10]))((IDxcResult*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IDxcOperationResult.Interface
    {
        [VtblIndex(6)]
        [return: NativeTypeName("BOOL")]
        int HasOutput(DXC_OUT_KIND dxcOutKind);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int GetOutput(DXC_OUT_KIND dxcOutKind, [NativeTypeName("const IID &")] Guid* iid, void** ppvObject, IDxcBlobWide** ppOutputName);

        [VtblIndex(8)]
        [return: NativeTypeName("UINT32")]
        uint GetNumOutputs();

        [VtblIndex(9)]
        DXC_OUT_KIND GetOutputByIndex([NativeTypeName("UINT32")] uint Index);

        [VtblIndex(10)]
        DXC_OUT_KIND PrimaryOutput();
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

        [NativeTypeName("HRESULT (HRESULT *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, int*, int> GetStatus;

        [NativeTypeName("HRESULT (IDxcBlob **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlob**, int> GetResult;

        [NativeTypeName("HRESULT (IDxcBlobEncoding **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcBlobEncoding**, int> GetErrorBuffer;

        [NativeTypeName("BOOL (DXC_OUT_KIND) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DXC_OUT_KIND, int> HasOutput;

        [NativeTypeName("HRESULT (DXC_OUT_KIND, const IID &, void **, IDxcBlobWide **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DXC_OUT_KIND, Guid*, void**, IDxcBlobWide**, int> GetOutput;

        [NativeTypeName("UINT32 ()")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> GetNumOutputs;

        [NativeTypeName("DXC_OUT_KIND (UINT32)")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, DXC_OUT_KIND> GetOutputByIndex;

        [NativeTypeName("DXC_OUT_KIND ()")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DXC_OUT_KIND> PrimaryOutput;
    }
}
