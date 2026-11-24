using System.Runtime.CompilerServices;

namespace Ghost.D3D12;

public partial struct D3D12_BLEND_DESC
{
    [NativeTypeName("BOOL")]
    public int AlphaToCoverageEnable;

    [NativeTypeName("BOOL")]
    public int IndependentBlendEnable;

    [NativeTypeName("D3D12_RENDER_TARGET_BLEND_DESC[8]")]
    public _RenderTarget_e__FixedBuffer RenderTarget;

    [InlineArray(8)]
    public partial struct _RenderTarget_e__FixedBuffer
    {
        public D3D12_RENDER_TARGET_BLEND_DESC e0;
    }
}
