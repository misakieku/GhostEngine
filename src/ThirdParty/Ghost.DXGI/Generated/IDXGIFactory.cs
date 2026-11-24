using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXGI.DXGI;

namespace Ghost.DXGI;

[Guid("7B7166EC-21C7-44AE-B21A-C9AE321AE369")]
[NativeTypeName("struct IDXGIFactory : IDXGIObject")]
public unsafe partial struct IDXGIFactory : IDXGIFactory.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDXGIFactory);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIFactory*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory*, uint>)(lpVtbl[1]))((IDXGIFactory*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory*, uint>)(lpVtbl[2]))((IDXGIFactory*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory*, Guid*, uint, void*, int>)(lpVtbl[3]))((IDXGIFactory*)Unsafe.AsPointer(ref this), Name, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* Name, [NativeTypeName("const IUnknown *")] IUnknown* pUnknown)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory*, Guid*, IUnknown*, int>)(lpVtbl[4]))((IDXGIFactory*)Unsafe.AsPointer(ref this), Name, pUnknown);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory*, Guid*, uint*, void*, int>)(lpVtbl[5]))((IDXGIFactory*)Unsafe.AsPointer(ref this), Name, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetParent([NativeTypeName("const IID &")] Guid* riid, void** ppParent)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory*, Guid*, void**, int>)(lpVtbl[6]))((IDXGIFactory*)Unsafe.AsPointer(ref this), riid, ppParent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int EnumAdapters(uint Adapter, IDXGIAdapter** ppAdapter)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory*, uint, IDXGIAdapter**, int>)(lpVtbl[7]))((IDXGIFactory*)Unsafe.AsPointer(ref this), Adapter, ppAdapter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int MakeWindowAssociation(HWND WindowHandle, uint Flags)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory*, HWND, uint, int>)(lpVtbl[8]))((IDXGIFactory*)Unsafe.AsPointer(ref this), WindowHandle, Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int GetWindowAssociation(HWND* pWindowHandle)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory*, HWND*, int>)(lpVtbl[9]))((IDXGIFactory*)Unsafe.AsPointer(ref this), pWindowHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int CreateSwapChain(IUnknown* pDevice, DXGI_SWAP_CHAIN_DESC* pDesc, IDXGISwapChain** ppSwapChain)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory*, IUnknown*, DXGI_SWAP_CHAIN_DESC*, IDXGISwapChain**, int>)(lpVtbl[10]))((IDXGIFactory*)Unsafe.AsPointer(ref this), pDevice, pDesc, ppSwapChain);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("HRESULT")]
    public int CreateSoftwareAdapter(HMODULE Module, IDXGIAdapter** ppAdapter)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIFactory*, HMODULE, IDXGIAdapter**, int>)(lpVtbl[11]))((IDXGIFactory*)Unsafe.AsPointer(ref this), Module, ppAdapter);
    }

    public interface Interface : IDXGIObject.Interface
    {
        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int EnumAdapters(uint Adapter, IDXGIAdapter** ppAdapter);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int MakeWindowAssociation(HWND WindowHandle, uint Flags);

        [VtblIndex(9)]
        [return: NativeTypeName("HRESULT")]
        int GetWindowAssociation(HWND* pWindowHandle);

        [VtblIndex(10)]
        [return: NativeTypeName("HRESULT")]
        int CreateSwapChain(IUnknown* pDevice, DXGI_SWAP_CHAIN_DESC* pDesc, IDXGISwapChain** ppSwapChain);

        [VtblIndex(11)]
        [return: NativeTypeName("HRESULT")]
        int CreateSoftwareAdapter(HMODULE Module, IDXGIAdapter** ppAdapter);
    }
}
