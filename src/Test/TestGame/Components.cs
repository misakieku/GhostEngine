using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;

namespace TestGame;

internal struct MoveDst : IComponentData
{
    public float3 position;
    public float3 lookAt;
    public float3 range;
    public bool updateRotation;
}