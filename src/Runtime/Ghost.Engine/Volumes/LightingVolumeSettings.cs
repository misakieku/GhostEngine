using Ghost.Core;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.Volumes;

public enum ShadowMode
{
    /// <summary>
    /// No shadows are rendered.
    /// </summary>
    None,
    /// <summary>
    /// Shadows are rendered using shadow maps.
    /// </summary>
    ShadowMap,
    /// <summary>
    /// Shadows are rendered using ray tracing.
    /// </summary>
    RayTraced,
}

[VolumeComponent]
public partial struct ShadowSettings
{
    /// <summary>
    /// Specifies the shadow rendering mode. This can be set to use shadow maps or ray tracing for shadows. The default is ShadowMap.
    /// </summary>
    public ShadowMode? shadowMode;

    /// <summary>
    /// Specifies the maximum distance at which shadows are rendered. This value can be adjusted to control the shadow rendering range in the scene. The default is 400.0f.
    /// </summary>
    public float? maxDistance;

    /// <summary>
    /// Specifies the number of cascades used for shadow mapping. This value can be adjusted to control the quality and performance of shadow rendering. The default is 4.
    /// </summary>
    [Hardcut]
    public int? cascadeCount;
    /// <summary>
    /// Specifies the split distances (percentages) for each cascade in shadow mapping. This value can be adjusted to control the distribution of shadow cascades in the scene. The default is (0.1f, 0.3f, 0.6f).
    /// </summary>
    public float3? splits;
    /// <summary>
    /// Specifies the blend distances (percentages) for each cascade in shadow mapping. This value can be adjusted to control the blending between shadow cascades in the scene. The default is (0.1f, 0.2f, 0.3f, 0.4f).
    /// </summary>
    public float4? blends;

    public static ShadowSettings Default => new ShadowSettings
    {
        shadowMode = ShadowMode.ShadowMap,
        maxDistance = 400.0f,
        cascadeCount = 4,
        splits = new float3(0.1f, 0.3f, 0.6f),
        blends = new float4(0.1f, 0.2f, 0.3f, 0.4f)
    };
}
