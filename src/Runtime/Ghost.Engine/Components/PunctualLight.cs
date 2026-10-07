using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.Components;

/// <summary>
/// Type of punctual (localized) light source.
/// </summary>
public enum PunctualLightType : byte
{
    Point = 0,
    Spot = 1
}

/// <summary>
/// Component representing a localized punctual light source (Point or Spot).
/// </summary>
public struct PunctualLight : IComponentData
{
    /// <summary>
    /// Type of light (Point or Spot).
    /// </summary>
    public PunctualLightType type;

    /// <summary>
    /// Linear RGB light color.
    /// </summary>
    public float3 color;

    /// <summary>
    /// Direct linear intensity multiplier.
    /// </summary>
    public float intensity;

    /// <summary>
    /// Attenuation cutoff radius in meters.
    /// </summary>
    public float range;

    /// <summary>
    /// Inner spot angle in radians (Spot light only).
    /// </summary>
    public float innerSpotAngle;

    /// <summary>
    /// Outer spot angle in radians (Spot light only).
    /// </summary>
    public float outerSpotAngle;

    /// <summary>
    /// Source radius in meters for specular GGX normalization and soft contact shadows.
    /// </summary>
    public float sourceRadius;

    /// <summary>
    /// Resolution of the shadow map tile in the shadow atlas (e.g. 256, 512). 0 means no shadow.
    /// </summary>
    public uint shadowSize;

    /// <summary>
    /// Depth bias for shadow mapping.
    /// </summary>
    public float shadowBias;

    /// <summary>
    /// Multiplier for volumetric fog scattering.
    /// </summary>
    public float volumetricScattering;

    public static PunctualLight CreatePoint(float3 color, float intensity, float range, uint shadowSize = 0) => new()
    {
        type = PunctualLightType.Point,
        color = color,
        intensity = intensity,
        range = range,
        innerSpotAngle = 0.0f,
        outerSpotAngle = 0.0f,
        sourceRadius = 0.05f,
        shadowSize = shadowSize,
        shadowBias = 0.001f,
        volumetricScattering = 1.0f
    };

    public static PunctualLight CreateSpot(float3 color, float intensity, float range, float innerAngleRad, float outerAngleRad, uint shadowSize = 0) => new()
    {
        type = PunctualLightType.Spot,
        color = color,
        intensity = intensity,
        range = range,
        innerSpotAngle = innerAngleRad,
        outerSpotAngle = outerAngleRad,
        sourceRadius = 0.05f,
        shadowSize = shadowSize,
        shadowBias = 0.001f,
        volumetricScattering = 1.0f
    };
}
