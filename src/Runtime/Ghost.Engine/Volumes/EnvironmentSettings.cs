using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.Volumes;

/// <summary>
/// Specifies the type of sky rendering active in the environment.
/// </summary>
public enum SkyType : byte
{
    None = 0,
    HDRI = 1,
    // TDOD: Add support for procedural sky and custom sky types in the future.
}

/// <summary>
/// Global environment volume component controlling the active sky type.
/// </summary>
[VolumeComponent]
public partial struct EnvironmentSettings
{
    /// <summary>
    /// The active sky type. Null if not overridden.
    /// </summary>
    [Hardcut]
    public SkyType? skyType;

    public static EnvironmentSettings Default => new()
    {
        skyType = SkyType.HDRI,
    };
}

/// <summary>
/// Volume settings for skybox rendering (cubemap texture, intensity, rotation, tint).
/// </summary>
[VolumeComponent]
public partial struct HDRISettings
{
    /// <summary>
    /// Cubemap texture handle. Null if not overridden.
    /// </summary>
    public Handle<GPUTexture>? texture;

    /// <summary>
    /// Direct skybox exposure/intensity multiplier. Null if not overridden.
    /// </summary>
    public float? intensity;

    /// <summary>
    /// Y-axis skybox rotation angle in radians. Null if not overridden.
    /// </summary>
    public float? rotation;

    /// <summary>
    /// Linear RGB tint applied to skybox radiance. Null if not overridden.
    /// </summary>
    public float3? tint;

    public static HDRISettings Default => new()
    {
        texture = Handle<GPUTexture>.Invalid,
        intensity = 1.0f,
        rotation = 0.0f,
        tint = new float3(1.0f, 1.0f, 1.0f)
    };
}
