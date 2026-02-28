using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Core;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Frustum
{
    // The data of the 6 planes of the frustum
    public float3 normal0;
    public float dist0;
    public float3 normal1;
    public float dist1;
    public float3 normal2;
    public float dist2;
    public float3 normal3;
    public float dist3;
    public float3 normal4;
    public float dist4;
    public float3 normal5;
    public float dist5;

    // The data of the 8 corners of the frustum
    public float3 corner0;
    public float3 corner1;
    public float3 corner2;
    public float3 corner3;
    public float3 corner4;
    public float3 corner5;
    public float3 corner6;
    public float3 corner7;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct RenderView
{
    public float4x4 view;
    public float4x4 projection;
    public float4x4 viewProjection;
    public float3 position;

    public Frustum frustum; // 192 bytes
    public float nearClipPlane;
    public float farClipPlane;

    public float2 sensorSize;
    public float iso;
    public float shutterSpeed;
    public float aperture;
    public float focalLength;
    public float focusDistance;

    public uint renderingLayerMask;
}

public unsafe struct RenderRequest
{
    public RenderView view;
    public Handle<Texture> colorTarget;
    public Handle<Texture> depthTarget;

    public delegate*<ref readonly RenderingContext, ref readonly RenderRequest, void> renderFunc;
}
