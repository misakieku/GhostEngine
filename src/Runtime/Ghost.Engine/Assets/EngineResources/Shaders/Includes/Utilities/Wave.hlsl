#ifndef GHOST_WAVE_HLSL
#define GHOST_WAVE_HLSL

#define WaveInterlockedAddScalar(Dest, bCondition, ValuePerThread, OutOffset) \
{ \
    /* Count how many lanes in the wave satisfy the condition (bCondition == true) */ \
    /* Hardware reads the active mask and counts set bits; very fast */ \
    uint _WaveCount = WaveActiveCountBits(bCondition); \
    \
    uint _WaveBaseOffset = 0; \
    \
    /* First lane performs one atomic on behalf of the wave */ \
    if (WaveIsFirstLane()) \
    { \
        if (_WaveCount > 0) \
        { \
            InterlockedAdd(Dest, _WaveCount * (ValuePerThread), _WaveBaseOffset); \
        } \
    } \
    \
    /* Broadcast the global base offset to all lanes in the wave */ \
    _WaveBaseOffset = WaveReadLaneFirst(_WaveBaseOffset); \
    \
    /* Compute this lane's rank among lanes that satisfy the condition (0, 1, 2...) */ \
    /* WavePrefixCountBits(bCondition) is equivalent to a prefix sum but uses bit ops */ \
    uint _LaneOffset = WavePrefixCountBits(bCondition); \
    \
    /* Final offset = global base offset + (rank * fixed increment) */ \
    OutOffset = _WaveBaseOffset + (_LaneOffset * (ValuePerThread)); \
}

#endif // GHOST_WAVE_HLSL
