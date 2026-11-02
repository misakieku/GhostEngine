#ifndef GHOST_RANDOM_HLSL
#define GHOST_RANDOM_HLSL

float RandomFloat(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

float Hash(uint s)
{
    s = s ^ 2747636419u;
    s = s * 2654435769u;
    s = s ^ (s >> 16);
    s = s * 2654435769u;
    s = s ^ (s >> 16);
    s = s * 2654435769u;
    return float(s) * rcp(4294967296.0); // 2^-32
}

// A single iteration of Bob Jenkins' One-At-A-Time hashing algorithm.
uint JenkinsHash(uint x)
{
    x += (x << 10u);
    x ^= (x >>  6u);
    x += (x <<  3u);
    x ^= (x >> 11u);
    x += (x << 15u);
    return x;
}

// Compound versions of the hashing algorithm.
uint JenkinsHash(uint2 v)
{
    return JenkinsHash(v.x ^ JenkinsHash(v.y));
}

uint JenkinsHash(uint3 v)
{
    return JenkinsHash(v.x ^ JenkinsHash(v.yz));
}

uint JenkinsHash(uint4 v)
{
    return JenkinsHash(v.x ^ JenkinsHash(v.yzw));
}

uint PCGHash(uint input)
{
    uint state = input * 747796405u + 2891336453u;
    uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
    return (word >> 22u) ^ word;
}

// Construct a float with half-open range [0, 1) using low 23 bits.
// All zeros yields 0, all ones yields the next smallest representable value below 1.
float ConstructFloat(int m)
{
    const int ieeeMantissa = 0x007FFFFF; // Binary FP32 mantissa bitmask
    const int ieeeOne      = 0x3F800000; // 1.0 in FP32 IEEE

    m &= ieeeMantissa;                   // Keep only mantissa bits (fractional part)
    m |= ieeeOne;                        // Add fractional part to 1.0

    float  f = asfloat(m);               // Range [1, 2)
    return f - 1;                        // Range [0, 1)
}

float ConstructFloat(uint m)
{
    return ConstructFloat(asint(m));
}

// Pseudo-random value in half-open range [0, 1). The distribution is reasonably uniform.
// Ref: https://stackoverflow.com/a/17479300
float GenerateHashedRandomFloat(uint x)
{
    return ConstructFloat(JenkinsHash(x));
}

float GenerateHashedRandomFloat(uint2 v)
{
    return ConstructFloat(JenkinsHash(v));
}

float GenerateHashedRandomFloat(uint3 v)
{
    return ConstructFloat(JenkinsHash(v));
}

float GenerateHashedRandomFloat(uint4 v)
{
    return ConstructFloat(JenkinsHash(v));
}

float2 InitRandom(float2 input)
{
    float2 r;
    r.x = Hash(uint(input.x * 0xFFFFFFFFu));
    r.y = Hash(uint(input.y * 0xFFFFFFFFu));

    return r;
}

// From Next Generation Post Processing in Call of Duty: Advanced Warfare [Jimenez 2014]
// http://advances.realtimerendering.com/s2014/index.html
float InterleavedGradientNoise(float2 pixCoord, int frameCount)
{
    const float3 magic = float3(0.06711056f, 0.00583715f, 52.9829189f);
    float2 frameMagicScale = float2(2.083f, 4.867f);
    pixCoord += frameCount * frameMagicScale;
    return frac(magic.z * frac(dot(pixCoord, magic.xy)));
}

// High-performance 2D integer hash (Wang / Triple32 / PCG derivative) with isotropic spatial distribution
uint Hash2D(uint2 v)
{
    uint2 q = v * 1664525u + 1013904223u;
    q.x += q.y * 1664525u;
    q.y += q.x * 1664525u;
    q ^= (q >> 16u);
    q.x += q.y * 1664525u;
    q.y += q.x * 1664525u;
    q ^= (q >> 16u);
    return q.x ^ q.y;
}

// Spatiotemporally decorrelated 2D quasi-random sample in [0, 1)^2:
// Combines an isotropic 2D spatial hash with Martin Roberts R2 low-discrepancy temporal sequence.
// Completely eliminates Jimenez IGN rank-1 lattice diagonal striping across screen space.
float2 GetDecorrelatedSample2D(uint2 pixelCoord, uint frameIndex)
{
    uint h1 = Hash2D(pixelCoord);
    uint h2 = Hash2D(pixelCoord ^ 0x9E3779B9u);
    float2 spatialSeed = float2(ConstructFloat(h1), ConstructFloat(h2));
    const float2 r2 = float2(0.7548776662f, 0.5698402910f);
    return frac(spatialSeed + float(frameIndex & 4095u) * r2);
}

// Spatiotemporally decorrelated 1D quasi-random sample in [0, 1):
// For 1D streaming reservoir sampling (RIS candidate selection)
float GetDecorrelatedSample1D(uint2 pixelCoord, uint frameIndex)
{
    uint h = Hash2D(pixelCoord + uint2(71u, 131u));
    float spatialSeed = ConstructFloat(h);
    const float phiInv = 0.61803398875f; // Golden ratio conjugate
    return frac(spatialSeed + float(frameIndex & 4095u) * phiInv);
}

// 32-bit Xorshift random number generator
uint XorShift(inout uint rngState)
{
    rngState ^= rngState << 13;
    rngState ^= rngState >> 17;
    rngState ^= rngState << 5;
    return rngState;
}

#endif
