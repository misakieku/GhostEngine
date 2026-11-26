using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("0ADF7D52-929C-4E61-ADDB-FFED30DE66EF")]
[NativeTypeName("struct ID3D12SharingContract : IUnknown")]
public unsafe partial struct ID3D12SharingContract : ID3D12SharingContract.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12SharingContract);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SharingContract*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12SharingContract*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SharingContract*, uint>)(lpVtbl[1]))((ID3D12SharingContract*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12SharingContract*, uint>)(lpVtbl[2]))((ID3D12SharingContract*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void Present(ID3D12Resource* pResource, uint Subresource, HWND window)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12SharingContract*, ID3D12Resource*, uint, HWND, void>)(lpVtbl[3]))((ID3D12SharingContract*)Unsafe.AsPointer(ref this), pResource, Subresource, window);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public void SharedFenceSignal(ID3D12Fence* pFence, [NativeTypeName("UINT64")] ulong FenceValue)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12SharingContract*, ID3D12Fence*, ulong, void>)(lpVtbl[4]))((ID3D12SharingContract*)Unsafe.AsPointer(ref this), pFence, FenceValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public void BeginCapturableWork([NativeTypeName("const GUID &")] Guid* guid)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12SharingContract*, Guid*, void>)(lpVtbl[5]))((ID3D12SharingContract*)Unsafe.AsPointer(ref this), guid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public void EndCapturableWork([NativeTypeName("const GUID &")] Guid* guid)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12SharingContract*, Guid*, void>)(lpVtbl[6]))((ID3D12SharingContract*)Unsafe.AsPointer(ref this), guid);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        void Present(ID3D12Resource* pResource, uint Subresource, HWND window);

        [VtblIndex(4)]
        void SharedFenceSignal(ID3D12Fence* pFence, [NativeTypeName("UINT64")] ulong FenceValue);

        [VtblIndex(5)]
        void BeginCapturableWork([NativeTypeName("const GUID &")] Guid* guid);

        [VtblIndex(6)]
        void EndCapturableWork([NativeTypeName("const GUID &")] Guid* guid);
    }
}
