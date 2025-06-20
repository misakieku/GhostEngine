using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Utilities;

public static class MatrixUtility
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

    /// <summary>
    /// Decomposes a transformation matrix into its position, rotation, and scale components.
    /// </summary>
    /// <remarks>This method assumes the input matrix represents a valid affine transformation, including
    /// translation, rotation, and scaling. If the matrix contains skew or other non-standard transformations, the
    /// results may be undefined.</remarks>
    /// <param name="matrix">The <see cref="Matrix4x4"/> to decompose. Must represent a valid transformation matrix.</param>
    /// <param name="position">When the method returns, contains the position component extracted from the matrix.</param>
    /// <param name="rotation">When the method returns, contains the rotation component extracted from the matrix as a <see
    /// cref="Quaternion"/>.</param>
    /// <param name="scale">When the method returns, contains the scale component extracted from the matrix.</param>
    public static void GetTRS(Matrix4x4 matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        position = new(matrix.M41, matrix.M42, matrix.M43);

        var scaleX = new Vector3(matrix.M11, matrix.M12, matrix.M13).Length();
        var scaleY = new Vector3(matrix.M21, matrix.M22, matrix.M23).Length();
        var scaleZ = new Vector3(matrix.M31, matrix.M32, matrix.M33).Length();
        scale = new(scaleX, scaleY, scaleZ);

        Matrix4x4 rotationMatrix = new(
            matrix.M11 / scale.X, matrix.M12 / scale.X, matrix.M13 / scale.X, 0,
            matrix.M21 / scale.Y, matrix.M22 / scale.Y, matrix.M23 / scale.Y, 0,
            matrix.M31 / scale.Z, matrix.M32 / scale.Z, matrix.M33 / scale.Z, 0,
            0, 0, 0, 1);

        rotation = Quaternion.CreateFromRotationMatrix(rotationMatrix);
    }
}