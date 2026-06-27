using System;

namespace Ghost.AssetForge.Core.Attributes;

public class SliderAttribute : DrawerAttribute
{
    public double Min { get; }
    public double Max { get; }

    public SliderAttribute(double min, double max)
    {
        Min = min;
        Max = max;
    }
}
