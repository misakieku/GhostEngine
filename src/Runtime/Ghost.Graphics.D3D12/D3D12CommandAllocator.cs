using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using TerraFX.Interop.DirectX;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12CommandAllocator : ICommandAllocator
{
    private UniquePtr<ID3D12CommandAllocator> _allocator;

    public SharedPtr<ID3D12CommandAllocator> NativeAllocator => _allocator.Share();

    public D3D12CommandAllocator(D3D12RenderDevice device, CommandBufferType type)
    {
        ID3D12CommandAllocator* pAllocator = default;
        var commandListType = D3D12Utility.ToCommandListType(type);

        device.NativeDevice.Get()->CreateCommandAllocator(commandListType, __uuidof(pAllocator), (void**)&pAllocator);

        _allocator.Attach(pAllocator);
    }

    public void Reset()
    {
        _allocator.Get()->Reset();
    }

    public void Dispose()
    {
        _allocator.Dispose();
    }
}