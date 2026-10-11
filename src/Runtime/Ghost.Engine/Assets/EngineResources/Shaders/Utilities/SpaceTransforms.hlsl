#ifndef GHOST_SPACETRANSFORMS_HLSL
#define GHOST_SPACETRANSFORMS_HLSL

// =========================================================================================
// GhostEngine Camera-Relative Rendering Space Transforms
// Modeled after Unity HDRP SpaceTransforms.hlsl.
// Camera-relative rendering is always enabled engine-wide:
// - Shaders operate in Camera-Relative World Space (RWS) where the camera is at (0, 0, 0).
// - g_ViewData.cameraPositionWS contains the absolute world-space camera position.
// - g_ViewData.viewMatrix is a pure rotation matrix (translation is zero).
// =========================================================================================

// Returns the camera position in Camera-Relative World Space (always origin).
float3 GetCameraPositionWS()
{
    return float3(0.0f, 0.0f, 0.0f);
}

// Returns the absolute world-space camera position.
float3 GetAbsoluteCameraPositionWS()
{
    return g_ViewData.cameraPositionWS;
}

// Transforms an absolute world-space model matrix into Camera-Relative World Space.
// In GhostEngine column-major matrices (mul(M, v)), column 3 (_m03_m13_m23) is the translation vector.
float4x4 ApplyCameraTranslationToMatrix(float4x4 modelMatrix)
{
    modelMatrix._m03_m13_m23 -= g_ViewData.cameraPositionWS;
    return modelMatrix;
}

// Reverts a Camera-Relative World Space model matrix back to absolute world space.
float4x4 RevertCameraTranslationFromMatrix(float4x4 modelMatrix)
{
    modelMatrix._m03_m13_m23 += g_ViewData.cameraPositionWS;
    return modelMatrix;
}

// Transforms an absolute inverse model matrix to Camera-Relative inverse model matrix.
float4x4 ApplyCameraTranslationToInverseMatrix(float4x4 inverseModelMatrix)
{
    float4x4 translationMatrix = {
        { 1.0f, 0.0f, 0.0f, g_ViewData.cameraPositionWS.x },
        { 0.0f, 1.0f, 0.0f, g_ViewData.cameraPositionWS.y },
        { 0.0f, 0.0f, 1.0f, g_ViewData.cameraPositionWS.z },
        { 0.0f, 0.0f, 0.0f, 1.0f }
    };
    return mul(inverseModelMatrix, translationMatrix);
}

// Reverts a Camera-Relative inverse model matrix back to absolute inverse model matrix.
float4x4 RevertCameraTranslationFromInverseMatrix(float4x4 inverseModelMatrix)
{
    float4x4 translationMatrix = {
        { 1.0f, 0.0f, 0.0f, -g_ViewData.cameraPositionWS.x },
        { 0.0f, 1.0f, 0.0f, -g_ViewData.cameraPositionWS.y },
        { 0.0f, 0.0f, 1.0f, -g_ViewData.cameraPositionWS.z },
        { 0.0f, 0.0f, 0.0f, 1.0f }
    };
    return mul(inverseModelMatrix, translationMatrix);
}

// Transforms an absolute world-space position into Camera-Relative World Space.
float3 TransformAbsoluteWorldToCameraRelative(float3 positionWS)
{
    return positionWS - g_ViewData.cameraPositionWS;
}

// Transforms a Camera-Relative position back into absolute world space.
float3 TransformCameraRelativeToAbsoluteWorld(float3 positionRWS)
{
    return positionRWS + g_ViewData.cameraPositionWS;
}

// Transforms a Camera-Relative World Space position into View Space.
// Since g_ViewData.viewMatrix is pure rotation, mul((float3x3)viewMatrix, pos) rotates into view space.
float3 TransformWorldToView(float3 positionRWS)
{
    return mul((float3x3)g_ViewData.viewMatrix, positionRWS);
}

// Transforms a View Space position into Camera-Relative World Space.
float3 TransformViewToWorld(float3 positionVS)
{
    return mul(transpose((float3x3)g_ViewData.viewMatrix), positionVS);
}

// Transforms a Camera-Relative World Space position into Homogeneous Clip Space.
float4 TransformWorldToHClip(float3 positionRWS)
{
    return mul(g_ViewData.viewProjectionMatrix, float4(positionRWS, 1.0f));
}

// Transforms a View Space position into Homogeneous Clip Space.
float4 TransformViewToHClip(float3 positionVS)
{
    return mul(g_ViewData.projectionMatrix, float4(positionVS, 1.0f));
}

// Returns the normalized view direction pointing from the surface towards the camera in Camera-Relative World Space.
float3 GetWorldSpaceNormalizeViewDir(float3 positionRWS)
{
    return -normalize(positionRWS);
}

// Returns the view direction vector pointing from the surface towards the camera in Camera-Relative World Space.
float3 GetWorldSpaceViewDir(float3 positionRWS)
{
    return -positionRWS;
}

#endif // GHOST_SPACETRANSFORMS_HLSL
