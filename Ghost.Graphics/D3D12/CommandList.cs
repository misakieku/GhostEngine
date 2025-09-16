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

    internal void BarrierTransition(GraphicsResource resource, ResourceStates beforeState, ResourceStates afterState)
    {
        _commandList.Ptr->ResourceBarrierTransition(resource.NativeResource.Ptr, beforeState, afterState);
    }

    internal void SetGraphicsRootConstantBufferView(uint slot, ulong gpuAddress)
    {
        _commandList.Ptr->SetGraphicsRootConstantBufferView(slot, gpuAddress);
    }

    /// <summary>
    /// Draws a mesh using fully bindless rendering with SM 6.6 support.
    /// This method does not use the Input Assembler stage and instead relies on
    /// vertex and index buffer access through bindless descriptors in the shader.
    /// </summary>
    /// <param name="mesh">The mesh to draw</param>
    /// <param name="material">The bindless material to use</param>
    public void DrawMesh(Mesh mesh, Material material)
    {
        // Bind the bindless material (sets up root signature, pipeline state, and descriptor heaps)
        material.Bind(this);

        // For fully bindless rendering, we don't use the Input Assembler stage
        // Instead, we use instanced drawing where each "instance" represents a triangle
        // The shader will use SV_InstanceID to index into the index buffer and then into the vertex buffer
        _commandList.Ptr->IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw without vertex/index buffers - use instanced drawing
        // Each instance represents a triangle (3 vertices)
        var triangleCount = mesh.IndexCount / 3;
        _commandList.Ptr->DrawInstanced(3, triangleCount, 0, 0);
    }

    public void SetRenderTarget(RenderTexture? color, RenderTexture? depth)
    {
        var rtvHandle = color?.RenderTargetView?.CpuHandle;
        var rtvHandleValue = rtvHandle ?? default;
        var pRtvHandle = rtvHandle.HasValue ? &rtvHandleValue : null;

        var dsvHandle = depth?.RenderTargetView?.CpuHandle;
        var dsvHandleValue = dsvHandle ?? default;
        var pDsvHandle = dsvHandle.HasValue ? &dsvHandleValue : null;

        _commandList.Ptr->OMSetRenderTargets(1, pRtvHandle, false, pDsvHandle);
    }

    public void ClearRenderTarget(RenderTexture renderTarget, Color16 color)
    {
        renderTarget.ClearColor(this, color);
    }

    public void ClearDepthStencil(RenderTexture depthStencil, ClearFlags flags, float depth = 1.0f, byte stencil = 0)
    {
        depthStencil.ClearDepthStencil(this, flags, depth, stencil);
    }

    public void CopyResource(GraphicsResource dstResource, uint dstOffset, GraphicsResource srcResource, uint srcOffset, uint size)
    {
        _commandList.Ptr->CopyBufferRegion(dstResource.NativeResource, dstOffset, srcResource.NativeResource, srcOffset, size);
    }
}