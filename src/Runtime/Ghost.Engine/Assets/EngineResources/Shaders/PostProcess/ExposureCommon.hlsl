#ifndef GHOST_EXPOSURE_COMMON_HLSL
#define GHOST_EXPOSURE_COMMON_HLSL

#include "EngineResources/Shaders/Common.hlsl"

// Byte offset of GPUPersistentViewState::eyeAdaptationState within each 256-byte view slice
#define EYE_ADAPTATION_STATE_OFFSET 112u

// eyeAdaptationState layout:
//   x: currentExposureScale (final linear exposure multiplier for this frame)
//   y: preExposureScale     (linear multiplier baked into current frame's HDR color & RIS buffers)
//   z: prevPreExposureScale (linear multiplier baked into previous frame's RIS temporal history)
//   w: adaptedEV100         (current smoothed EV100)
float4 LoadViewExposureState(BYTE_ADDRESS_BUFFER persistentViewStateBuffer, uint viewId)
{
    if (!IS_VALID_BUFFER(persistentViewStateBuffer))
    {
        return float4(1.0f, 1.0f, 1.0f, 0.0f);
    }

    ByteAddressBuffer buf = GET_BUFFER(persistentViewStateBuffer);
    uint offset = viewId * 256u + EYE_ADAPTATION_STATE_OFFSET;
    return asfloat(buf.Load4(offset));
}

float GetViewPreExposure(BYTE_ADDRESS_BUFFER persistentViewStateBuffer, uint viewId)
{
    float4 state = LoadViewExposureState(persistentViewStateBuffer, viewId);
    return (state.y > 0.0f && isfinite(state.y)) ? state.y : 1.0f;
}

float EV100ToExposure(float ev100)
{
    // Standard SBS calibration: Lmax = (78 / (q * S)) * 2^EV100 ~= 1.2 * 2^EV100 for q=0.65, S=100
    return rcp(max(1.2f * exp2(ev100), 1e-6f));
}

float LuminanceToEV100(float luminance)
{
    // K = 12.5 cd*s/m^2 => EV100 = log2(L * 100 / K) = log2(L * 8)
    return log2(max(luminance, 1e-5f) * 8.0f);
}

#endif // GHOST_EXPOSURE_COMMON_HLSL
