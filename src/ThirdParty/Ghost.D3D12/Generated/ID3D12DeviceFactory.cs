using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("61F307D3-D34E-4E7C-8374-3BA4DE23CCCB")]
[NativeTypeName("struct ID3D12DeviceFactory : IUnknown")]
public unsafe partial struct ID3D12DeviceFactory : ID3D12DeviceFactory.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DeviceFactory);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceFactory*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DeviceFactory*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceFactory*, uint>)(lpVtbl[1]))((ID3D12DeviceFactory*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceFactory*, uint>)(lpVtbl[2]))((ID3D12DeviceFactory*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT InitializeFromGlobalState()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceFactory*, int>)(lpVtbl[3]))((ID3D12DeviceFactory*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT ApplyToGlobalState()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceFactory*, int>)(lpVtbl[4]))((ID3D12DeviceFactory*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT SetFlags(D3D12_DEVICE_FACTORY_FLAGS flags)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceFactory*, D3D12_DEVICE_FACTORY_FLAGS, int>)(lpVtbl[5]))((ID3D12DeviceFactory*)Unsafe.AsPointer(ref this), flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public D3D12_DEVICE_FACTORY_FLAGS GetFlags()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceFactory*, D3D12_DEVICE_FACTORY_FLAGS>)(lpVtbl[6]))((ID3D12DeviceFactory*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public HRESULT GetConfigurationInterface([NativeTypeName("const IID &")] Guid* clsid, [NativeTypeName("const IID &")] Guid* iid, void** ppv)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceFactory*, Guid*, Guid*, void**, int>)(lpVtbl[7]))((ID3D12DeviceFactory*)Unsafe.AsPointer(ref this), clsid, iid, ppv);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public HRESULT EnableExperimentalFeatures(uint NumFeatures, [NativeTypeName("const IID *")] Guid* pIIDs, void* pConfigurationStructs, uint* pConfigurationStructSizes)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceFactory*, uint, Guid*, void*, uint*, int>)(lpVtbl[8]))((ID3D12DeviceFactory*)Unsafe.AsPointer(ref this), NumFeatures, pIIDs, pConfigurationStructs, pConfigurationStructSizes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public HRESULT CreateDevice(IUnknown* adapter, D3D_FEATURE_LEVEL FeatureLevel, [NativeTypeName("const IID &")] Guid* riid, void** ppvDevice)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceFactory*, IUnknown*, D3D_FEATURE_LEVEL, Guid*, void**, int>)(lpVtbl[9]))((ID3D12DeviceFactory*)Unsafe.AsPointer(ref this), adapter, FeatureLevel, riid, ppvDevice);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT InitializeFromGlobalState();

        [VtblIndex(4)]
        HRESULT ApplyToGlobalState();

        [VtblIndex(5)]
        HRESULT SetFlags(D3D12_DEVICE_FACTORY_FLAGS flags);

        [VtblIndex(6)]
        D3D12_DEVICE_FACTORY_FLAGS GetFlags();

        [VtblIndex(7)]
        HRESULT GetConfigurationInterface([NativeTypeName("const IID &")] Guid* clsid, [NativeTypeName("const IID &")] Guid* iid, void** ppv);

        [VtblIndex(8)]
        HRESULT EnableExperimentalFeatures(uint NumFeatures, [NativeTypeName("const IID *")] Guid* pIIDs, void* pConfigurationStructs, uint* pConfigurationStructSizes);

        [VtblIndex(9)]
        HRESULT CreateDevice(IUnknown* adapter, D3D_FEATURE_LEVEL FeatureLevel, [NativeTypeName("const IID &")] Guid* riid, void** ppvDevice);
    }
}
