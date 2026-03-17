using System;

namespace Ghost.Graphics.Meshlet;

/// <summary>
/// Configuration parameters for the cluster LOD generation pipeline.
/// </summary>
public struct ClodConfig
{
    /// <summary> The maximum number of vertices per meshlet. </summary>
    public nuint maxVertices;
    /// <summary> The minimum number of triangles per meshlet. </summary>
    public nuint minTriangles;
    /// <summary> The maximum number of triangles per meshlet. </summary>
    public nuint maxTriangles;
    /// <summary> Whether to use spatial partitioning during meshlet building. </summary>
    public bool partitionSpatial;
    /// <summary> Whether to sort clusters after partitioning. </summary>
    public bool partitionSort;
    /// <summary> The target size for partitions. </summary>
    public nuint partitionSize;
    /// <summary> Whether to cluster meshlets using spatial clustering. </summary>
    public bool clusterSpatial;
    /// <summary> Weight factor for cluster fill calculation. </summary>
    public float clusterFillWeight;
    /// <summary> Split factor for flexible clustering. </summary>
    public float clusterSplitFactor;
    /// <summary> The simplification ratio to achieve per LOD level. </summary>
    public float simplifyRatio;
    /// <summary> Threshold for stopping simplification. </summary>
    public float simplifyThreshold;
    /// <summary> Error factor used when merging previous LOD level errors. </summary>
    public float simplifyErrorMergePrevious;
    /// <summary> Additive error factor when merging LOD levels. </summary>
    public float simplifyErrorMergeAdditive;
    /// <summary> Error factor for sloppy simplification. </summary>
    public float simplifyErrorFactorSloppy;
    /// <summary> Edge length limit error factor. </summary>
    public float simplifyErrorEdgeLimit;
    /// <summary> Whether to allow permissive simplification. </summary>
    public bool simplifyPermissive;
    /// <summary> Whether to fallback to permissive simplification. </summary>
    public bool simplifyFallbackPermissive;
    /// <summary> Whether to fallback to sloppy simplification. </summary>
    public bool simplifyFallbackSloppy;
    /// <summary> Whether to regularize the mesh during simplification. </summary>
    public bool simplifyRegularize;
    /// <summary> Whether to optimize cluster bounds. </summary>
    public bool optimizeBounds;
    /// <summary> Whether to optimize clusters post-build. </summary>
    public bool optimizeClusters;
}
