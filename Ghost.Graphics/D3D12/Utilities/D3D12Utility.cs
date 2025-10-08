using Misaki.HighPerformance.LowLevel.Utilities;
using TerraFX.Interop.DirectX;

using static TerraFX.Aliases.D3D12_Alias;

namespace Ghost.Graphics.D3D12.Utilities;

internal unsafe static class ID3D12Resource_Extensions
{
    extension(ID3D12Resource resource)
    {
        public void SetName(ReadOnlySpan<char> name)
        {
            resource.SetName(name.GetUnsafePtr());
        }
    }
}

internal static class D3D12_RASTERIZER_DESC_Extensions
{
    extension(D3D12_RASTERIZER_DESC)
    {
        public static D3D12_RASTERIZER_DESC CULL_NONE => Create(D3D12_FILL_MODE_SOLID, D3D12_CULL_MODE_NONE);
        public static D3D12_RASTERIZER_DESC CULL_CLOCKWISE => Create(D3D12_FILL_MODE_SOLID, D3D12_CULL_MODE_FRONT);
        public static D3D12_RASTERIZER_DESC CULL_COUNTER_CLOCKWISE => Create(D3D12_FILL_MODE_SOLID, D3D12_CULL_MODE_BACK);
        public static D3D12_RASTERIZER_DESC WIREFRAME => Create(D3D12_FILL_MODE_WIREFRAME, D3D12_CULL_MODE_NONE);

        public static D3D12_RASTERIZER_DESC Create(
            D3D12_FILL_MODE fillMode,
            D3D12_CULL_MODE cullMode,
            bool frontCounterClockwise = false,
            int depthBias = D3D12_DEFAULT_DEPTH_BIAS,
            float depthBiasClamp = D3D12_DEFAULT_DEPTH_BIAS_CLAMP,
            float slopeScaledDepthBias = D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS,
            bool depthClipEnable = true,
            bool multisampleEnable = true,
            bool antialiasedLineEnable = false,
            uint forcedSampleCount = 0,
            D3D12_CONSERVATIVE_RASTERIZATION_MODE conservativeRaster = D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF)
        {
            return new D3D12_RASTERIZER_DESC
            {
                FillMode = fillMode,
                CullMode = cullMode,
                FrontCounterClockwise = frontCounterClockwise ? TRUE : FALSE,
                DepthBias = depthBias,
                DepthBiasClamp = depthBiasClamp,
                SlopeScaledDepthBias = slopeScaledDepthBias,
                DepthClipEnable = depthClipEnable ? TRUE : FALSE,
                MultisampleEnable = multisampleEnable ? TRUE : FALSE,
                AntialiasedLineEnable = antialiasedLineEnable ? TRUE : FALSE,
                ForcedSampleCount = forcedSampleCount,
                ConservativeRaster = conservativeRaster
            };
        }
    }
}

internal static class D3D12_BLEND_DESC_Extensions
{
    extension(D3D12_BLEND_DESC)
    {
        public static D3D12_BLEND_DESC OPAQUE => Create(D3D12_BLEND_ONE, D3D12_BLEND_ZERO);
        public static D3D12_BLEND_DESC ALPHA_BLEND => Create(D3D12_BLEND_SRC_ALPHA, D3D12_BLEND_INV_SRC_ALPHA);
        public static D3D12_BLEND_DESC ADDITIVE => Create(D3D12_BLEND_SRC_ALPHA, D3D12_BLEND_ONE);
        public static D3D12_BLEND_DESC NON_PREMULTIPLIED => Create(D3D12_BLEND_SRC_ALPHA, D3D12_BLEND_INV_SRC_ALPHA);

        public static D3D12_BLEND_DESC Create(D3D12_BLEND srcBlend, D3D12_BLEND destBlend)
        {
            var blendDesc = new D3D12_BLEND_DESC
            {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = false
            };

            for (var i = 0; i < D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT; i++)
            {
                blendDesc.RenderTarget[i].BlendEnable = srcBlend != D3D12_BLEND_ONE || destBlend != D3D12_BLEND_ZERO;
                blendDesc.RenderTarget[i].LogicOp = D3D12_LOGIC_OP_NOOP;
                blendDesc.RenderTarget[i].SrcBlend = srcBlend;
                blendDesc.RenderTarget[i].DestBlend = destBlend;
                blendDesc.RenderTarget[i].BlendOp = D3D12_BLEND_OP_ADD;
                blendDesc.RenderTarget[i].SrcBlendAlpha = srcBlend;
                blendDesc.RenderTarget[i].DestBlendAlpha = destBlend;
                blendDesc.RenderTarget[i].BlendOpAlpha = D3D12_BLEND_OP_ADD;
                blendDesc.RenderTarget[i].RenderTargetWriteMask = (byte)D3D12_COLOR_WRITE_ENABLE_ALL;
            }

            return blendDesc;
        }
    }
}

internal static class D3D12_DEPTH_STENCIL_DESC_Extensions
{
    extension(D3D12_DEPTH_STENCIL_DESC)
    {
        public static D3D12_DEPTH_STENCIL_DESC NONE => Create(false, false, D3D12_COMPARISON_FUNC_LESS_EQUAL);
        public static D3D12_DEPTH_STENCIL_DESC READ => Create(true, false, D3D12_COMPARISON_FUNC_LESS_EQUAL);
        public static D3D12_DEPTH_STENCIL_DESC REVERSE_Z => Create(true, true, D3D12_COMPARISON_FUNC_GREATER_EQUAL);
        public static D3D12_DEPTH_STENCIL_DESC READ_REVERSE_Z => Create(true, false, D3D12_COMPARISON_FUNC_GREATER_EQUAL);

        public static D3D12_DEPTH_STENCIL_DESC Create(bool depthEnable,
            bool depthWriteEnable,
            D3D12_COMPARISON_FUNC depthFunc,
            bool stencilEnable = false,
            byte stencilReadMask = D3D12_DEFAULT_STENCIL_READ_MASK,
            byte stencilWriteMask = D3D12_DEFAULT_STENCIL_WRITE_MASK,
            D3D12_STENCIL_OP frontStencilFailOp = D3D12_STENCIL_OP_KEEP,
            D3D12_STENCIL_OP frontStencilDepthFailOp = D3D12_STENCIL_OP_KEEP,
            D3D12_STENCIL_OP frontStencilPassOp = D3D12_STENCIL_OP_KEEP,
            D3D12_COMPARISON_FUNC frontStencilFunc = D3D12_COMPARISON_FUNC_ALWAYS,
            D3D12_STENCIL_OP backStencilFailOp = D3D12_STENCIL_OP_KEEP,
            D3D12_STENCIL_OP backStencilDepthFailOp = D3D12_STENCIL_OP_KEEP,
            D3D12_STENCIL_OP backStencilPassOp = D3D12_STENCIL_OP_KEEP,
            D3D12_COMPARISON_FUNC backStencilFunc = D3D12_COMPARISON_FUNC_ALWAYS)
        {
            return new D3D12_DEPTH_STENCIL_DESC
            {
                DepthEnable = depthEnable,
                DepthWriteMask = depthWriteEnable ? D3D12_DEPTH_WRITE_MASK_ALL : D3D12_DEPTH_WRITE_MASK_ZERO,
                DepthFunc = depthFunc,
                StencilEnable = stencilEnable,
                StencilReadMask = stencilReadMask,
                StencilWriteMask = stencilWriteMask,
                FrontFace = D3D12_DEPTH_STENCILOP_DESC.Create(frontStencilFailOp, frontStencilDepthFailOp, frontStencilPassOp, frontStencilFunc),
                BackFace = D3D12_DEPTH_STENCILOP_DESC.Create(backStencilFailOp, backStencilDepthFailOp, backStencilPassOp, backStencilFunc)
            };
        }

    }
}

internal static class D3D12_DEPTH_STENCILOP_DESC_Extensions
{
    extension(D3D12_DEPTH_STENCILOP_DESC)
    {
        public static D3D12_DEPTH_STENCILOP_DESC DEFAULT => Create(D3D12_STENCIL_OP_KEEP, D3D12_STENCIL_OP_KEEP, D3D12_STENCIL_OP_KEEP, D3D12_COMPARISON_FUNC_ALWAYS);

        public static D3D12_DEPTH_STENCILOP_DESC Create(D3D12_STENCIL_OP stencilFailOp, D3D12_STENCIL_OP stencilDepthFailOp, D3D12_STENCIL_OP stencilPassOp, D3D12_COMPARISON_FUNC stencilFunc)
        {
            return new D3D12_DEPTH_STENCILOP_DESC
            {
                StencilFailOp = stencilFailOp,
                StencilDepthFailOp = stencilDepthFailOp,
                StencilPassOp = stencilPassOp,
                StencilFunc = stencilFunc
            };
        }
    }
}

internal unsafe static class D3D12MA_Allocation_Extensions
{
    extension(ref readonly D3D12MA_Allocation allocation)
    {
        public bool IsNull => allocation.GetResource() == null
            && allocation.GetHeap() == null
            && allocation.GetSize() == 0;

        public bool IsNotNull => !allocation.IsNull;
    }
}