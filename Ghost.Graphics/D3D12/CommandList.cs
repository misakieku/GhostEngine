using Ghost.Core;
using Ghost.Graphics.Data;
using Win32.Graphics.Direct3D;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

public unsafe class CommandList
{
    private readonly ConstPtr<ID3D12GraphicsCommandList10> _commandList;

    internal ConstPtr<ID3D12GraphicsCommandList10> NativeCommandList => _commandList;

    public CommandList(ID3D12GraphicsCommandList10* commandList)
    {
        _commandList = commandList;
    }

    public void BarrierTransition(GraphicsResource resource, ResourceStates beforeState, ResourceStates afterState)
    {
        _commandList.Ptr->ResourceBarrierTransition(resource.NativeResource.Ptr, beforeState, afterState);
    }

    public void SetGraphicsRootConstantBufferView(uint slot, ulong gpuAddress)
    {
        _commandList.Ptr->SetGraphicsRootConstantBufferView(slot, gpuAddress);
    }

    public void DrawMesh(Mesh mesh, Material material)
    {
        _commandList.Ptr->SetGraphicsRootSignature(material.Shader.RootSignature);
        _commandList.Ptr->SetPipelineState(material.Shader.PipelineState);

        // Bind shader-visible descriptor heaps before setting descriptor tables
        if (material.Shader.Textures.Count > 0)
        {
            var shaderVisibleHeaps = GraphicsPipeline.DescriptorAllocator.GetShaderVisibleHeaps();
            var heapPtrs = stackalloc ID3D12DescriptorHeap*[shaderVisibleHeaps.Length];
            for (var i = 0; i < shaderVisibleHeaps.Length; i++)
            {
                heapPtrs[i] = shaderVisibleHeaps[i].Ptr;
            }
            _commandList.Ptr->SetDescriptorHeaps((uint)shaderVisibleHeaps.Length, heapPtrs);
        }

        material.Bind(this);

        _commandList.Ptr->IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        _commandList.Ptr->IASetVertexBuffers(0, 1, mesh.VertexBufferView);
        _commandList.Ptr->IASetIndexBuffer(mesh.IndexBufferView);

        _commandList.Ptr->DrawIndexedInstanced(mesh.IndexCount, 1, 0, 0, 0);
    }

    public void CopyResource(GraphicsResource dstResource, uint dstOffset, GraphicsResource srcResource, uint srcOffset, uint size)
    {
        _commandList.Ptr->CopyBufferRegion(dstResource.NativeResource, dstOffset, srcResource.NativeResource, srcOffset, size);
    }
}