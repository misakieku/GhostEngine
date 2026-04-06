using Ghost.Core;
using Ghost.Graphics.RenderPipeline;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Core;

public enum GateFit : uint
{
    Vertical,
    Horizontal,
    Fill,
    Overscan,
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Frustum
{
    [InlineArray(6)]
    public struct plane_array
    {
        private float4 plane;
    }

    [InlineArray(8)]
    public struct corner_array
    {
        private float3 corner;
    }

    public plane_array planes;
    public corner_array corners;

    public static void CalculateFrustumPlanes(float4x4 finalMatrix, ref plane_array outPlanes)
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
}

// Since we are using ByteAddressBuffer in hlsl, we don't need to care about the 16 bytes alignment of the data like in CBuffer.
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct RenderView
{
    public float4x4 localToWorld;
    //public float4x4 viewMatrix;
    //public float4x4 projectionMatrix;
    //public float3 position;

    //public Frustum frustum; // 192 bytes
    public float nearClipPlane;
    public float farClipPlane;

    // Maybe use fov directly?
    public float2 sensorSize;
    public GateFit gateFit;
    public float iso;
    public float shutterSpeed;
    public float aperture;
    public float focalLength;
    public float focusDistance;

    public RenderingLayerMask renderingLayerMask;
}

public struct RenderRequest: IDisposable
{
    public RenderView view;

    public int swapChainIndex;
    public Handle<GPUTexture> colorTarget;
    public Handle<GPUTexture> depthTarget;

    public RenderList opaqueRenderList;
    public RenderList transparentRenderList;
    public RenderList shadowCasterRenderList;

    public void Dispose()
    {
        opaqueRenderList.Dispose();
        transparentRenderList.Dispose();
        shadowCasterRenderList.Dispose();
    }
}
