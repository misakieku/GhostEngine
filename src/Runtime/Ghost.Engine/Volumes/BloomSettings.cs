using Ghost.Core;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.Volumes;

/// <summary>
/// Volume settings for bloom post-processing (threshold, intensity, scatter, tint, clamp).
/// </summary>
[VolumeComponent]
public partial struct BloomSettings
{
    /// <summary>
    /// Energy threshold above which pixels contribute to bloom. Null if not overridden.
    /// </summary>
    public float? threshold;

    /// <summary>
    /// Strength of the bloom effect. Null if not overridden.
    /// </summary>
    public float? intensity;

    /// <summary>
    /// Extent of the bloom scatter across screen. Null if not overridden.
    /// </summary>
    public float? scatter;

    /// <summary>
    /// Color tint applied to the bloom radiance. Null if not overridden.
    /// </summary>
    public float3? tint;

    /// <summary>
    /// Maximum brightness clamp to prevent fireflies. Hardcut property. Null if not overridden.
    /// </summary>
    [Hardcut]
    public float? clamp;

    /// <summary>
    /// Whether high-quality bicubic filtering is enabled. Hardcut property. Null if not overridden.
    /// </summary>
    public bool? highQuality;

    public static BloomSettings Default => new()
    {
        threshold = 0.9f,
        intensity = 1.0f,
        scatter = 0.7f,
        tint = new float3(1.0f, 1.0f, 1.0f),
        clamp = 65472.0f,
        highQuality = false
    };
}
