using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using TerraFX.Interop.DirectX;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12CommandAllocator : D3D12Object<ID3D12CommandAllocator>, ICommandAllocator
{
    private static ID3D12CommandAllocator* CreateCommandAllocator(ID3D12Device14* device, D3D12_COMMAND_LIST_TYPE type)
    {
        ID3D12CommandAllocator* pAllocator = default;
        ThrowIfFailed(device->CreateCommandAllocator(type, __uuidof(pAllocator), (void**)&pAllocator));
        return pAllocator;
    }

    public D3D12CommandAllocator(D3D12RenderDevice device, CommandBufferType type)
        : base(CreateCommandAllocator(device.NativeObject, D3D12Utility.ToCommandListType(type)))
    {
    }

    public void Reset()
    {
        AssertNotDisposed();
        pNativeObject->Reset();
    }
}
