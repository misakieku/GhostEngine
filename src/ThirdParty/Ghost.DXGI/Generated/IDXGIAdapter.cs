using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXGI.DXGI;

namespace Ghost.DXGI;

[Guid("2411E7E1-12AC-4CCF-BD14-9798E8534DC0")]
[NativeTypeName("struct IDXGIAdapter : IDXGIObject")]
public unsafe partial struct IDXGIAdapter : IDXGIAdapter.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDXGIAdapter);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIAdapter*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIAdapter*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIAdapter*, uint>)(lpVtbl[1]))((IDXGIAdapter*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIAdapter*, uint>)(lpVtbl[2]))((IDXGIAdapter*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint DataSize, [NativeTypeName("const void *")] void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIAdapter*, Guid*, uint, void*, int>)(lpVtbl[3]))((IDXGIAdapter*)Unsafe.AsPointer(ref this), Name, DataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int SetPrivateDataInterface([NativeTypeName("const GUID &")] Guid* Name, [NativeTypeName("const IUnknown *")] IUnknown* pUnknown)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIAdapter*, Guid*, IUnknown*, int>)(lpVtbl[4]))((IDXGIAdapter*)Unsafe.AsPointer(ref this), Name, pUnknown);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetPrivateData([NativeTypeName("const GUID &")] Guid* Name, uint* pDataSize, void* pData)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIAdapter*, Guid*, uint*, void*, int>)(lpVtbl[5]))((IDXGIAdapter*)Unsafe.AsPointer(ref this), Name, pDataSize, pData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetParent([NativeTypeName("const IID &")] Guid* riid, void** ppParent)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIAdapter*, Guid*, void**, int>)(lpVtbl[6]))((IDXGIAdapter*)Unsafe.AsPointer(ref this), riid, ppParent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int EnumOutputs(uint Output, IDXGIOutput** ppOutput)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIAdapter*, uint, IDXGIOutput**, int>)(lpVtbl[7]))((IDXGIAdapter*)Unsafe.AsPointer(ref this), Output, ppOutput);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int GetDesc(DXGI_ADAPTER_DESC* pDesc)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIAdapter*, DXGI_ADAPTER_DESC*, int>)(lpVtbl[8]))((IDXGIAdapter*)Unsafe.AsPointer(ref this), pDesc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int CheckInterfaceSupport([NativeTypeName("const GUID &")] Guid* InterfaceName, LARGE_INTEGER* pUMDVersion)
    {
        return ((delegate* unmanaged[MemberFunction]<IDXGIAdapter*, Guid*, LARGE_INTEGER*, int>)(lpVtbl[9]))((IDXGIAdapter*)Unsafe.AsPointer(ref this), InterfaceName, pUMDVersion);
    }

    public interface Interface : IDXGIObject.Interface
    {
        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int EnumOutputs(uint Output, IDXGIOutput** ppOutput);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int GetDesc(DXGI_ADAPTER_DESC* pDesc);

        [VtblIndex(9)]
        [return: NativeTypeName("HRESULT")]
        int CheckInterfaceSupport([NativeTypeName("const GUID &")] Guid* InterfaceName, LARGE_INTEGER* pUMDVersion);
    }
}
