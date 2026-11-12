using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Components;

/// <summary>
/// Component representing a directional light source (Sun/Moon/Celestial body).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct DirectionalLight : IComponentData
{
    /// <summary>
    /// Linear RGB light color.
    /// </summary>
    public float3 color;

    /// <summary>
    /// Direct linear intensity multiplier.
    /// </summary>
    public float intensity;

    /// <summary>
    /// Sun disk angular diameter in radians (e.g., ~0.53° for Sun ≈ 0.00925 rad).
    /// </summary>
    public float sunDiskAngularDiameter;

    /// <summary>
    /// Dimmer for the overall lighting contribution of this light.
    /// </summary>
    public float lightingDimmer;

    /// <summary>
    /// Dimmer for the diffuse lighting contribution of this light.
    /// </summary>
    public float diffuseDimmer;

    /// <summary>
    /// Dimmer for the specular lighting contribution of this light.
    /// </summary>
    public float specularDimmer;

    /// <summary>
    /// Multiplier for atmospheric / volumetric fog god-rays and in-scattering.
    /// </summary>
    public float volumetricScattering;

    /// <summary>
    /// True if this light drives physical atmospheric Rayleigh/Mie sky scattering.
    /// </summary>
    public bool affectAtmosphere;

    /// <summary>
    /// True if this light casts cascaded shadows (if selected as the active primary sun).
    /// </summary>
    public bool castShadows;

    /// <summary>
    /// Resolution of the shadow map tile in the shadow atlas (e.g. 256, 512). 0 means no shadow.
    /// </summary>
    public uint shadowResolution;

    /// <summary>
    /// Normal bias for shadow mapping.
    /// </summary>
    public float normalBias;

    /// <summary>
    /// Depth bias for shadow mapping.
    /// </summary>
    public float depthBias;

    /// <summary>
    /// Dimmer for the shadow contribution of this light.
    /// </summary>
    public float shadowDimmer;

    /// <summary>
    /// The scale of the lod error when rendering the shadow.
    /// </summary>
    public float shadowLodErrorScale;

    /// <summary>
    /// Whether to use contact shadows for this light.
    /// </summary>
    public bool contactShadow;

    public static DirectionalLight Default => new()
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
        shadowResolution = 0,
        normalBias = 0.0f,
        depthBias = 0.0f,
        shadowDimmer = 1.0f,
        shadowLodErrorScale = 1.0f,
        contactShadow = false,
    };
}
