namespace Ghost.Graphics.Meshlet;

public struct ClodConfig
{
    public nuint maxVertices;
    public nuint minTriangles;
    public nuint maxTriangles;

    public bool partitionSpatial;
    public bool partitionSort;
    public nuint partitionSize;

    public bool clusterSpatial;
    public float clusterFillWeight;
    public float clusterSplitFactor;

    public float simplifyRatio;
    public float simplifyThreshold;

    public float simplifyErrorMergePrevious;
    public float simplifyErrorMergeAdditive;

    public float simplifyErrorFactorSloppy;

    public float simplifyErrorEdgeLimit;

    public bool simplifyPermissive;

    public bool simplifyFallbackPermissive;
    public bool simplifyFallbackSloppy;

    public bool simplifyRegularize;

    public bool optimizeBounds;

    public bool optimizeClusters;
}
