using Ghost.Core.Graphics;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RHI;

public static class RootSignatureLayout
{
    public const int PUSH_CONSTANT_SLOT = 0;

    public const int ROOT_PARAMETER_COUNT = 1;
}

[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct PushConstantsData
{
    public const uint NUM_32BITS_VALUE = 16u / sizeof(uint);
    public const int PROPERTY_OR_INSTANCE_START = 8;
    public const int PROPERTY_OR_INSTANCE_OFFSET = 2;

    [FieldOffset(0)]
    public uint frameBuffer;
    [FieldOffset(4)]
    public uint viewBuffer;
    [FieldOffset(8)]
    public uint instanceIndex;
    [FieldOffset(8)]
    public uint propertyBuffer;
    [FieldOffset(12)]
    public uint userData;

    public readonly ReadOnlySpan<uint> AsUInts()
    {
        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in frameBuffer), (int)NUM_32BITS_VALUE);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct FrameData
{
    public uint instanceBuffer;
    public uint userBuffer;
    public uint paletteOffsetBuffer;   // bindless index into PaletteOffsetBuffer
    public uint materialIndexBuffer;   // bindless index into MaterialIndexBuffer
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct InstanceData
{
    public float4x4 localToWorld;
    public uint meshBuffer;
    public uint materialPaletteIndex;  // index into PaletteOffsetBuffer (from MaterialPaletteStore)
    public uint pad0;
    public uint pad1;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Frustum
{
    [InlineArray(6)]
    public struct __plane_array
    {
        private float4 plane;
    }

    [InlineArray(8)]
    public struct __corner_array
    {
        private float3 corner;
    }

    [GenerateAsHLSLType("float4[6]")]
    public __plane_array planes;
    [GenerateAsHLSLType("float3[8]")]
    public __corner_array corners;

    private static void CalculateFrustumPlanes(float4x4 finalMatrix, ref __plane_array outPlanes)
    {
        const int planeFrustumLeft = 0;
        const int planeFrustumRight = 1;
        const int planeFrustumBottom = 2;
        const int planeFrustumTop = 3;
        const int planeFrustumNear = 4;
        const int planeFrustumFar = 5;

        float4 tmpVec = default;
        float4 otherVec = default;

        tmpVec[0] = finalMatrix[0][3];
        tmpVec[1] = finalMatrix[1][3];
        tmpVec[2] = finalMatrix[2][3];
        tmpVec[3] = finalMatrix[3][3];

        otherVec[0] = finalMatrix[0][0];
        otherVec[1] = finalMatrix[1][0];
        otherVec[2] = finalMatrix[2][0];
        otherVec[3] = finalMatrix[3][0];

        // left & right
        var leftNormalX = otherVec[0] + tmpVec[0];
        var leftNormalY = otherVec[1] + tmpVec[1];
        var leftNormalZ = otherVec[2] + tmpVec[2];
        var leftDistance = otherVec[3] + tmpVec[3];
        var leftDot = leftNormalX * leftNormalX + leftNormalY * leftNormalY + leftNormalZ * leftNormalZ;
        var leftMagnitude = math.sqrt(leftDot);
        var leftInvMagnitude = 1.0f / leftMagnitude;
        leftNormalX *= leftInvMagnitude;
        leftNormalY *= leftInvMagnitude;
        leftNormalZ *= leftInvMagnitude;
        leftDistance *= leftInvMagnitude;
        outPlanes[planeFrustumLeft].xyz = new float3(leftNormalX, leftNormalY, leftNormalZ);
        outPlanes[planeFrustumLeft].w = leftDistance;

        var rightNormalX = -otherVec[0] + tmpVec[0];
        var rightNormalY = -otherVec[1] + tmpVec[1];
        var rightNormalZ = -otherVec[2] + tmpVec[2];
        var rightDistance = -otherVec[3] + tmpVec[3];
        var rightDot = rightNormalX * rightNormalX + rightNormalY * rightNormalY + rightNormalZ * rightNormalZ;
        var rightMagnitude = math.sqrt(rightDot);
        var rightInvMagnitude = 1.0f / rightMagnitude;
        rightNormalX *= rightInvMagnitude;
        rightNormalY *= rightInvMagnitude;
        rightNormalZ *= rightInvMagnitude;
        rightDistance *= rightInvMagnitude;
        outPlanes[planeFrustumRight].xyz = new float3(rightNormalX, rightNormalY, rightNormalZ);
        outPlanes[planeFrustumRight].w = rightDistance;

        // bottom & top
        otherVec[0] = finalMatrix[0][1];
        otherVec[1] = finalMatrix[1][1];
        otherVec[2] = finalMatrix[2][1];
        otherVec[3] = finalMatrix[3][1];

        var bottomNormalX = otherVec[0] + tmpVec[0];
        var bottomNormalY = otherVec[1] + tmpVec[1];
        var bottomNormalZ = otherVec[2] + tmpVec[2];
        var bottomDistance = otherVec[3] + tmpVec[3];
        var bottomDot = bottomNormalX * bottomNormalX + bottomNormalY * bottomNormalY + bottomNormalZ * bottomNormalZ;
        var bottomMagnitude = math.sqrt(bottomDot);
        var bottomInvMagnitude = 1.0f / bottomMagnitude;
        bottomNormalX *= bottomInvMagnitude;
        bottomNormalY *= bottomInvMagnitude;
        bottomNormalZ *= bottomInvMagnitude;
        bottomDistance *= bottomInvMagnitude;
        outPlanes[planeFrustumBottom].xyz = new float3(bottomNormalX, bottomNormalY, bottomNormalZ);
        outPlanes[planeFrustumBottom].w = bottomDistance;

        var topNormalX = -otherVec[0] + tmpVec[0];
        var topNormalY = -otherVec[1] + tmpVec[1];
        var topNormalZ = -otherVec[2] + tmpVec[2];
        var topDistance = -otherVec[3] + tmpVec[3];
        var topDot = topNormalX * topNormalX + topNormalY * topNormalY + topNormalZ * topNormalZ;
        var topMagnitude = math.sqrt(topDot);
        var topInvMagnitude = 1.0f / topMagnitude;
        topNormalX *= topInvMagnitude;
        topNormalY *= topInvMagnitude;
        topNormalZ *= topInvMagnitude;
        topDistance *= topInvMagnitude;
        outPlanes[planeFrustumTop].xyz = new float3(topNormalX, topNormalY, topNormalZ);
        outPlanes[planeFrustumTop].w = topDistance;

        // near & far
        otherVec[0] = finalMatrix[0][2];
        otherVec[1] = finalMatrix[1][2];
        otherVec[2] = finalMatrix[2][2];
        otherVec[3] = finalMatrix[3][2];

        var nearNormalX = otherVec[0] + tmpVec[0];
        var nearNormalY = otherVec[1] + tmpVec[1];
        var nearNormalZ = otherVec[2] + tmpVec[2];
        var nearDistance = otherVec[3] + tmpVec[3];
        var nearDot = nearNormalX * nearNormalX + nearNormalY * nearNormalY + nearNormalZ * nearNormalZ;
        var nearMagnitude = math.sqrt(nearDot);
        var nearInvMagnitude = 1.0f / nearMagnitude;
        nearNormalX *= nearInvMagnitude;
        nearNormalY *= nearInvMagnitude;
        nearNormalZ *= nearInvMagnitude;
        nearDistance *= nearInvMagnitude;
        outPlanes[planeFrustumNear].xyz = new float3(nearNormalX, nearNormalY, nearNormalZ);
        outPlanes[planeFrustumNear].w = nearDistance;

        var farNormalX = -otherVec[0] + tmpVec[0];
        var farNormalY = -otherVec[1] + tmpVec[1];
        var farNormalZ = -otherVec[2] + tmpVec[2];
        var farDistance = -otherVec[3] + tmpVec[3];
        var farDot = farNormalX * farNormalX + farNormalY * farNormalY + farNormalZ * farNormalZ;
        var farMagnitude = math.sqrt(farDot);
        var farInvMagnitude = 1.0f / farMagnitude;
        farNormalX *= farInvMagnitude;
        farNormalY *= farInvMagnitude;
        farNormalZ *= farInvMagnitude;
        farDistance *= farInvMagnitude;
        outPlanes[planeFrustumFar].xyz = new float3(farNormalX, farNormalY, farNormalZ);
        outPlanes[planeFrustumFar].w = farDistance;
    }

    private static float3 IntersectFrustumPlanes(float4 p0, float4 p1, float4 p2)
    {
        var n0 = p0.xyz;
        var n1 = p1.xyz;
        var n2 = p2.xyz;

        var det = math.dot(math.cross(n0, n1), n2);
        return (math.cross(n2, n1) * p0.w + math.cross(n0, n2) * p1.w - math.cross(n0, n1) * p2.w) * (1.0f / det);
    }

    public static Frustum Create(float4x4 vpMatrix, float3 viewPos, float3 viewDir, float nearClip, float farClip)
    {
        var frustum = new Frustum();
        CalculateFrustumPlanes(vpMatrix, ref frustum.planes);

        viewDir = math.normalize(viewDir);
        var nearD = -math.dot(viewDir, viewPos) - nearClip;
        var farD = math.dot(viewDir, viewPos) + farClip;

        frustum.planes[4] = new float4(viewDir, nearD);
        frustum.planes[5] = new float4(-viewDir, farD);

        // Compute corners from the planes instead of projection matrix. Otherwise you get the same issue with near and far for oblique projection.
        frustum.corners[0] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[3], frustum.planes[4]);
        frustum.corners[1] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[3], frustum.planes[4]);
        frustum.corners[2] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[2], frustum.planes[4]);
        frustum.corners[3] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[2], frustum.planes[4]);
        frustum.corners[4] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[3], frustum.planes[5]);
        frustum.corners[5] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[3], frustum.planes[5]);
        frustum.corners[6] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[2], frustum.planes[5]);
        frustum.corners[7] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[2], frustum.planes[5]);

        return frustum;
    }
}


[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct ViewData
{
    public float4x4 viewMatrix;
    public float4x4 projectionMatrix;
    public float4x4 viewProjectionMatrix;
    public float4x4 preVPMatrix;
    public float3 cameraPosition;
    public float nearClip;
    public float3 cameraDirection;
    public float farClip;
    public float4 screenSize; // xy: size, zw: 1/size
    public Frustum frustum;
};

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct MeshData
{
    public float3 worldBoundsMin;
    public uint vertexBuffer;
    public float3 worldBoundsMax;
    public uint indexBuffer;

    public uint meshletBuffer;
    public uint meshletVerticesBuffer;
    public uint meshletTrianglesBuffer;
    public uint meshletGroupBuffer;
    public uint meshletHierarchyBuffer;
    public uint meshletCount;
    public uint meshletGroupCount;
    public uint lodLevelCount;
    public uint materialSlotCount;     // number of material slots baked into this mesh's meshlets
};
