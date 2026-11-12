using Ghost.Core;
using Ghost.Graphics.Core;
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
    /// Specifies the bias value applies when casting rays for all effects.
    /// </summary>
    public float? rayBias;

    /// <summary>
    /// Specifies the Ray Bias value used when the distance between the pixel and the camera is close to the far plane.
    /// </summary>
    public float? distanceRayBias;

    /// <summary>
    /// Specifies the maximum distance at which shadows are cast for directional lights.
    /// </summary>
    public float? directionalShadowRayLength;

    /// <summary>
    /// Specifies the fallback illuminance value used for directional lights when the ray hit the point outside the light's shadow coverage.
    /// </summary>
    public float? directionalShadowFallback;

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
        rayBias = 0.02f,
        distanceRayBias = 0.05f,
        directionalShadowRayLength = 100.0f,
        directionalShadowFallback = 0.0f,
        cullMode = RayTracingCullMode.ExtendedFrustum,
        sphereCullDistance = 1000.0f,
        solidAngleThreshold = math.radians(4.0f),
    };
}
