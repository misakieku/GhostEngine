#ifndef GHOST_MATH_HLSL
#define GHOST_MATH_HLSL

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

#endif // GHOST_MATH_HLSL
