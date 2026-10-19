using Ghost.Core;

namespace Ghost.Engine.Volumes;

/// <summary>
/// Specifies the type of sky rendering active in the environment.
/// </summary>
public enum SkyType : byte
{
    None = 0,
    HDRI = 1,
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
