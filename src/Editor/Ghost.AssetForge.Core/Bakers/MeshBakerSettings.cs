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

public class MeshBakeSettings : IBakeSettings
{
    public const int VERSION = 1;

    public int Version { get; set; } = VERSION;

    public CoordinateAxis ObjectUpAxis { get; set; } = CoordinateAxis.PositiveY;

    public CoordinateAxis ObjectForwardAxis { get; set; } = CoordinateAxis.NegativeZ;

    public CoordinateAxis ObjectRightAxis { get; set; } = CoordinateAxis.PositiveX;

    public float UnitMeterScale { get; set; } = 1.0f;

    public VertexDataSource NormalDataSource { get; set; } = VertexDataSource.ComputedIfMissing;

    public VertexDataSource TangentDataSource { get; set; } = VertexDataSource.ComputedIfMissing;

    public float SimplifyRatio { get; set; } = 0.5f;

    public float SimplifyThreshold { get; set; } = 0.85f;

    public int PartitionSize { get; set; } = 32;

    public float SimplifyErrorMergePrevious { get; set; } = 1.0f;

    public float SimplifyErrorFactorSloppy { get; set; } = 2.0f;

    public bool SimplifyPermissive { get; set; } = true;

    public bool SimplifyFallbackPermissive { get; set; } = false;

    public bool SimplifyFallbackSloppy { get; set; } = true;

    public bool OptimizeClusters { get; set; } = true;

    public bool FlipUVs { get; set; } = true;

    public float FallbackPercentage { get; set; } = 0.0f;

    public float FallbackRelativeError { get; set; } = 0.01f;

    public bool ExtractMaterials { get; set; } = false;

    public bool ExtractEmbeddedTextures { get; set; } = false;

    public Dictionary<string, Guid> MaterialSlotRemap { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
