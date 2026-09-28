#ifndef GHOST_LIT_HLSL
#define GHOST_LIT_HLSL

struct DirectLighting
{
    float3 diffuse;
    float3 specular;
};

struct IndirectLighting
{
    float3 specularReflected;
    float3 specularTransmitted;
};

struct AggregateLighting
{
    DirectLighting direct;
    IndirectLighting indirect;
};

struct LightLoopOutput
{
    float3 diffuse;
    float3 specular;
};

static inline void AccumulateDirectLighting(inout AggregateLighting total, in DirectLighting direct)
{
    total.direct.diffuse += direct.diffuse;
    total.direct.specular += direct.specular;
}

static inline void AccumulateIndirectLighting(inout AggregateLighting total, in IndirectLighting indirect)
{
    total.indirect.specularReflected += indirect.specularReflected;
    total.indirect.specularTransmitted += indirect.specularTransmitted;
}

#endif // GHOST_LIT_HLSL
