namespace Ghost.Graphics.Meshlet;

public unsafe struct ClodMesh
{
    public uint* indices;
    public nuint indexCount;
    public nuint vertexCount;
    public float* vertexPositions;
    public nuint vertexPositionsStride;
    public float* vertexAttributes;
    public nuint vertexAttributesStride;
    public byte* vertexLock;
    public float* attributeWeights;
    public nuint attributeCount;
    public uint attributeProtectMask;
}
