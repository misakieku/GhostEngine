using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXGI.DXGI;

namespace Ghost.DXGI;

[Guid("310D36A0-D2E7-4C0A-AA04-6A9D23B8886A")]
[NativeTypeName("struct IDXGISwapChain : IDXGIDeviceSubObject")]
public unsafe partial struct IDXGISwapChain : IDXGISwapChain.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDXGISwapChain);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, Guid*, void**, int>)(lpVtbl[0]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, uint>)(lpVtbl[1]))((IDXGISwapChain*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, uint>)(lpVtbl[2]))((IDXGISwapChain*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, Guid*, uint, void*, int>)(lpVtbl[3]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), Name, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* Name, [NativeTypeName("const IUnknown *")] IUnknown* pUnknown)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, Guid*, IUnknown*, int>)(lpVtbl[4]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), Name, pUnknown);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, Guid*, uint*, void*, int>)(lpVtbl[5]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), Name, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetParent([NativeTypeName("const IID &")] Guid* riid, void** ppParent)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, Guid*, void**, int>)(lpVtbl[6]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), riid, ppParent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetDevice([NativeTypeName("const IID &")] Guid* riid, void** ppDevice)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, Guid*, void**, int>)(lpVtbl[7]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), riid, ppDevice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int Present(uint SyncInterval, uint Flags)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, uint, uint, int>)(lpVtbl[8]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), SyncInterval, Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int GetBuffer(uint Buffer, [NativeTypeName("const IID &")] Guid* riid, void** ppSurface)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, uint, Guid*, void**, int>)(lpVtbl[9]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), Buffer, riid, ppSurface);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int SetFullscreenState([NativeTypeName("BOOL")] int Fullscreen, IDXGIOutput* pTarget)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, int, IDXGIOutput*, int>)(lpVtbl[10]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), Fullscreen, pTarget);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("HRESULT")]
    public int GetFullscreenState([NativeTypeName("BOOL *")] int* pFullscreen, IDXGIOutput** ppTarget)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, int*, IDXGIOutput**, int>)(lpVtbl[11]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), pFullscreen, ppTarget);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    [return: NativeTypeName("HRESULT")]
    public int GetDesc(DXGI_SWAP_CHAIN_DESC* pDesc)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, DXGI_SWAP_CHAIN_DESC*, int>)(lpVtbl[12]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), pDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    [return: NativeTypeName("HRESULT")]
    public int ResizeBuffers(uint BufferCount, uint Width, uint Height, DXGI_FORMAT NewFormat, uint SwapChainFlags)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, uint, uint, uint, DXGI_FORMAT, uint, int>)(lpVtbl[13]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), BufferCount, Width, Height, NewFormat, SwapChainFlags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    [return: NativeTypeName("HRESULT")]
    public int ResizeTarget([NativeTypeName("const DXGI_MODE_DESC *")] DXGI_MODE_DESC* pNewTargetParameters)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, DXGI_MODE_DESC*, int>)(lpVtbl[14]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), pNewTargetParameters);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    [return: NativeTypeName("HRESULT")]
    public int GetContainingOutput(IDXGIOutput** ppOutput)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, IDXGIOutput**, int>)(lpVtbl[15]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), ppOutput);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    [return: NativeTypeName("HRESULT")]
    public int GetFrameStatistics(DXGI_FRAME_STATISTICS* pStats)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, DXGI_FRAME_STATISTICS*, int>)(lpVtbl[16]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), pStats);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    [return: NativeTypeName("HRESULT")]
    public int GetLastPresentCount(uint* pLastPresentCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain*, uint*, int>)(lpVtbl[17]))((IDXGISwapChain*)Unsafe.AsPointer(ref this), pLastPresentCount);
    }

    public interface Interface : IDXGIDeviceSubObject.Interface
    {
        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int Present(uint SyncInterval, uint Flags);

        [VtblIndex(9)]
        [return: NativeTypeName("HRESULT")]
        int GetBuffer(uint Buffer, [NativeTypeName("const IID &")] Guid* riid, void** ppSurface);

        [VtblIndex(10)]
        [return: NativeTypeName("HRESULT")]
        int SetFullscreenState([NativeTypeName("BOOL")] int Fullscreen, IDXGIOutput* pTarget);

        [VtblIndex(11)]
        [return: NativeTypeName("HRESULT")]
        int GetFullscreenState([NativeTypeName("BOOL *")] int* pFullscreen, IDXGIOutput** ppTarget);

        [VtblIndex(12)]
        [return: NativeTypeName("HRESULT")]
        int GetDesc(DXGI_SWAP_CHAIN_DESC* pDesc);

        [VtblIndex(13)]
        [return: NativeTypeName("HRESULT")]
        int ResizeBuffers(uint BufferCount, uint Width, uint Height, DXGI_FORMAT NewFormat, uint SwapChainFlags);

        [VtblIndex(14)]
        [return: NativeTypeName("HRESULT")]
        int ResizeTarget([NativeTypeName("const DXGI_MODE_DESC *")] DXGI_MODE_DESC* pNewTargetParameters);

        [VtblIndex(15)]
        [return: NativeTypeName("HRESULT")]
        int GetContainingOutput(IDXGIOutput** ppOutput);

        [VtblIndex(16)]
        [return: NativeTypeName("HRESULT")]
        int GetFrameStatistics(DXGI_FRAME_STATISTICS* pStats);

        [VtblIndex(17)]
        [return: NativeTypeName("HRESULT")]
        int GetLastPresentCount(uint* pLastPresentCount);
    }
}
