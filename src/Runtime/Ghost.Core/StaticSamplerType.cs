namespace Ghost.Core;

/// <summary>
/// Built-in static sampler configurations registered in the global bindless sampler heap.
/// </summary>
public enum StaticSamplerType : uint
{
    PointClamp,
    PointRepeat,
    LinearClamp,
    LinearRepeat,
    TrilinearClamp,
    TrilinearRepeat,
    AnisotropicClamp,
    AnisotropicRepeat,
    LinearClampCompareGE,
    Count
}
