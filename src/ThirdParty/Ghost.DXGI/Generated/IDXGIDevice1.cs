using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXGI.DXGI;

namespace Ghost.DXGI;

[Guid("77DB970F-6276-48BA-BA28-070143B4392C")]
[NativeTypeName("struct IDXGIDevice1 : IDXGIDevice")]
public unsafe partial struct IDXGIDevice1 : IDXGIDevice1.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDXGIDevice1);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice1*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIDevice1*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice1*, uint>)(lpVtbl[1]))((IDXGIDevice1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice1*, uint>)(lpVtbl[2]))((IDXGIDevice1*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice1*, Guid*, uint, void*, int>)(lpVtbl[3]))((IDXGIDevice1*)Unsafe.AsPointer(ref this), Name, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* Name, [NativeTypeName("const IUnknown *")] IUnknown* pUnknown)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice1*, Guid*, IUnknown*, int>)(lpVtbl[4]))((IDXGIDevice1*)Unsafe.AsPointer(ref this), Name, pUnknown);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice1*, Guid*, uint*, void*, int>)(lpVtbl[5]))((IDXGIDevice1*)Unsafe.AsPointer(ref this), Name, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetParent([NativeTypeName("const IID &")] Guid* riid, void** ppParent)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice1*, Guid*, void**, int>)(lpVtbl[6]))((IDXGIDevice1*)Unsafe.AsPointer(ref this), riid, ppParent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetAdapter(IDXGIAdapter** pAdapter)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice1*, IDXGIAdapter**, int>)(lpVtbl[7]))((IDXGIDevice1*)Unsafe.AsPointer(ref this), pAdapter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int CreateSurface([NativeTypeName("const DXGI_SURFACE_DESC *")] DXGI_SURFACE_DESC* pDesc, uint NumSurfaces, [NativeTypeName("DXGI_USAGE")] uint Usage, [NativeTypeName("const DXGI_SHARED_RESOURCE *")] DXGI_SHARED_RESOURCE* pSharedResource, IDXGISurface** ppSurface)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice1*, DXGI_SURFACE_DESC*, uint, uint, DXGI_SHARED_RESOURCE*, IDXGISurface**, int>)(lpVtbl[8]))((IDXGIDevice1*)Unsafe.AsPointer(ref this), pDesc, NumSurfaces, Usage, pSharedResource, ppSurface);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int QueryResourceResidency([NativeTypeName("IUnknown *const *")] IUnknown** ppResources, DXGI_RESIDENCY* pResidencyStatus, uint NumResources)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice1*, IUnknown**, DXGI_RESIDENCY*, uint, int>)(lpVtbl[9]))((IDXGIDevice1*)Unsafe.AsPointer(ref this), ppResources, pResidencyStatus, NumResources);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int SetGPUThreadPriority(int Priority)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice1*, int, int>)(lpVtbl[10]))((IDXGIDevice1*)Unsafe.AsPointer(ref this), Priority);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("HRESULT")]
    public int GetGPUThreadPriority(int* pPriority)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice1*, int*, int>)(lpVtbl[11]))((IDXGIDevice1*)Unsafe.AsPointer(ref this), pPriority);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    [return: NativeTypeName("HRESULT")]
    public int SetMaximumFrameLatency(uint MaxLatency)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice1*, uint, int>)(lpVtbl[12]))((IDXGIDevice1*)Unsafe.AsPointer(ref this), MaxLatency);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    [return: NativeTypeName("HRESULT")]
    public int GetMaximumFrameLatency(uint* pMaxLatency)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice1*, uint*, int>)(lpVtbl[13]))((IDXGIDevice1*)Unsafe.AsPointer(ref this), pMaxLatency);
    }

    public interface Interface : IDXGIDevice.Interface
    {
        [VtblIndex(12)]
        [return: NativeTypeName("HRESULT")]
        int SetMaximumFrameLatency(uint MaxLatency);

        [VtblIndex(13)]
        [return: NativeTypeName("HRESULT")]
        int GetMaximumFrameLatency(uint* pMaxLatency);
    }
}
