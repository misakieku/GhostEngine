using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Helpers;

public static class MatrixHelpers
{
    /// <summary>
    /// Generates a transformation matrix from position, rotation, and scale vectors.
    /// </summary>
    /// <param name="position">Defines the translation component of the transformation matrix.</param>
    /// <param name="rotation">Specifies the orientation of the object in 3D space.</param>
    /// <param name="scale">Determines the size of the object along each axis.</param>
    /// <returns>Returns a transformation matrix that combines the specified position, rotation, and scale.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateTRS(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        return Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
    }
}