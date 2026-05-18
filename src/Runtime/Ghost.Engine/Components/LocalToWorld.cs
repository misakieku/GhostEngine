using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.Components;

public struct LocalToWorld : IComponentData
{
    public float4x4 matrix;
}