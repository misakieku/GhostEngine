using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXGI.DXGI;

namespace Ghost.DXGI;

[Guid("50C83A1C-E072-4C48-87B0-3630FA36A6D0")]
[NativeTypeName("struct IDXGIFactory2 : IDXGIFactory1")]
public unsafe partial struct IDXGIFactory2 : IDXGIFactory2.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDXGIFactory2);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, uint>)(lpVtbl[1]))((IDXGIFactory2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, uint>)(lpVtbl[2]))((IDXGIFactory2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, Guid*, uint, void*, int>)(lpVtbl[3]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), Name, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* Name, [NativeTypeName("const IUnknown *")] IUnknown* pUnknown)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, Guid*, IUnknown*, int>)(lpVtbl[4]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), Name, pUnknown);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, Guid*, uint*, void*, int>)(lpVtbl[5]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), Name, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetParent([NativeTypeName("const IID &")] Guid* riid, void** ppParent)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, Guid*, void**, int>)(lpVtbl[6]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), riid, ppParent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int EnumAdapters(uint Adapter, IDXGIAdapter** ppAdapter)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, uint, IDXGIAdapter**, int>)(lpVtbl[7]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), Adapter, ppAdapter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int MakeWindowAssociation(HWND WindowHandle, uint Flags)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, HWND, uint, int>)(lpVtbl[8]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), WindowHandle, Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int GetWindowAssociation(HWND* pWindowHandle)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, HWND*, int>)(lpVtbl[9]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), pWindowHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int CreateSwapChain(IUnknown* pDevice, DXGI_SWAP_CHAIN_DESC* pDesc, IDXGISwapChain** ppSwapChain)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, IUnknown*, DXGI_SWAP_CHAIN_DESC*, IDXGISwapChain**, int>)(lpVtbl[10]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), pDevice, pDesc, ppSwapChain);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("HRESULT")]
    public int CreateSoftwareAdapter(HMODULE Module, IDXGIAdapter** ppAdapter)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, HMODULE, IDXGIAdapter**, int>)(lpVtbl[11]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), Module, ppAdapter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    [return: NativeTypeName("HRESULT")]
    public int EnumAdapters1(uint Adapter, IDXGIAdapter1** ppAdapter)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, uint, IDXGIAdapter1**, int>)(lpVtbl[12]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), Adapter, ppAdapter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    [return: NativeTypeName("BOOL")]
    public int IsCurrent()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, int>)(lpVtbl[13]))((IDXGIFactory2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    [return: NativeTypeName("BOOL")]
    public int IsWindowedStereoEnabled()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, int>)(lpVtbl[14]))((IDXGIFactory2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    [return: NativeTypeName("HRESULT")]
    public int CreateSwapChainForHwnd(IUnknown* pDevice, HWND hWnd, [NativeTypeName("const DXGI_SWAP_CHAIN_DESC1 *")] DXGI_SWAP_CHAIN_DESC1* pDesc, [NativeTypeName("const DXGI_SWAP_CHAIN_FULLSCREEN_DESC *")] DXGI_SWAP_CHAIN_FULLSCREEN_DESC* pFullscreenDesc, IDXGIOutput* pRestrictToOutput, IDXGISwapChain1** ppSwapChain)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, IUnknown*, HWND, DXGI_SWAP_CHAIN_DESC1*, DXGI_SWAP_CHAIN_FULLSCREEN_DESC*, IDXGIOutput*, IDXGISwapChain1**, int>)(lpVtbl[15]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), pDevice, hWnd, pDesc, pFullscreenDesc, pRestrictToOutput, ppSwapChain);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    [return: NativeTypeName("HRESULT")]
    public int CreateSwapChainForCoreWindow(IUnknown* pDevice, IUnknown* pWindow, [NativeTypeName("const DXGI_SWAP_CHAIN_DESC1 *")] DXGI_SWAP_CHAIN_DESC1* pDesc, IDXGIOutput* pRestrictToOutput, IDXGISwapChain1** ppSwapChain)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, IUnknown*, IUnknown*, DXGI_SWAP_CHAIN_DESC1*, IDXGIOutput*, IDXGISwapChain1**, int>)(lpVtbl[16]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), pDevice, pWindow, pDesc, pRestrictToOutput, ppSwapChain);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    [return: NativeTypeName("HRESULT")]
    public int GetSharedResourceAdapterLuid([NativeTypeName("HANDLE")] void* hResource, LUID* pLuid)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, void*, LUID*, int>)(lpVtbl[17]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), hResource, pLuid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(18)]
    [return: NativeTypeName("HRESULT")]
    public int RegisterStereoStatusWindow(HWND WindowHandle, uint wMsg, [NativeTypeName("DWORD *")] uint* pdwCookie)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, HWND, uint, uint*, int>)(lpVtbl[18]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), WindowHandle, wMsg, pdwCookie);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(19)]
    [return: NativeTypeName("HRESULT")]
    public int RegisterStereoStatusEvent([NativeTypeName("HANDLE")] void* hEvent, [NativeTypeName("DWORD *")] uint* pdwCookie)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, void*, uint*, int>)(lpVtbl[19]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), hEvent, pdwCookie);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(20)]
    public void UnregisterStereoStatus([NativeTypeName("DWORD")] uint dwCookie)
    {
        ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, uint, void>)(lpVtbl[20]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), dwCookie);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(21)]
    [return: NativeTypeName("HRESULT")]
    public int RegisterOcclusionStatusWindow(HWND WindowHandle, uint wMsg, [NativeTypeName("DWORD *")] uint* pdwCookie)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, HWND, uint, uint*, int>)(lpVtbl[21]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), WindowHandle, wMsg, pdwCookie);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(22)]
    [return: NativeTypeName("HRESULT")]
    public int RegisterOcclusionStatusEvent([NativeTypeName("HANDLE")] void* hEvent, [NativeTypeName("DWORD *")] uint* pdwCookie)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, void*, uint*, int>)(lpVtbl[22]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), hEvent, pdwCookie);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(23)]
    public void UnregisterOcclusionStatus([NativeTypeName("DWORD")] uint dwCookie)
    {
        ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, uint, void>)(lpVtbl[23]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), dwCookie);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(24)]
    [return: NativeTypeName("HRESULT")]
    public int CreateSwapChainForComposition(IUnknown* pDevice, [NativeTypeName("const DXGI_SWAP_CHAIN_DESC1 *")] DXGI_SWAP_CHAIN_DESC1* pDesc, IDXGIOutput* pRestrictToOutput, IDXGISwapChain1** ppSwapChain)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory2*, IUnknown*, DXGI_SWAP_CHAIN_DESC1*, IDXGIOutput*, IDXGISwapChain1**, int>)(lpVtbl[24]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), pDevice, pDesc, pRestrictToOutput, ppSwapChain);
    }

    public interface Interface : IDXGIFactory1.Interface
    {
        [VtblIndex(14)]
        [return: NativeTypeName("BOOL")]
        int IsWindowedStereoEnabled();

        [VtblIndex(15)]
        [return: NativeTypeName("HRESULT")]
        int CreateSwapChainForHwnd(IUnknown* pDevice, HWND hWnd, [NativeTypeName("const DXGI_SWAP_CHAIN_DESC1 *")] DXGI_SWAP_CHAIN_DESC1* pDesc, [NativeTypeName("const DXGI_SWAP_CHAIN_FULLSCREEN_DESC *")] DXGI_SWAP_CHAIN_FULLSCREEN_DESC* pFullscreenDesc, IDXGIOutput* pRestrictToOutput, IDXGISwapChain1** ppSwapChain);

        [VtblIndex(16)]
        [return: NativeTypeName("HRESULT")]
        int CreateSwapChainForCoreWindow(IUnknown* pDevice, IUnknown* pWindow, [NativeTypeName("const DXGI_SWAP_CHAIN_DESC1 *")] DXGI_SWAP_CHAIN_DESC1* pDesc, IDXGIOutput* pRestrictToOutput, IDXGISwapChain1** ppSwapChain);

        [VtblIndex(17)]
        [return: NativeTypeName("HRESULT")]
        int GetSharedResourceAdapterLuid([NativeTypeName("HANDLE")] void* hResource, LUID* pLuid);

        [VtblIndex(18)]
        [return: NativeTypeName("HRESULT")]
        int RegisterStereoStatusWindow(HWND WindowHandle, uint wMsg, [NativeTypeName("DWORD *")] uint* pdwCookie);

        [VtblIndex(19)]
        [return: NativeTypeName("HRESULT")]
        int RegisterStereoStatusEvent([NativeTypeName("HANDLE")] void* hEvent, [NativeTypeName("DWORD *")] uint* pdwCookie);

        [VtblIndex(20)]
        void UnregisterStereoStatus([NativeTypeName("DWORD")] uint dwCookie);

        [VtblIndex(21)]
        [return: NativeTypeName("HRESULT")]
        int RegisterOcclusionStatusWindow(HWND WindowHandle, uint wMsg, [NativeTypeName("DWORD *")] uint* pdwCookie);

        [VtblIndex(22)]
        [return: NativeTypeName("HRESULT")]
        int RegisterOcclusionStatusEvent([NativeTypeName("HANDLE")] void* hEvent, [NativeTypeName("DWORD *")] uint* pdwCookie);

        [VtblIndex(23)]
        void UnregisterOcclusionStatus([NativeTypeName("DWORD")] uint dwCookie);

        [VtblIndex(24)]
        [return: NativeTypeName("HRESULT")]
        int CreateSwapChainForComposition(IUnknown* pDevice, [NativeTypeName("const DXGI_SWAP_CHAIN_DESC1 *")] DXGI_SWAP_CHAIN_DESC1* pDesc, IDXGIOutput* pRestrictToOutput, IDXGISwapChain1** ppSwapChain);
    }
}
