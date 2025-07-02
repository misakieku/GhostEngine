using Ghost.Graphics.Data;
using System.Runtime.CompilerServices;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.D3D12.Utilities;

internal unsafe static class D3D12PipelineResource
{
    private readonly static InputElementDescription[] s_inputElementDescs = [
        new InputElementDescription{ SemanticName = Vertex.Semantic.PositionName, SemanticIndex = 0u, Format =  Format.R32G32B32A32Float, InputSlot = 0u, AlignedByteOffset = 0u, InputSlotClass = InputClassification.PerVertexData, InstanceDataStepRate = 0 },
        new InputElementDescription{ SemanticName = Vertex.Semantic.NormalName, SemanticIndex = 0u, Format =  Format.R32G32B32A32Float, InputSlot = 0u, AlignedByteOffset = 16u, InputSlotClass = InputClassification.PerVertexData, InstanceDataStepRate = 0 },
        new InputElementDescription{ SemanticName = Vertex.Semantic.TangentName, SemanticIndex = 0u, Format =  Format.R32G32B32A32Float, InputSlot = 0u, AlignedByteOffset = 32u, InputSlotClass = InputClassification.PerVertexData, InstanceDataStepRate = 0 },
        new InputElementDescription{ SemanticName = Vertex.Semantic.ColorName, SemanticIndex = 0u, Format =  Format.R32G32B32A32Float, InputSlot = 0u, AlignedByteOffset = 48u, InputSlotClass = InputClassification.PerVertexData, InstanceDataStepRate = 0 },
        new InputElementDescription{ SemanticName = Vertex.Semantic.UVName, SemanticIndex = 0u, Format =  Format.R32G32B32A32Float, InputSlot = 0u, AlignedByteOffset = 64u, InputSlotClass = InputClassification.PerVertexData, InstanceDataStepRate = 0 }
    ];

    public const Format SWAP_CHAIN_BACK_BUFFER_FORMAT = Format.B8G8R8A8Unorm;

    public static InputLayoutDescription InputLayoutDescription => new()
    {
        pInputElementDescs = (InputElementDescription*)Unsafe.AsPointer(ref s_inputElementDescs[0]),
        NumElements = (uint)s_inputElementDescs.Length
    };
}