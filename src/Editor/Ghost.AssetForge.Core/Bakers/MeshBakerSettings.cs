using CommunityToolkit.Mvvm.ComponentModel;

namespace Ghost.AssetForge.Core.Bakers;

public enum CoordinateAxis
{
    PositiveX,
    PositiveY,
    PositiveZ,
    NegativeX,
    NegativeY,
    NegativeZ
}

public enum VertexDataSource
{
    Imported,
    Computed,
    ComputedIfMissing
}

public partial class MeshBakeSettings : ObservableObject, IBakeSettings
{
    [ObservableProperty]
    public partial CoordinateAxis ObjectUpAxis { get; set; } = CoordinateAxis.PositiveY;

    [ObservableProperty]
    public partial CoordinateAxis ObjectForwardAxis { get; set; } = CoordinateAxis.NegativeZ;

    [ObservableProperty]
    public partial CoordinateAxis ObjectRightAxis { get; set; } = CoordinateAxis.PositiveX;

    [ObservableProperty]
    public partial float UnitMeterScale { get; set; } = 1.0f;

    [ObservableProperty]
    public partial VertexDataSource NormalDataSource { get; set; } = VertexDataSource.ComputedIfMissing;

    [ObservableProperty]
    public partial VertexDataSource TangentDataSource { get; set; } = VertexDataSource.ComputedIfMissing;

    [ObservableProperty]
    public partial int MaxVerticesPerMeshlet { get; set; } = 64;

    [ObservableProperty]
    public partial int MinTrianglesPerMeshlet { get; set; } = 32;

    [ObservableProperty]
    public partial int MaxTrianglesPerMeshlet { get; set; } = 124;

    [ObservableProperty]
    public partial float SimplifyRatio { get; set; } = 0.5f;

    [ObservableProperty]
    public partial float SimplifyThreshold { get; set; } = 0.85f;

    [ObservableProperty]
    public partial bool OptimizeClusters { get; set; } = true;
}
