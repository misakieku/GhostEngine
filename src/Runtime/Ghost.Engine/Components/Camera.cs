using Ghost.Core;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.Components;

[RequireComponent<LocalToWorld>]
public unsafe struct Camera : IComponent
{
    public float nearClipPlane;
    public float farClipPlane;

    public float2 sensorSize;
    public GateFit gateFit;
    public float iso;
    public float shutterSpeed;
    public float aperture;
    public float focalLength;
    public float focusDistance;

    public RenderingLayerMask renderingLayerMask;

    public int swapChainIndex; // The index of the swap chain to render to. -1 means render to rt only.
    public int priority;

    public Handle<Texture> colorTarget;
    public Handle<Texture> depthTarget;
    // TODO: Add more render targets like motion vector, etc.

    // Custim render function. If it's not null, the render system will call this function instead of the default render pipeline.
    public delegate*<ref readonly RenderingContext, ref readonly RenderRequest, void> renderFunc;
}
