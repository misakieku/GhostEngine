using Ghost.Graphics.RHI;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;

using static TerraFX.Interop.DirectX.D3D12_INPUT_CLASSIFICATION;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;

namespace Ghost.Graphics.D3D12.Utilities;

internal static unsafe class D3D12PipelineResource
{
    private static readonly D3D12_INPUT_ELEMENT_DESC[] s_inputElementDescs = [
        new D3D12_INPUT_ELEMENT_DESC{ SemanticName = (sbyte*)Vertex.Semantic.Position.GetUnsafePtr(), SemanticIndex = 0u, Format =  DXGI_FORMAT_R32G32B32A32_FLOAT, InputSlot = 0u, AlignedByteOffset = 0u, InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, InstanceDataStepRate = 0 },
        new D3D12_INPUT_ELEMENT_DESC{ SemanticName = (sbyte*)Vertex.Semantic.Normal.GetUnsafePtr(), SemanticIndex = 0u, Format =  DXGI_FORMAT_R32G32B32A32_FLOAT, InputSlot = 0u, AlignedByteOffset = 16u, InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, InstanceDataStepRate = 0 },
        new D3D12_INPUT_ELEMENT_DESC{ SemanticName = (sbyte*)Vertex.Semantic.Tangent.GetUnsafePtr(), SemanticIndex = 0u, Format =  DXGI_FORMAT_R32G32B32A32_FLOAT, InputSlot = 0u, AlignedByteOffset = 32u, InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, InstanceDataStepRate = 0 },
        new D3D12_INPUT_ELEMENT_DESC{ SemanticName = (sbyte*)Vertex.Semantic.Uv.GetUnsafePtr(), SemanticIndex = 0u, Format =  DXGI_FORMAT_R32G32B32A32_FLOAT, InputSlot = 0u, AlignedByteOffset = 48u, InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, InstanceDataStepRate = 0 },
        new D3D12_INPUT_ELEMENT_DESC{ SemanticName = (sbyte*)Vertex.Semantic.Color.GetUnsafePtr(), SemanticIndex = 0u, Format =  DXGI_FORMAT_R32G32B32A32_FLOAT, InputSlot = 0u, AlignedByteOffset = 64u, InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, InstanceDataStepRate = 0 },
    ];

    public const DXGI_FORMAT SWAP_CHAIN_BACK_BUFFER_FORMAT = DXGI_FORMAT_B8G8R8A8_UNORM;

    public static D3D12_INPUT_LAYOUT_DESC InputLayoutDescription => new()
    {
        pInputElementDescs = (D3D12_INPUT_ELEMENT_DESC*)Unsafe.AsPointer(ref s_inputElementDescs[0]),
        NumElements = (uint)s_inputElementDescs.Length
    };
}
