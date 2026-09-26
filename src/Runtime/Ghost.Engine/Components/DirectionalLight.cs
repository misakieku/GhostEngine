using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.Components;

/// <summary>
/// Component representing a directional light source (Sun/Moon/Celestial body).
/// </summary>
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
    /// True if this light casts cascaded shadows (if selected as the active primary sun).
    /// </summary>
    public bool castShadows;

    /// <summary>
    /// Local fine-tuning multiplier on global shadow depth biases. Default is 1.0.
    /// </summary>
    public float shadowBiasMultiplier;

    /// <summary>
    /// Sun disk angular diameter in radians (e.g., ~0.53° for Sun ≈ 0.00925 rad).
    /// </summary>
    public float sunDiskAngularDiameter;

    /// <summary>
    /// Multiplier for atmospheric / volumetric fog god-rays and in-scattering.
    /// </summary>
    public float volumetricScattering;

    /// <summary>
    /// True if this light drives physical atmospheric Rayleigh/Mie sky scattering.
    /// </summary>
    public bool affectAtmosphere;

    public static DirectionalLight Default => new()
    {
        color = new float3(1.0f, 1.0f, 1.0f),
        intensity = 1.0f,
        castShadows = true,
        shadowBiasMultiplier = 1.0f,
        sunDiskAngularDiameter = 0.00925f,
        volumetricScattering = 1.0f,
        affectAtmosphere = true
    };
}
