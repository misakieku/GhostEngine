using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXGI.DXGI;

namespace Ghost.DXGI;

[Guid("A4966EED-76DB-44DA-84C1-EE9A7AFB20A8")]
[NativeTypeName("struct IDXGIFactory7 : IDXGIFactory6")]
public unsafe partial struct IDXGIFactory7 : IDXGIFactory7.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDXGIFactory7);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, uint>)(lpVtbl[1]))((IDXGIFactory7*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, uint>)(lpVtbl[2]))((IDXGIFactory7*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, Guid*, uint, void*, int>)(lpVtbl[3]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), Name, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* Name, [NativeTypeName("const IUnknown *")] IUnknown* pUnknown)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, Guid*, IUnknown*, int>)(lpVtbl[4]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), Name, pUnknown);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, Guid*, uint*, void*, int>)(lpVtbl[5]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), Name, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetParent([NativeTypeName("const IID &")] Guid* riid, void** ppParent)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, Guid*, void**, int>)(lpVtbl[6]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), riid, ppParent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int EnumAdapters(uint Adapter, IDXGIAdapter** ppAdapter)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, uint, IDXGIAdapter**, int>)(lpVtbl[7]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), Adapter, ppAdapter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int MakeWindowAssociation(HWND WindowHandle, uint Flags)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, HWND, uint, int>)(lpVtbl[8]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), WindowHandle, Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int GetWindowAssociation(HWND* pWindowHandle)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, HWND*, int>)(lpVtbl[9]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), pWindowHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int CreateSwapChain(IUnknown* pDevice, DXGI_SWAP_CHAIN_DESC* pDesc, IDXGISwapChain** ppSwapChain)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, IUnknown*, DXGI_SWAP_CHAIN_DESC*, IDXGISwapChain**, int>)(lpVtbl[10]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), pDevice, pDesc, ppSwapChain);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("HRESULT")]
    public int CreateSoftwareAdapter(HMODULE Module, IDXGIAdapter** ppAdapter)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, HMODULE, IDXGIAdapter**, int>)(lpVtbl[11]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), Module, ppAdapter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    [return: NativeTypeName("HRESULT")]
    public int EnumAdapters1(uint Adapter, IDXGIAdapter1** ppAdapter)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, uint, IDXGIAdapter1**, int>)(lpVtbl[12]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), Adapter, ppAdapter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    [return: NativeTypeName("BOOL")]
    public int IsCurrent()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, int>)(lpVtbl[13]))((IDXGIFactory7*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    [return: NativeTypeName("BOOL")]
    public int IsWindowedStereoEnabled()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, int>)(lpVtbl[14]))((IDXGIFactory7*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    [return: NativeTypeName("HRESULT")]
    public int CreateSwapChainForHwnd(IUnknown* pDevice, HWND hWnd, [NativeTypeName("const DXGI_SWAP_CHAIN_DESC1 *")] DXGI_SWAP_CHAIN_DESC1* pDesc, [NativeTypeName("const DXGI_SWAP_CHAIN_FULLSCREEN_DESC *")] DXGI_SWAP_CHAIN_FULLSCREEN_DESC* pFullscreenDesc, IDXGIOutput* pRestrictToOutput, IDXGISwapChain1** ppSwapChain)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, IUnknown*, HWND, DXGI_SWAP_CHAIN_DESC1*, DXGI_SWAP_CHAIN_FULLSCREEN_DESC*, IDXGIOutput*, IDXGISwapChain1**, int>)(lpVtbl[15]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), pDevice, hWnd, pDesc, pFullscreenDesc, pRestrictToOutput, ppSwapChain);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    [return: NativeTypeName("HRESULT")]
    public int CreateSwapChainForCoreWindow(IUnknown* pDevice, IUnknown* pWindow, [NativeTypeName("const DXGI_SWAP_CHAIN_DESC1 *")] DXGI_SWAP_CHAIN_DESC1* pDesc, IDXGIOutput* pRestrictToOutput, IDXGISwapChain1** ppSwapChain)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, IUnknown*, IUnknown*, DXGI_SWAP_CHAIN_DESC1*, IDXGIOutput*, IDXGISwapChain1**, int>)(lpVtbl[16]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), pDevice, pWindow, pDesc, pRestrictToOutput, ppSwapChain);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    [return: NativeTypeName("HRESULT")]
    public int GetSharedResourceAdapterLuid([NativeTypeName("HANDLE")] void* hResource, LUID* pLuid)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, void*, LUID*, int>)(lpVtbl[17]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), hResource, pLuid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(18)]
    [return: NativeTypeName("HRESULT")]
    public int RegisterStereoStatusWindow(HWND WindowHandle, uint wMsg, [NativeTypeName("DWORD *")] uint* pdwCookie)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, HWND, uint, uint*, int>)(lpVtbl[18]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), WindowHandle, wMsg, pdwCookie);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(19)]
    [return: NativeTypeName("HRESULT")]
    public int RegisterStereoStatusEvent([NativeTypeName("HANDLE")] void* hEvent, [NativeTypeName("DWORD *")] uint* pdwCookie)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, void*, uint*, int>)(lpVtbl[19]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), hEvent, pdwCookie);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(20)]
    public void UnregisterStereoStatus([NativeTypeName("DWORD")] uint dwCookie)
    {
        ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, uint, void>)(lpVtbl[20]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), dwCookie);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(21)]
    [return: NativeTypeName("HRESULT")]
    public int RegisterOcclusionStatusWindow(HWND WindowHandle, uint wMsg, [NativeTypeName("DWORD *")] uint* pdwCookie)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, HWND, uint, uint*, int>)(lpVtbl[21]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), WindowHandle, wMsg, pdwCookie);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(22)]
    [return: NativeTypeName("HRESULT")]
    public int RegisterOcclusionStatusEvent([NativeTypeName("HANDLE")] void* hEvent, [NativeTypeName("DWORD *")] uint* pdwCookie)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, void*, uint*, int>)(lpVtbl[22]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), hEvent, pdwCookie);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(23)]
    public void UnregisterOcclusionStatus([NativeTypeName("DWORD")] uint dwCookie)
    {
        ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, uint, void>)(lpVtbl[23]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), dwCookie);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(24)]
    [return: NativeTypeName("HRESULT")]
    public int CreateSwapChainForComposition(IUnknown* pDevice, [NativeTypeName("const DXGI_SWAP_CHAIN_DESC1 *")] DXGI_SWAP_CHAIN_DESC1* pDesc, IDXGIOutput* pRestrictToOutput, IDXGISwapChain1** ppSwapChain)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, IUnknown*, DXGI_SWAP_CHAIN_DESC1*, IDXGIOutput*, IDXGISwapChain1**, int>)(lpVtbl[24]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), pDevice, pDesc, pRestrictToOutput, ppSwapChain);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(25)]
    public uint GetCreationFlags()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, uint>)(lpVtbl[25]))((IDXGIFactory7*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(26)]
    [return: NativeTypeName("HRESULT")]
    public int EnumAdapterByLuid(LUID AdapterLuid, [NativeTypeName("const IID &")] Guid* riid, void** ppvAdapter)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, LUID, Guid*, void**, int>)(lpVtbl[26]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), AdapterLuid, riid, ppvAdapter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(27)]
    [return: NativeTypeName("HRESULT")]
    public int EnumWarpAdapter([NativeTypeName("const IID &")] Guid* riid, void** ppvAdapter)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, Guid*, void**, int>)(lpVtbl[27]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), riid, ppvAdapter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(28)]
    [return: NativeTypeName("HRESULT")]
    public int CheckFeatureSupport(DXGI_FEATURE Feature, void* pFeatureSupportData, uint FeatureSupportDataSize)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, DXGI_FEATURE, void*, uint, int>)(lpVtbl[28]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), Feature, pFeatureSupportData, FeatureSupportDataSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(29)]
    [return: NativeTypeName("HRESULT")]
    public int EnumAdapterByGpuPreference(uint Adapter, DXGI_GPU_PREFERENCE GpuPreference, [NativeTypeName("const IID &")] Guid* riid, void** ppvAdapter)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, uint, DXGI_GPU_PREFERENCE, Guid*, void**, int>)(lpVtbl[29]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), Adapter, GpuPreference, riid, ppvAdapter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(30)]
    [return: NativeTypeName("HRESULT")]
    public int RegisterAdaptersChangedEvent([NativeTypeName("HANDLE")] void* hEvent, [NativeTypeName("DWORD *")] uint* pdwCookie)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, void*, uint*, int>)(lpVtbl[30]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), hEvent, pdwCookie);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(31)]
    [return: NativeTypeName("HRESULT")]
    public int UnregisterAdaptersChangedEvent([NativeTypeName("DWORD")] uint dwCookie)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory7*, uint, int>)(lpVtbl[31]))((IDXGIFactory7*)Unsafe.AsPointer(ref this), dwCookie);
    }

    public interface Interface : IDXGIFactory6.Interface
    {
        [VtblIndex(30)]
        [return: NativeTypeName("HRESULT")]
        int RegisterAdaptersChangedEvent([NativeTypeName("HANDLE")] void* hEvent, [NativeTypeName("DWORD *")] uint* pdwCookie);

        [VtblIndex(31)]
        [return: NativeTypeName("HRESULT")]
        int UnregisterAdaptersChangedEvent([NativeTypeName("DWORD")] uint dwCookie);
    }
}
