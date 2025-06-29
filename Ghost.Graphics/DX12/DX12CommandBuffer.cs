using Ghost.Graphics.Contracts;
using Vortice.Direct3D12;

namespace Ghost.Graphics.DX12;

internal class DX12CommandBuffer : ICommandBuffer
{
    private ID3D12GraphicsCommandList10 _commandList;

    public DX12CommandBuffer(ID3D12GraphicsCommandList10 commandList)
    {
        _commandList = commandList;
    }
}