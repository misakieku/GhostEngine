namespace Ghost.Graphics.Meshlet;

/// <summary>
/// Contains input data for the Cluster LOD generation pipeline.
/// </summary>
public unsafe struct ClodMesh
{
    /// <summary> Pointer to vertex position data (float array). </summary>
    public float* vertexPositions;
    /// <summary> Number of vertices in the mesh. </summary>
    public nuint vertexCount;
    /// <summary> Stride in bytes for vertex position data. </summary>
    public nuint vertexPositionsStride;
    /// <summary> Pointer to vertex attribute data (float array). </summary>
    public float* vertexAttributes;
    /// <summary> Stride in bytes for vertex attribute data. </summary>
    public nuint vertexAttributesStride;
    /// <summary> Pointer to attribute weights for simplification. </summary>
    public float* attributeWeights;
    /// <summary> Number of vertex attributes. </summary>
    public nuint attributeCount;
    /// <summary> Pointer to index data. </summary>
    public uint* indices;
    /// <summary> Number of indices in the mesh. </summary>
    public nuint indexCount;
    /// <summary> Pointer to per-vertex lock flags (1 byte per vertex). </summary>
    public byte* vertexLock;
    /// <summary> Mask indicating which attributes are protected during simplification. </summary>
    public uint attributeProtectMask;
}

/// <summary>
/// Defines a group of clusters in the LOD hierarchy.
/// </summary>
public struct ClodGroup
{
    /// <summary> LOD hierarchy depth of this group. </summary>
    public int depth;
    /// <summary> Bounding information for the simplified group. </summary>
    public ClodBounds simplified;
}

/// <summary>
/// Represents a cluster of meshlets in the LOD hierarchy.
/// </summary>
public unsafe struct ClodCluster
{
    /// <summary> Refinement level of the cluster. </summary>
    public int refined;
    /// <summary> Bounding info for the cluster. </summary>
    public ClodBounds bounds;
    /// <summary> Pointer to indices for this cluster. </summary>
    public uint* indices;
    /// <summary> Number of indices. </summary>
    public nuint indexCount;
    /// <summary> Number of vertices in the cluster. </summary>
    public nuint vertexCount;
}

/// <summary>
/// Delegate type for processing generated LOD groups.
/// </summary>
public unsafe delegate int ClodOutputDelegate(void* context, ClodGroup group, ClodCluster* clusters, nuint clusterCount);
