#ifndef GHOST_MATH_HLSL
#define GHOST_MATH_HLSL

#define HALF_MAX        65504.0 // (2 - 2^-10) * 2^15
#define HALF_MAX_MINUS1 65472.0 // (2 - 2^-9) * 2^15
#define EPSILON         1.0e-4
#define PI              3.14159265359
#define TWO_PI          6.28318530718
#define FOUR_PI         12.56637061436
#define INV_PI          0.31830988618
#define INV_TWO_PI      0.15915494309
#define INV_FOUR_PI     0.07957747155
#define HALF_PI         1.57079632679
#define INV_HALF_PI     0.636619772367

#define FLT_EPSILON     1.192092896e-07 // Smallest positive number, such that 1.0 + FLT_EPSILON != 1.0
#define FLT_MIN         1.175494351e-38 // Minimum representable positive floating-point number
#define FLT_MAX         3.402823466e+38 // Maximum representable floating-point number

template<typename T>
T min3(T a, T b, T c)
{
    return min(a, min(b, c));
}

template<typename T>
T max3(T a, T b, T c)
{
    return max(a, max(b, c));
}

template<typename T>
T min4(T a, T b, T c, T d)
{
    return min(min(a, b), min(c, d));
}

template<typename T>
T max4(T a, T b, T c, T d)
{
    return max(max(a, b), max(c, d));
}

template<typename T>
T FastSign(T x)
{
    return saturate(x * FLT_MAX + 0.5) * 2.0 - 1.0;
}

#endif // GHOST_MATH_HLSL
