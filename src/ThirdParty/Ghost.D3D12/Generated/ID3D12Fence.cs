using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("0A753DCF-C4D8-4B91-ADF6-BE5A60D95A76")]
[NativeTypeName("struct ID3D12Fence : ID3D12Pageable")]
public unsafe partial struct ID3D12Fence : ID3D12Fence.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12Fence);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Fence*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12Fence*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Fence*, uint>)(lpVtbl[1]))((ID3D12Fence*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Fence*, uint>)(lpVtbl[2]))((ID3D12Fence*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Fence*, Guid*, uint*, void*, int>)(lpVtbl[3]))((ID3D12Fence*)Unsafe.AsPointer(ref this), guid, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT SetPrivateData([NativeTypeName("const GUID &")] Guid* guid, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Fence*, Guid*, uint, void*, int>)(lpVtbl[4]))((ID3D12Fence*)Unsafe.AsPointer(ref this), guid, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* guid, [NativeTypeName("const IUnknown *")] IUnknown* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Fence*, Guid*, IUnknown*, int>)(lpVtbl[5]))((ID3D12Fence*)Unsafe.AsPointer(ref this), guid, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT SetName([NativeTypeName("LPCWSTR")] char* Name)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Fence*, char*, int>)(lpVtbl[6]))((ID3D12Fence*)Unsafe.AsPointer(ref this), Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public HRESULT GetDevice([NativeTypeName("const IID &")] Guid* riid, void** ppvDevice)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Fence*, Guid*, void**, int>)(lpVtbl[7]))((ID3D12Fence*)Unsafe.AsPointer(ref this), riid, ppvDevice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("UINT64")]
    public ulong GetCompletedValue()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Fence*, ulong>)(lpVtbl[8]))((ID3D12Fence*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public HRESULT SetEventOnCompletion([NativeTypeName("UINT64")] ulong Value, [NativeTypeName("HANDLE")] void* hEvent)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Fence*, ulong, void*, int>)(lpVtbl[9]))((ID3D12Fence*)Unsafe.AsPointer(ref this), Value, hEvent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public HRESULT Signal([NativeTypeName("UINT64")] ulong Value)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12Fence*, ulong, int>)(lpVtbl[10]))((ID3D12Fence*)Unsafe.AsPointer(ref this), Value);
    }

    public interface Interface : ID3D12Pageable.Interface
    {
        [VtblIndex(8)]
        [return: NativeTypeName("UINT64")]
        ulong GetCompletedValue();

        [VtblIndex(9)]
        HRESULT SetEventOnCompletion([NativeTypeName("UINT64")] ulong Value, [NativeTypeName("HANDLE")] void* hEvent);

        [VtblIndex(10)]
        HRESULT Signal([NativeTypeName("UINT64")] ulong Value);
    }
}
