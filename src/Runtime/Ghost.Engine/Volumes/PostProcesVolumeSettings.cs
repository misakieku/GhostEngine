using Ghost.Core;

namespace Ghost.Engine.Volumes;

public enum TonemapMode : uint
{
    /// <summary>
    /// No tone mapping is applied. The output color values are linear and may exceed the displayable range.
    /// </summary>
    None,
    /// <summary>
    /// Applies AgX tone mapping.
    /// </summary>
    AgX,
}

[VolumeComponent]
public partial struct TonemapSettings : IVolumeComponent<TonemapSettings>
{
    /// <summary>
    /// The tone mapping mode to use.
    /// </summary>
    public TonemapMode? mode;

    /// <summary>
    /// Whether to use punchy tone mapping in AgX mode.
    /// </summary>
    public bool? punchy;

    public static TonemapSettings Default => new TonemapSettings
    {
        mode = TonemapMode.AgX,
        punchy = true,
    };
}