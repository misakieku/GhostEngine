using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Components;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct LocalToWorld : IComponentData
{
    public float4x4 matrix;
}