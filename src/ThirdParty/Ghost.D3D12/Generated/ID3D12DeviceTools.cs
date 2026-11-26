using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.D3D12.D3D12;

namespace Ghost.D3D12;

[Guid("2EA68E9C-19C3-4E47-A109-6CDADFF0ACA9")]
[NativeTypeName("struct ID3D12DeviceTools : IUnknown")]
public unsafe partial struct ID3D12DeviceTools : ID3D12DeviceTools.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_ID3D12DeviceTools);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools*, Guid*, void**, int>)(lpVtbl[0]))((ID3D12DeviceTools*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools*, uint>)(lpVtbl[1]))((ID3D12DeviceTools*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools*, uint>)(lpVtbl[2]))((ID3D12DeviceTools*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void SetNextAllocationAddress([NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")] ulong nextAllocationVirtualAddress)
    {
        ((delegate* unmanaged[MemberFunction]<ID3D12DeviceTools*, ulong, void>)(lpVtbl[3]))((ID3D12DeviceTools*)Unsafe.AsPointer(ref this), nextAllocationVirtualAddress);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        void SetNextAllocationAddress([NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")] ulong nextAllocationVirtualAddress);
    }
}
