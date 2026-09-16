using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Core;

public enum GateFit : uint
{
    Vertical,
    Horizontal,
    Fill,
    Overscan,
}

// Since we are using ByteAddressBuffer in hlsl, we don't need to care about the 16 bytes alignment of the data like in CBuffer.
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct RenderView
{
    public float4x4 localToWorld;
    public float nearClipPlane;
    public float farClipPlane;

    // Maybe use fov directly?
    public float2 sensorSize;
    public GateFit gateFit;
    public float iso;
    public float shutterSpeed;
    public float aperture;
    public float focalLength;
    public float focusDistance;

    public RenderingLayerMask renderingLayerMask;
}

[InlineArray(16)]
public struct HZBMipHandles
{
    private Handle<GPUTexture> _element0;
}

public struct RenderRequest
{
    public RenderView view;

    public int swapChainIndex;
    public Handle<GPUTexture> colorTarget;
    public Handle<GPUTexture> depthTarget;

    public uint viewId;
}

