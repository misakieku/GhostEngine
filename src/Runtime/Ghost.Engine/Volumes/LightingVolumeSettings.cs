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
    /// Specifies the shadow rendering mode.
    /// </summary>
    public ShadowMode? shadowMode;

    /// <summary>
    /// Specifies the maximum distance at which shadows are rendered.
    /// </summary>
    public float? maxDistance;

    /// <summary>
    /// Specifies the number of cascades used for shadow mapping.
    /// </summary>
    [Hardcut]
    public int? cascadeCount;
    /// <summary>
    /// Specifies the split distances (percentages) for each cascade in shadow mapping.
    /// </summary>
    public float3? splits;
    /// <summary>
    /// Specifies the blend distances (percentages) for each cascade in shadow mapping.
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

[VolumeComponent]
public partial struct ContactShadowSettings: IVolumeComponent<ContactShadowSettings>
{
    /// <summary>
    /// Specifies whether contact shadows are enabled.
    /// </summary>
    public bool? enabled;

    /// <summary>
    /// Specifies the maximum distance at which contact shadows are rendered.
    /// </summary>
    public float? maxDistance;

    /// <summary>
    /// Specifies the tolerance for contact shadow rendering.
    /// </summary>
    public float? tolerance;

    public static ContactShadowSettings Default => new ContactShadowSettings
    {
        enabled = true,
        maxDistance = 0.25f,
        tolerance = 0.15f
    };
}