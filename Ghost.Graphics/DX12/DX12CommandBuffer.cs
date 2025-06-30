using Ghost.Graphics.Contracts;
using Ghost.Graphics.Data;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace Ghost.Graphics.DX12;

internal class DX12CommandBuffer : ICommandBuffer
{
    private ID3D12GraphicsCommandList10 _commandList;

    public DX12CommandBuffer(ID3D12GraphicsCommandList10 commandList)
    {
        _commandList = commandList;
    }

    public void CopyResource(IResource dstResource, uint dstOffset, IResource srcResource, uint srcOffset, uint size)
    {
        GraphicsPipeline.CheckAPI(GraphicsAPI.DX12).EnsureSuccess();

        var dstDXResource = Unsafe.As<DX12Resource>(dstResource);
        var srcDXResource = Unsafe.As<DX12Resource>(srcResource);

        _commandList.CopyBufferRegion(dstDXResource.NativeResource, dstOffset, srcDXResource.NativeResource, srcOffset, size);
    }
}