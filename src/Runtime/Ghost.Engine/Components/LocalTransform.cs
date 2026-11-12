using Ghost.Engine.Utilities;
using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Components;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct LocalTransform : IComponentData
{
    public float3 position;
    public quaternion rotation;
    public float3 scale;

    public static LocalTransform Identity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new() { position = float3.zero, rotation = quaternion.identity, scale = new float3(1.0f) };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LocalTransform FromPosition(float3 position) => new() { position = position, rotation = quaternion.identity, scale = new float3(1.0f) };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LocalTransform FromPositionRotation(float3 position, quaternion rotation) => new() { position = position, rotation = rotation, scale = new float3(1.0f) };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LocalTransform FromPositionRotationScale(float3 position, quaternion rotation, float3 scale) => new() { position = position, rotation = rotation, scale = scale };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float4x4 ToMatrix() => MathUtility.TRS(position, rotation, scale);
}
