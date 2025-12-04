using Ghost.Engine.Utilities;
using Ghost.SparseEntities.Components;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Components;

[SkipLocalsInit]
public struct LocalToWorld : IComponentData
{
    public Matrix4x4 matrix;

    public static LocalToWorld Identity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new()
        {
            matrix = MatrixUtility.CreateTRS(Vector3.Zero, Quaternion.Identity, Vector3.One)
        };
    }
}