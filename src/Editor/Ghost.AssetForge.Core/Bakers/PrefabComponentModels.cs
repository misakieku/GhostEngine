using Ghost.Core;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.AssetForge.Core.Bakers;

public static class PrefabComponentTypeNames
{
    public const string LOCAL_TRANSFORM = "Ghost.Engine.Components.LocalTransform";
    public const string LOCAL_TO_WORLD = "Ghost.Engine.Components.LocalToWorld";
    public const string HIERARCHY = "Ghost.Engine.Components.Hierarchy";
    public const string MESH_INSTANCE = "Ghost.Engine.Components.MeshInstance";
    public const string DIRECTIONAL_LIGHT = "Ghost.Engine.Components.DirectionalLight";
    public const string PUNCTUAL_LIGHT = "Ghost.Engine.Components.PunctualLight";
    public const string CAMERA = "Ghost.Engine.Components.Camera";
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PrefabLocalTransform
{
    public float3 position;
    public quaternion rotation;
    public float3 scale;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PrefabLocalToWorld
{
    public float4x4 matrix;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PrefabMeshInstance
{
    public uint mesh;
    public int materialPalette;
    public uint renderingLayerMask;
    public uint shadowCastingMode;
    public bool staticShadowCaster;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PrefabDirectionalLight
{
    public float3 color;
    public float intensity;
    public float sunDiskAngularDiameter;
    public float lightingDimmer;
    public float diffuseDimmer;
    public float specularDimmer;
    public float volumetricScattering;
    public bool affectAtmosphere;
    public bool castShadows;
    public uint shadowSize;
    public float normalBias;
    public float depthBias;
    public float shadowDimmer;
    public float shadowLodErrorScale;
    public bool contactShadow;

    public static PrefabDirectionalLight Default => new()
    {
        color = new float3(1.0f, 1.0f, 1.0f),
        intensity = 1.0f,
        sunDiskAngularDiameter = 0.00925f,
        lightingDimmer = 1.0f,
        diffuseDimmer = 1.0f,
        specularDimmer = 1.0f,
        volumetricScattering = 1.0f,
        affectAtmosphere = true,
        castShadows = false,
        shadowSize = 0,
        normalBias = 0.0f,
        depthBias = 0.0f,
        shadowDimmer = 1.0f,
        shadowLodErrorScale = 1.0f,
        contactShadow = false,
    };
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PrefabPunctualLight
{
    public byte type;
    public float3 color;
    public float intensity;
    public float range;
    public float innerSpotAngle;
    public float outerSpotAngle;
    public float sourceRadius;
    public float lightingDimmer;
    public float diffuseDimmer;
    public float specularDimmer;
    public float fadeDistance;
    public float volumetricScattering;
    public uint shadowSize;
    public float normalBias;
    public float depthBias;
    public float nearPlane;
    public float shadowDimmer;
    public float shadowFadeDistance;
    public float shadowLodErrorScale;
    public bool contactShadow;

    public static PrefabPunctualLight CreatePoint(float3 color, float intensity, float range, uint shadowSize = 0) => new()
    {
        type = 0,
        color = color,
        intensity = intensity,
        range = range,
        innerSpotAngle = 0.0f,
        outerSpotAngle = 0.0f,
        sourceRadius = 0.05f,
        nearPlane = 0.05f,
        fadeDistance = 1000.0f,
        volumetricScattering = 1.0f,
        shadowSize = shadowSize,
        normalBias = 0.75f,
        depthBias = 0.5f,
        lightingDimmer = 1.0f,
        diffuseDimmer = 1.0f,
        specularDimmer = 1.0f,
        shadowDimmer = 1.0f,
        shadowFadeDistance = 1000.0f,
        shadowLodErrorScale = 1.0f,
    };

    public static PrefabPunctualLight CreateSpot(float3 color, float intensity, float range, float innerAngleRad, float outerAngleRad, uint shadowSize = 0) => new()
    {
        type = 1,
        color = color,
        intensity = intensity,
        range = range,
        innerSpotAngle = innerAngleRad,
        outerSpotAngle = outerAngleRad,
        sourceRadius = 0.05f,
        nearPlane = 0.05f,
        fadeDistance = 1000.0f,
        volumetricScattering = 1.0f,
        shadowSize = shadowSize,
        normalBias = 0.75f,
        depthBias = 0.5f,
        lightingDimmer = 1.0f,
        diffuseDimmer = 1.0f,
        specularDimmer = 1.0f,
        shadowDimmer = 1.0f,
        shadowFadeDistance = 1000.0f,
        shadowLodErrorScale = 1.0f,
    };
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PrefabCamera
{
    public float nearClipPlane;
    public float farClipPlane;
    public float2 sensorSize;
    public uint gateFit;
    public float iso;
    public float shutterSpeed;
    public float aperture;
    public float focalLength;
    public float focusDistance;
    public uint renderingLayerMask;
    public int swapChainIndex;
    public int priority;
    public uint colorTarget;
    public uint depthTarget;

    public static PrefabCamera Default => new()
    {
        nearClipPlane = 0.01f,
        farClipPlane = 1000.0f,
        sensorSize = new float2(36.0f, 24.0f),
        gateFit = 1, // Vertical
        iso = 100.0f,
        shutterSpeed = 0.01f,
        aperture = 16.0f,
        focalLength = 50.0f,
        focusDistance = 10.0f,
        renderingLayerMask = uint.MaxValue,
        swapChainIndex = 0,
        priority = 0,
        colorTarget = 0,
        depthTarget = 0,
    };
}
