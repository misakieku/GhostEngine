using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXGI.DXGI;

namespace Ghost.DXGI;

[Guid("6007896C-3244-4AFD-BF18-A6D3BEDA5023")]
[NativeTypeName("struct IDXGIDevice3 : IDXGIDevice2")]
public unsafe partial struct IDXGIDevice3 : IDXGIDevice3.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDXGIDevice3);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIDevice3*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, uint>)(lpVtbl[1]))((IDXGIDevice3*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, uint>)(lpVtbl[2]))((IDXGIDevice3*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, Guid*, uint, void*, int>)(lpVtbl[3]))((IDXGIDevice3*)Unsafe.AsPointer(ref this), Name, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* Name, [NativeTypeName("const IUnknown *")] IUnknown* pUnknown)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, Guid*, IUnknown*, int>)(lpVtbl[4]))((IDXGIDevice3*)Unsafe.AsPointer(ref this), Name, pUnknown);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, Guid*, uint*, void*, int>)(lpVtbl[5]))((IDXGIDevice3*)Unsafe.AsPointer(ref this), Name, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetParent([NativeTypeName("const IID &")] Guid* riid, void** ppParent)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, Guid*, void**, int>)(lpVtbl[6]))((IDXGIDevice3*)Unsafe.AsPointer(ref this), riid, ppParent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetAdapter(IDXGIAdapter** pAdapter)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, IDXGIAdapter**, int>)(lpVtbl[7]))((IDXGIDevice3*)Unsafe.AsPointer(ref this), pAdapter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int CreateSurface([NativeTypeName("const DXGI_SURFACE_DESC *")] DXGI_SURFACE_DESC* pDesc, uint NumSurfaces, [NativeTypeName("DXGI_USAGE")] uint Usage, [NativeTypeName("const DXGI_SHARED_RESOURCE *")] DXGI_SHARED_RESOURCE* pSharedResource, IDXGISurface** ppSurface)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, DXGI_SURFACE_DESC*, uint, uint, DXGI_SHARED_RESOURCE*, IDXGISurface**, int>)(lpVtbl[8]))((IDXGIDevice3*)Unsafe.AsPointer(ref this), pDesc, NumSurfaces, Usage, pSharedResource, ppSurface);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int QueryResourceResidency([NativeTypeName("IUnknown *const *")] IUnknown** ppResources, DXGI_RESIDENCY* pResidencyStatus, uint NumResources)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, IUnknown**, DXGI_RESIDENCY*, uint, int>)(lpVtbl[9]))((IDXGIDevice3*)Unsafe.AsPointer(ref this), ppResources, pResidencyStatus, NumResources);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int SetGPUThreadPriority(int Priority)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, int, int>)(lpVtbl[10]))((IDXGIDevice3*)Unsafe.AsPointer(ref this), Priority);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("HRESULT")]
    public int GetGPUThreadPriority(int* pPriority)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, int*, int>)(lpVtbl[11]))((IDXGIDevice3*)Unsafe.AsPointer(ref this), pPriority);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    [return: NativeTypeName("HRESULT")]
    public int SetMaximumFrameLatency(uint MaxLatency)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, uint, int>)(lpVtbl[12]))((IDXGIDevice3*)Unsafe.AsPointer(ref this), MaxLatency);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    [return: NativeTypeName("HRESULT")]
    public int GetMaximumFrameLatency(uint* pMaxLatency)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, uint*, int>)(lpVtbl[13]))((IDXGIDevice3*)Unsafe.AsPointer(ref this), pMaxLatency);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    [return: NativeTypeName("HRESULT")]
    public int OfferResources(uint NumResources, [NativeTypeName("IDXGIResource *const *")] IDXGIResource** ppResources, [NativeTypeName("DXGI_OFFER_RESOURCE_PRIORITY")] _DXGI_OFFER_RESOURCE_PRIORITY Priority)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, uint, IDXGIResource**, _DXGI_OFFER_RESOURCE_PRIORITY, int>)(lpVtbl[14]))((IDXGIDevice3*)Unsafe.AsPointer(ref this), NumResources, ppResources, Priority);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    [return: NativeTypeName("HRESULT")]
    public int ReclaimResources(uint NumResources, [NativeTypeName("IDXGIResource *const *")] IDXGIResource** ppResources, [NativeTypeName("BOOL *")] int* pDiscarded)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, uint, IDXGIResource**, int*, int>)(lpVtbl[15]))((IDXGIDevice3*)Unsafe.AsPointer(ref this), NumResources, ppResources, pDiscarded);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    [return: NativeTypeName("HRESULT")]
    public int EnqueueSetEvent([NativeTypeName("HANDLE")] void* hEvent)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, void*, int>)(lpVtbl[16]))((IDXGIDevice3*)Unsafe.AsPointer(ref this), hEvent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    public void Trim()
    {
        ((delegate* unmanaged[MemberFunction]<IDXGIDevice3*, void>)(lpVtbl[17]))((IDXGIDevice3*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IDXGIDevice2.Interface
    {
        [VtblIndex(17)]
        void Trim();
    }
}
