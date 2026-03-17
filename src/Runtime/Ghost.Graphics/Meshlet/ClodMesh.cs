using System;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.Graphics.Meshlet;

public unsafe struct ClodMesh
{
    public float* vertexPositions;
    public nuint vertexCount;
    public nuint vertexPositionsStride;

    public float* vertexAttributes;
    public nuint vertexAttributesStride;
    public float* attributeWeights;
    public nuint attributeCount;

    public uint* indices;
    public nuint indexCount;

    public byte* vertexLock;
    public uint attributeProtectMask;
}

public struct ClodGroup
{
    public int depth;
    public ClodBounds simplified;
}

public unsafe struct ClodCluster
{
    public int refined;
    public ClodBounds bounds;

    public uint* indices;
    public nuint indexCount;
    public nuint vertexCount;
}

public unsafe delegate int ClodOutputDelegate(void* context, ClodGroup group, ClodCluster* clusters, nuint clusterCount);
