using Ghost.Core;

namespace Ghost.Engine.Volumes;

/// <summary>
/// Specifies how scene exposure is computed for the view.
/// </summary>
public enum ExposureMode : uint
{
    /// <summary>
    /// Uses a fixed manual EV100 value.
    /// </summary>
    Fixed = 0,
    /// <summary>
    /// Computes EV100 from the camera's physical aperture, shutter speed, and ISO.
    /// </summary>
    UsePhysicalCamera = 1,
    /// <summary>
    /// Automatically adapts exposure using a GPU log-luminance histogram.
    /// </summary>
    AutomaticHistogram = 2,
}

/// <summary>
/// Specifies how screen pixels are weighted during automatic exposure metering.
/// </summary>
public enum MeteringMode : uint
{
    /// <summary>
    /// Uniformly weights all pixels across the screen.
    /// </summary>
    Average = 0,
    /// <summary>
    /// Applies a radial falloff prioritizing the center of the view.
    /// </summary>
    CenterWeighted = 1,
}

/// <summary>
/// Volume component controlling camera exposure and automatic eye adaptation.
/// </summary>
[VolumeComponent]
public partial struct ExposureSettings
{
    /// <summary>
    /// Active exposure mode.
    /// </summary>
    [Hardcut]
    public ExposureMode? mode;

    /// <summary>
    /// Metering mode used when <see cref="mode"/> is <see cref="ExposureMode.AutomaticHistogram"/>.
    /// </summary>
    [Hardcut]
    public MeteringMode? meteringMode;

    /// <summary>
    /// Manual EV100 value used in <see cref="ExposureMode.Fixed"/> mode.
    /// </summary>
    public float? fixedExposure;

    /// <summary>
    /// Exposure compensation offset in EV stops applied in all modes.
    /// </summary>
    public float? compensation;

    /// <summary>
    /// Minimum allowed EV100 in automatic exposure mode.
    /// </summary>
    public float? minEV100;

    /// <summary>
    /// Maximum allowed EV100 in automatic exposure mode.
    /// </summary>
    public float? maxEV100;

    /// <summary>
    /// Eye adaptation speed (EV/sec) when adapting from dark to bright.
    /// </summary>
    public float? speedUp;

    /// <summary>
    /// Eye adaptation speed (EV/sec) when adapting from bright to dark.
    /// </summary>
    public float? speedDown;

    /// <summary>
    /// Lower percentile [0..1] of the luminance histogram to exclude.
    /// </summary>
    public float? lowPercent;

    /// <summary>
    /// Upper percentile [0..1] of the luminance histogram to exclude.
    /// </summary>
    public float? highPercent;

    /// <summary>
    /// Minimum log2 luminance tracked by the histogram.
    /// </summary>
    public float? histogramMinEV;

    /// <summary>
    /// Maximum log2 luminance tracked by the histogram.
    /// </summary>
    public float? histogramMaxEV;

    public static ExposureSettings Default => new()
    {
        mode = ExposureMode.AutomaticHistogram,
        meteringMode = MeteringMode.CenterWeighted,
        fixedExposure = 0.0f,
        compensation = 0.0f,
        minEV100 = 0.0f,
        maxEV100 = 12.0f,
        speedUp = 3.0f,
        speedDown = 1.0f,
        lowPercent = 0.40f,
        highPercent = 0.90f,
        histogramMinEV = -12.0f,
        histogramMaxEV = 22.0f,
    };
}
