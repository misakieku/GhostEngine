using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXGI.DXGI;

namespace Ghost.DXGI;

[Guid("AE02EEDB-C735-4690-8D52-5A8DC20213AA")]
[NativeTypeName("struct IDXGIOutput : IDXGIObject")]
public unsafe partial struct IDXGIOutput : IDXGIOutput.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDXGIOutput);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIOutput*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, uint>)(lpVtbl[1]))((IDXGIOutput*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, uint>)(lpVtbl[2]))((IDXGIOutput*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, Guid*, uint, void*, int>)(lpVtbl[3]))((IDXGIOutput*)Unsafe.AsPointer(ref this), Name, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* Name, [NativeTypeName("const IUnknown *")] IUnknown* pUnknown)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, Guid*, IUnknown*, int>)(lpVtbl[4]))((IDXGIOutput*)Unsafe.AsPointer(ref this), Name, pUnknown);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, Guid*, uint*, void*, int>)(lpVtbl[5]))((IDXGIOutput*)Unsafe.AsPointer(ref this), Name, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetParent([NativeTypeName("const IID &")] Guid* riid, void** ppParent)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, Guid*, void**, int>)(lpVtbl[6]))((IDXGIOutput*)Unsafe.AsPointer(ref this), riid, ppParent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetDesc(DXGI_OUTPUT_DESC* pDesc)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, DXGI_OUTPUT_DESC*, int>)(lpVtbl[7]))((IDXGIOutput*)Unsafe.AsPointer(ref this), pDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int GetDisplayModeList(DXGI_FORMAT EnumFormat, uint Flags, uint* pNumModes, DXGI_MODE_DESC* pDesc)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, DXGI_FORMAT, uint, uint*, DXGI_MODE_DESC*, int>)(lpVtbl[8]))((IDXGIOutput*)Unsafe.AsPointer(ref this), EnumFormat, Flags, pNumModes, pDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int FindClosestMatchingMode([NativeTypeName("const DXGI_MODE_DESC *")] DXGI_MODE_DESC* pModeToMatch, DXGI_MODE_DESC* pClosestMatch, IUnknown* pConcernedDevice)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, DXGI_MODE_DESC*, DXGI_MODE_DESC*, IUnknown*, int>)(lpVtbl[9]))((IDXGIOutput*)Unsafe.AsPointer(ref this), pModeToMatch, pClosestMatch, pConcernedDevice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int WaitForVBlank()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, int>)(lpVtbl[10]))((IDXGIOutput*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("HRESULT")]
    public int TakeOwnership(IUnknown* pDevice, [NativeTypeName("BOOL")] int Exclusive)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, IUnknown*, int, int>)(lpVtbl[11]))((IDXGIOutput*)Unsafe.AsPointer(ref this), pDevice, Exclusive);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    public void ReleaseOwnership()
    {
        ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, void>)(lpVtbl[12]))((IDXGIOutput*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    [return: NativeTypeName("HRESULT")]
    public int GetGammaControlCapabilities(DXGI_GAMMA_CONTROL_CAPABILITIES* pGammaCaps)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, DXGI_GAMMA_CONTROL_CAPABILITIES*, int>)(lpVtbl[13]))((IDXGIOutput*)Unsafe.AsPointer(ref this), pGammaCaps);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    [return: NativeTypeName("HRESULT")]
    public int SetGammaControl([NativeTypeName("const DXGI_GAMMA_CONTROL *")] DXGI_GAMMA_CONTROL* pArray)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, DXGI_GAMMA_CONTROL*, int>)(lpVtbl[14]))((IDXGIOutput*)Unsafe.AsPointer(ref this), pArray);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    [return: NativeTypeName("HRESULT")]
    public int GetGammaControl(DXGI_GAMMA_CONTROL* pArray)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, DXGI_GAMMA_CONTROL*, int>)(lpVtbl[15]))((IDXGIOutput*)Unsafe.AsPointer(ref this), pArray);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    [return: NativeTypeName("HRESULT")]
    public int SetDisplaySurface(IDXGISurface* pScanoutSurface)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, IDXGISurface*, int>)(lpVtbl[16]))((IDXGIOutput*)Unsafe.AsPointer(ref this), pScanoutSurface);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    [return: NativeTypeName("HRESULT")]
    public int GetDisplaySurfaceData(IDXGISurface* pDestination)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, IDXGISurface*, int>)(lpVtbl[17]))((IDXGIOutput*)Unsafe.AsPointer(ref this), pDestination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(18)]
    [return: NativeTypeName("HRESULT")]
    public int GetFrameStatistics(DXGI_FRAME_STATISTICS* pStats)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIOutput*, DXGI_FRAME_STATISTICS*, int>)(lpVtbl[18]))((IDXGIOutput*)Unsafe.AsPointer(ref this), pStats);
    }

    public interface Interface : IDXGIObject.Interface
    {
        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int GetDesc(DXGI_OUTPUT_DESC* pDesc);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int GetDisplayModeList(DXGI_FORMAT EnumFormat, uint Flags, uint* pNumModes, DXGI_MODE_DESC* pDesc);

        [VtblIndex(9)]
        [return: NativeTypeName("HRESULT")]
        int FindClosestMatchingMode([NativeTypeName("const DXGI_MODE_DESC *")] DXGI_MODE_DESC* pModeToMatch, DXGI_MODE_DESC* pClosestMatch, IUnknown* pConcernedDevice);

        [VtblIndex(10)]
        [return: NativeTypeName("HRESULT")]
        int WaitForVBlank();

        [VtblIndex(11)]
        [return: NativeTypeName("HRESULT")]
        int TakeOwnership(IUnknown* pDevice, [NativeTypeName("BOOL")] int Exclusive);

        [VtblIndex(12)]
        void ReleaseOwnership();

        [VtblIndex(13)]
        [return: NativeTypeName("HRESULT")]
        int GetGammaControlCapabilities(DXGI_GAMMA_CONTROL_CAPABILITIES* pGammaCaps);

        [VtblIndex(14)]
        [return: NativeTypeName("HRESULT")]
        int SetGammaControl([NativeTypeName("const DXGI_GAMMA_CONTROL *")] DXGI_GAMMA_CONTROL* pArray);

        [VtblIndex(15)]
        [return: NativeTypeName("HRESULT")]
        int GetGammaControl(DXGI_GAMMA_CONTROL* pArray);

        [VtblIndex(16)]
        [return: NativeTypeName("HRESULT")]
        int SetDisplaySurface(IDXGISurface* pScanoutSurface);

        [VtblIndex(17)]
        [return: NativeTypeName("HRESULT")]
        int GetDisplaySurfaceData(IDXGISurface* pDestination);

        [VtblIndex(18)]
        [return: NativeTypeName("HRESULT")]
        int GetFrameStatistics(DXGI_FRAME_STATISTICS* pStats);
    }
}
