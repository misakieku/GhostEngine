using Ghost.Core;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.Volumes;

public enum RayTracingCullMode
{
    /// <summary>
    /// Exclude the frustum into a w * h * 2d bounding box and use this for culling.
    /// </summary>
    ExtendedFrustum,
    /// <summary>
    /// A bounding sphere is used for culling.
    /// </summary>
    Sphere,
    /// <summary>
    /// Use a solid angle to determine if the object is visible. This is more accurate than a bounding sphere, but more expensive to compute.
    /// </summary>
    SolidAngle,
}

[VolumeComponent]
public partial struct RayTracingSettings
{
    /// <summary>
    /// Specifies the bias value applies when casting rays for all effects. This value should remain unchained unless your scene scale is significantly smaller or larger than average.
    /// </summary>
    public float? rayBias;

    /// <summary>
    /// Specifies the Ray Bias value used when the distance between the pixel and the camera is close to the far plane. Between the near and far plane the Ray Bias and Distant Ray Bias are interpolated linearly.
    /// This does not affect Path Tracing or Recursive Rendering. This value can be increased to mitigate Ray Tracing z-fighting issues at a distance.
    /// </summary>
    public float? distanceRayBias;

    /// <summary>
    /// Specifies the culling mode used for Ray Tracing. This can be set to use a bounding sphere, extended frustum, or solid angle for culling. The default is Extended Frustum.
    /// </summary>
    public RayTracingCullMode? cullMode;

    /// <summary>
    /// Specifies the distance at which to cull objects when using a bounding sphere for culling.
    /// </summary>
    public float? sphereCullDistance;

    /// <summary>
    /// Specifies the solid angle threshold in radians used for culling when using solid angle culling.
    /// </summary>
    public float? solidAngleThreshold;

    public static RayTracingSettings Default => new RayTracingSettings
    {
        rayBias = 0.001f,
        distanceRayBias = 0.01f,
        cullMode = RayTracingCullMode.ExtendedFrustum,
        sphereCullDistance = 1000.0f,
        solidAngleThreshold = math.radians(4.0f),
    };
}
