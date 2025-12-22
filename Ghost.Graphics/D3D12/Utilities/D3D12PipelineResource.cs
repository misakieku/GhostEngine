using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;
using Ghost.Graphics.Core;

namespace Ghost.Graphics.D3D12.Utilities;

internal unsafe static class D3D12PipelineResource
{
    private readonly static D3D12_INPUT_ELEMENT_DESC[] s_inputElementDescs = [
        new D3D12_INPUT_ELEMENT_DESC{ SemanticName = (sbyte*)Vertex.Semantic.position.GetUnsafePointer(), SemanticIndex = 0u, Format =  Vertex.Semantic.ALIGNED_FORMAT, InputSlot = 0u, AlignedByteOffset = 0u, InputSlotClass = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, InstanceDataStepRate = 0 },
        new D3D12_INPUT_ELEMENT_DESC{ SemanticName = (sbyte*)Vertex.Semantic.normal.GetUnsafePointer(), SemanticIndex = 0u, Format =  Vertex.Semantic.ALIGNED_FORMAT, InputSlot = 0u, AlignedByteOffset = 16u, InputSlotClass = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, InstanceDataStepRate = 0 },
        new D3D12_INPUT_ELEMENT_DESC{ SemanticName = (sbyte*)Vertex.Semantic.tangent.GetUnsafePointer(), SemanticIndex = 0u, Format =  Vertex.Semantic.ALIGNED_FORMAT, InputSlot = 0u, AlignedByteOffset = 32u, InputSlotClass = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, InstanceDataStepRate = 0 },
        new D3D12_INPUT_ELEMENT_DESC{ SemanticName = (sbyte*)Vertex.Semantic.uv.GetUnsafePointer(), SemanticIndex = 0u, Format =  Vertex.Semantic.ALIGNED_FORMAT, InputSlot = 0u, AlignedByteOffset = 48u, InputSlotClass = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, InstanceDataStepRate = 0 },
        new D3D12_INPUT_ELEMENT_DESC{ SemanticName = (sbyte*)Vertex.Semantic.color.GetUnsafePointer(), SemanticIndex = 0u, Format =  Vertex.Semantic.ALIGNED_FORMAT, InputSlot = 0u, AlignedByteOffset = 64u, InputSlotClass = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, InstanceDataStepRate = 0 },
    ];

    public const DXGI_FORMAT SWAP_CHAIN_BACK_BUFFER_FORMAT = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM;

    public static D3D12_INPUT_LAYOUT_DESC InputLayoutDescription => new()
    {
        pInputElementDescs = (D3D12_INPUT_ELEMENT_DESC*)Unsafe.AsPointer(ref s_inputElementDescs[0]),
        NumElements = (uint)s_inputElementDescs.Length
    };
}
