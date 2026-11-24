using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXGI.DXGI;

namespace Ghost.DXGI;

[Guid("790A45F7-0D42-4876-983A-0A55CFE6F4AA")]
[NativeTypeName("struct IDXGISwapChain1 : IDXGISwapChain")]
public unsafe partial struct IDXGISwapChain1 : IDXGISwapChain1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDXGISwapChain1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, Guid*, void**, int>)(lpVtbl[0]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, uint>)(lpVtbl[1]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, uint>)(lpVtbl[2]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, Guid*, uint, void*, int>)(lpVtbl[3]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), Name, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* Name, [NativeTypeName("const IUnknown *")] IUnknown* pUnknown)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, Guid*, IUnknown*, int>)(lpVtbl[4]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), Name, pUnknown);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, Guid*, uint*, void*, int>)(lpVtbl[5]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), Name, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetParent([NativeTypeName("const IID &")] Guid* riid, void** ppParent)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, Guid*, void**, int>)(lpVtbl[6]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), riid, ppParent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetDevice([NativeTypeName("const IID &")] Guid* riid, void** ppDevice)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, Guid*, void**, int>)(lpVtbl[7]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), riid, ppDevice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int Present(uint SyncInterval, uint Flags)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, uint, uint, int>)(lpVtbl[8]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), SyncInterval, Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int GetBuffer(uint Buffer, [NativeTypeName("const IID &")] Guid* riid, void** ppSurface)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, uint, Guid*, void**, int>)(lpVtbl[9]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), Buffer, riid, ppSurface);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int SetFullscreenState([NativeTypeName("BOOL")] int Fullscreen, IDXGIOutput* pTarget)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, int, IDXGIOutput*, int>)(lpVtbl[10]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), Fullscreen, pTarget);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("HRESULT")]
    public int GetFullscreenState([NativeTypeName("BOOL *")] int* pFullscreen, IDXGIOutput** ppTarget)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, int*, IDXGIOutput**, int>)(lpVtbl[11]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), pFullscreen, ppTarget);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    [return: NativeTypeName("HRESULT")]
    public int GetDesc(DXGI_SWAP_CHAIN_DESC* pDesc)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, DXGI_SWAP_CHAIN_DESC*, int>)(lpVtbl[12]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), pDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    [return: NativeTypeName("HRESULT")]
    public int ResizeBuffers(uint BufferCount, uint Width, uint Height, DXGI_FORMAT NewFormat, uint SwapChainFlags)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, uint, uint, uint, DXGI_FORMAT, uint, int>)(lpVtbl[13]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), BufferCount, Width, Height, NewFormat, SwapChainFlags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    [return: NativeTypeName("HRESULT")]
    public int ResizeTarget([NativeTypeName("const DXGI_MODE_DESC *")] DXGI_MODE_DESC* pNewTargetParameters)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, DXGI_MODE_DESC*, int>)(lpVtbl[14]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), pNewTargetParameters);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    [return: NativeTypeName("HRESULT")]
    public int GetContainingOutput(IDXGIOutput** ppOutput)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, IDXGIOutput**, int>)(lpVtbl[15]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), ppOutput);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    [return: NativeTypeName("HRESULT")]
    public int GetFrameStatistics(DXGI_FRAME_STATISTICS* pStats)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, DXGI_FRAME_STATISTICS*, int>)(lpVtbl[16]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), pStats);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    [return: NativeTypeName("HRESULT")]
    public int GetLastPresentCount(uint* pLastPresentCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, uint*, int>)(lpVtbl[17]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), pLastPresentCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(18)]
    [return: NativeTypeName("HRESULT")]
    public int GetDesc1(DXGI_SWAP_CHAIN_DESC1* pDesc)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, DXGI_SWAP_CHAIN_DESC1*, int>)(lpVtbl[18]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), pDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(19)]
    [return: NativeTypeName("HRESULT")]
    public int GetFullscreenDesc(DXGI_SWAP_CHAIN_FULLSCREEN_DESC* pDesc)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, DXGI_SWAP_CHAIN_FULLSCREEN_DESC*, int>)(lpVtbl[19]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), pDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(20)]
    [return: NativeTypeName("HRESULT")]
    public int GetHwnd(HWND* pHwnd)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, HWND*, int>)(lpVtbl[20]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), pHwnd);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(21)]
    [return: NativeTypeName("HRESULT")]
    public int GetCoreWindow([NativeTypeName("const IID &")] Guid* refiid, void** ppUnk)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, Guid*, void**, int>)(lpVtbl[21]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), refiid, ppUnk);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(22)]
    [return: NativeTypeName("HRESULT")]
    public int Present1(uint SyncInterval, uint PresentFlags, [NativeTypeName("const DXGI_PRESENT_PARAMETERS *")] DXGI_PRESENT_PARAMETERS* pPresentParameters)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, uint, uint, DXGI_PRESENT_PARAMETERS*, int>)(lpVtbl[22]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), SyncInterval, PresentFlags, pPresentParameters);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(23)]
    [return: NativeTypeName("BOOL")]
    public int IsTemporaryMonoSupported()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, int>)(lpVtbl[23]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(24)]
    [return: NativeTypeName("HRESULT")]
    public int GetRestrictToOutput(IDXGIOutput** ppRestrictToOutput)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, IDXGIOutput**, int>)(lpVtbl[24]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), ppRestrictToOutput);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(25)]
    [return: NativeTypeName("HRESULT")]
    public int SetBackgroundColor([NativeTypeName("const DXGI_RGBA *")] _D3DCOLORVALUE* pColor)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, _D3DCOLORVALUE*, int>)(lpVtbl[25]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), pColor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(26)]
    [return: NativeTypeName("HRESULT")]
    public int GetBackgroundColor([NativeTypeName("DXGI_RGBA *")] _D3DCOLORVALUE* pColor)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, _D3DCOLORVALUE*, int>)(lpVtbl[26]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), pColor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(27)]
    [return: NativeTypeName("HRESULT")]
    public int SetRotation(DXGI_MODE_ROTATION Rotation)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, DXGI_MODE_ROTATION, int>)(lpVtbl[27]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), Rotation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(28)]
    [return: NativeTypeName("HRESULT")]
    public int GetRotation(DXGI_MODE_ROTATION* pRotation)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGISwapChain1*, DXGI_MODE_ROTATION*, int>)(lpVtbl[28]))((IDXGISwapChain1*)Unsafe.AsPointer(ref this), pRotation);
    }

    public interface Interface : IDXGISwapChain.Interface
    {
        [VtblIndex(18)]
        [return: NativeTypeName("HRESULT")]
        int GetDesc1(DXGI_SWAP_CHAIN_DESC1* pDesc);

        [VtblIndex(19)]
        [return: NativeTypeName("HRESULT")]
        int GetFullscreenDesc(DXGI_SWAP_CHAIN_FULLSCREEN_DESC* pDesc);

        [VtblIndex(20)]
        [return: NativeTypeName("HRESULT")]
        int GetHwnd(HWND* pHwnd);

        [VtblIndex(21)]
        [return: NativeTypeName("HRESULT")]
        int GetCoreWindow([NativeTypeName("const IID &")] Guid* refiid, void** ppUnk);

        [VtblIndex(22)]
        [return: NativeTypeName("HRESULT")]
        int Present1(uint SyncInterval, uint PresentFlags, [NativeTypeName("const DXGI_PRESENT_PARAMETERS *")] DXGI_PRESENT_PARAMETERS* pPresentParameters);

        [VtblIndex(23)]
        [return: NativeTypeName("BOOL")]
        int IsTemporaryMonoSupported();

        [VtblIndex(24)]
        [return: NativeTypeName("HRESULT")]
        int GetRestrictToOutput(IDXGIOutput** ppRestrictToOutput);

        [VtblIndex(25)]
        [return: NativeTypeName("HRESULT")]
        int SetBackgroundColor([NativeTypeName("const DXGI_RGBA *")] _D3DCOLORVALUE* pColor);

        [VtblIndex(26)]
        [return: NativeTypeName("HRESULT")]
        int GetBackgroundColor([NativeTypeName("DXGI_RGBA *")] _D3DCOLORVALUE* pColor);

        [VtblIndex(27)]
        [return: NativeTypeName("HRESULT")]
        int SetRotation(DXGI_MODE_ROTATION Rotation);

        [VtblIndex(28)]
        [return: NativeTypeName("HRESULT")]
        int GetRotation(DXGI_MODE_ROTATION* pRotation);
    }
}
