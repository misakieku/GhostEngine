using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using TerraFX.Interop.DirectX;

using static TerraFX.Aliases.D3D12_Alias;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12CommandSignature : D3D12Object<ID3D12CommandSignature>, ICommandSignature
{
    private static ID3D12CommandSignature* CreateCommandSignature(D3D12RenderDevice device, D3D12PipelineLibrary pipelineLibrary, ref readonly CommandSignatureDesc desc)
    {
        var pDevice = device.NativeObject.Get();
        var pRootSignature = pipelineLibrary.DefaultRootSignature;

        var pArgumentDescs = stackalloc D3D12_INDIRECT_ARGUMENT_DESC[desc.Arguments.Length];

        for (var i = 0; i < desc.Arguments.Length; i++)
        {
            var argument = desc.Arguments[i];
            var pArgumentDesc = &pArgumentDescs[i];

            switch (argument.Type)
            {
                case IndirectArgumentType.Draw:
                    pArgumentDesc->Type = D3D12_INDIRECT_ARGUMENT_TYPE_DRAW;
                    break;
                case IndirectArgumentType.DrawIndexed:
                    pArgumentDesc->Type = D3D12_INDIRECT_ARGUMENT_TYPE_DRAW_INDEXED;
                    break;
                case IndirectArgumentType.Dispatch:
                    pArgumentDesc->Type = D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH;
                    break;
                case IndirectArgumentType.VertexBufferView:
                    pArgumentDesc->Type = D3D12_INDIRECT_ARGUMENT_TYPE_VERTEX_BUFFER_VIEW;
                    pArgumentDesc->VertexBuffer.Slot = argument.VertexBuffer.Slot;
                    break;
                case IndirectArgumentType.IndexBufferView:
                    pArgumentDesc->Type = D3D12_INDIRECT_ARGUMENT_TYPE_INDEX_BUFFER_VIEW;
                    break;
                case IndirectArgumentType.Constant:
                    pArgumentDesc->Type = D3D12_INDIRECT_ARGUMENT_TYPE_CONSTANT;
                    pArgumentDesc->Constant.RootParameterIndex = argument.Constant.RootParameterIndex;
                    pArgumentDesc->Constant.DestOffsetIn32BitValues = argument.Constant.DestOffsetIn32BitValues;
                    pArgumentDesc->Constant.Num32BitValuesToSet = argument.Constant.Num32BitValuesToSet;
                    break;
                case IndirectArgumentType.ConstantBufferView:
                    pArgumentDesc->Type = D3D12_INDIRECT_ARGUMENT_TYPE_CONSTANT_BUFFER_VIEW;
                    pArgumentDesc->ConstantBufferView.RootParameterIndex = argument.ConstantBufferView.RootParameterIndex;
                    break;
                case IndirectArgumentType.ShaderResourceView:
                    pArgumentDesc->Type = D3D12_INDIRECT_ARGUMENT_TYPE_SHADER_RESOURCE_VIEW;
                    pArgumentDesc->ShaderResourceView.RootParameterIndex = argument.ShaderResourceView.RootParameterIndex;
                    break;
                case IndirectArgumentType.UnorderedAccessView:
                    pArgumentDesc->Type = D3D12_INDIRECT_ARGUMENT_TYPE_UNORDERED_ACCESS_VIEW;
                    pArgumentDesc->UnorderedAccessView.RootParameterIndex = argument.UnorderedAccessView.RootParameterIndex;
                    break;
                case IndirectArgumentType.DispatchRays:
                    pArgumentDesc->Type = D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH_RAYS;
                    break;
                case IndirectArgumentType.DispatchMesh:
                    pArgumentDesc->Type = D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH_MESH;
                    break;
                case IndirectArgumentType.IncrementingConstant:
                    pArgumentDesc->Type = D3D12_INDIRECT_ARGUMENT_TYPE_INCREMENTING_CONSTANT;
                    pArgumentDesc->IncrementingConstant.RootParameterIndex = argument.IncrementingConstant.RootParameterIndex;
                    pArgumentDesc->IncrementingConstant.DestOffsetIn32BitValues = argument.IncrementingConstant.DestOffsetIn32BitValues;
                    break;
                default:
                    break;
            }
        }

        var d3d12Desc = new D3D12_COMMAND_SIGNATURE_DESC
        {
            ByteStride = desc.Stride,
            NumArgumentDescs = (uint)desc.Arguments.Length,
            pArgumentDescs = pArgumentDescs,
            NodeMask = 0
        };

        ID3D12CommandSignature* pCommandSignature = default;
        ThrowIfFailed(pDevice->CreateCommandSignature(&d3d12Desc, pRootSignature, __uuidof(pCommandSignature), (void**)pCommandSignature));

        return pCommandSignature;
    }

    public IntPtr NativePointer => (IntPtr)NativeObject.Get();

    public D3D12CommandSignature(D3D12RenderDevice device, D3D12PipelineLibrary pipelineLibrary, ref readonly CommandSignatureDesc desc, Key128<PipelineState> pipelineKey)
        : base(CreateCommandSignature(device, pipelineLibrary, in desc))
    {
    }
}
