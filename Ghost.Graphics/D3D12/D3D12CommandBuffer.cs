using Ghost.Core;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.Data;
using Win32.Graphics.Direct3D;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12CommandBuffer : ICommandBuffer
{
    private readonly ConstPtr<ID3D12GraphicsCommandList10> _commandList;

    internal ConstPtr<ID3D12GraphicsCommandList10> CommandList => _commandList;

    public D3D12CommandBuffer(ID3D12GraphicsCommandList10* commandList)
    {
        _commandList = commandList;
    }

    public void BarrierTransition(IResource resource, ResourceStates beforeState, ResourceStates afterState)
    {
        var dxResource = (D3D12Resource)resource;
        _commandList.Ptr->ResourceBarrierTransition(dxResource.NativeResource.Ptr, beforeState, afterState);
    }

    public void SetGraphicsRootConstantBufferView(uint slot, ulong gpuAddress)
    {
        _commandList.Ptr->SetGraphicsRootConstantBufferView(slot, gpuAddress);
    }

    public void DrawMesh(Mesh mesh, Material material)
    {
        _commandList.Ptr->SetGraphicsRootSignature(material.Shader.RootSignature);
        _commandList.Ptr->SetPipelineState(material.Shader.PipelineState);

        material.UploadAndBind(this);

        _commandList.Ptr->IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        _commandList.Ptr->IASetVertexBuffers(0, 1, mesh.VertexBufferView);
        _commandList.Ptr->IASetIndexBuffer(mesh.IndexBufferView);

        _commandList.Ptr->DrawIndexedInstanced(mesh.IndexCount, 1, 0, 0, 0);
    }

    public void CopyResource(IResource dstResource, uint dstOffset, IResource srcResource, uint srcOffset, uint size)
    {
        var dstDXResource = (D3D12Resource)dstResource;
        var srcDXResource = (D3D12Resource)srcResource;

        _commandList.Ptr->CopyBufferRegion(dstDXResource.NativeResource, dstOffset, srcDXResource.NativeResource, srcOffset, size);
    }
}