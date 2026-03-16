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

    public nuint MaxVertices { get => maxVertices; set => maxVertices = value; }
    public nuint MinTriangles { get => minTriangles; set => minTriangles = value; }
    public nuint MaxTriangles { get => maxTriangles; set => maxTriangles = value; }
    public bool PartitionSpatial { get => partitionSpatial; set => partitionSpatial = value; }
    public bool PartitionSort { get => partitionSort; set => partitionSort = value; }
    public nuint PartitionSize { get => partitionSize; set => partitionSize = value; }
    public bool ClusterSpatial { get => clusterSpatial; set => clusterSpatial = value; }
    public float ClusterFillWeight { get => clusterFillWeight; set => clusterFillWeight = value; }
    public float ClusterSplitFactor { get => clusterSplitFactor; set => clusterSplitFactor = value; }
    public float SimplifyRatio { get => simplifyRatio; set => simplifyRatio = value; }
    public float SimplifyThreshold { get => simplifyThreshold; set => simplifyThreshold = value; }
    public float SimplifyErrorMergePrevious { get => simplifyErrorMergePrevious; set => simplifyErrorMergePrevious = value; }
    public float SimplifyErrorMergeAdditive { get => simplifyErrorMergeAdditive; set => simplifyErrorMergeAdditive = value; }
    public float SimplifyErrorFactorSloppy { get => simplifyErrorFactorSloppy; set => simplifyErrorFactorSloppy = value; }
    public float SimplifyErrorEdgeLimit { get => simplifyErrorEdgeLimit; set => simplifyErrorEdgeLimit = value; }
    public bool SimplifyPermissive { get => simplifyPermissive; set => simplifyPermissive = value; }
    public bool SimplifyFallbackPermissive { get => simplifyFallbackPermissive; set => simplifyFallbackPermissive = value; }
    public bool SimplifyFallbackSloppy { get => simplifyFallbackSloppy; set => simplifyFallbackSloppy = value; }
    public bool SimplifyRegularize { get => simplifyRegularize; set => simplifyRegularize = value; }
    public bool OptimizeBounds { get => optimizeBounds; set => optimizeBounds = value; }
    public bool OptimizeClusters { get => optimizeClusters; set => optimizeClusters = value; }
}
