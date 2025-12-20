using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Components;

[SkipLocalsInit]
public struct LocalToWorld : IComponent
{
    public float4x4 matrix;
}