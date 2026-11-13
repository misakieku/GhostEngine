using Ghost.Core;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Components;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Camera : IComponentData
{
    public float nearClipPlane;
    public float farClipPlane;

    public float2 sensorSize; // mm
    public GateFit gateFit;
    public float iso;
    public float shutterSpeed;
    public float aperture;
    public float focalLength; // mm
    public float focusDistance; // m

    public RenderingLayerMask renderingLayerMask;

    public int swapChainIndex; // The index of the swap chain to render to. -1 means render to rt only.
    public int priority;

    public Handle<GPUTexture> colorTarget;
    public Handle<GPUTexture> depthTarget;

    public static Camera Default => new()
    {
        nearClipPlane = 0.01f,
        farClipPlane = 1000.0f,
        sensorSize = new float2(36.0f, 24.0f),
        gateFit = GateFit.Vertical,
        iso = 100.0f,
        shutterSpeed = 1.0f / 125.0f,
        aperture = 5.6f,
        focalLength = 35.0f,
        focusDistance = 10.0f,
        renderingLayerMask = RenderingLayerMask.All,
        swapChainIndex = 0,
        priority = 0,
        colorTarget = Handle<GPUTexture>.Invalid,
        depthTarget = Handle<GPUTexture>.Invalid,
    };
}
