using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Utilities;

public static class MathUtility
{
    extension(float4x4 matrix)
    {
        /// <summary>
        /// Creates a transformation matrix from position, rotation, and scale vectors.
        /// </summary>
        /// <param name="position">Defines the translation component of the transformation matrix.</param>
        /// <param name="rotation">Specifies the orientation of the object in 3D space.</param>
        /// <param name="scale">Determines the size of the object along each axis.</param>
        /// <returns>Returns a transformation matrix that combines the specified position, rotation, and scale.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 TRS(float3 position, quaternion rotation, float3 scale)
        {
            var R = new float3x3(rotation);
            return new float4x4(
                    R[0][0] * scale.x, R[0][1] * scale.y, R[0][2] * scale.z, position.x,
                    R[1][0] * scale.x, R[1][1] * scale.y, R[1][2] * scale.z, position.y,
                    R[2][0] * scale.x, R[2][1] * scale.y, R[2][2] * scale.z, position.z,
                    0f, 0f, 0f, 1f
                );
        }

        /// <summary>
        /// Gets the translation, rotation, and scale components from a transformation matrix.
        /// </summary>
        /// <param name="position">The position component extracted from the matrix.</param>
        /// <param name="rotation">The rotation component extracted from the matrix as a quaternion.</param>
        /// <param name="scale">The scale component extracted from the matrix.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetTRS(out float3 position, out quaternion rotation, out float3 scale)
        {
            position = matrix.c3.xyz;

            var scaleX = math.length(matrix.c0.xyz);
            var scaleY = math.length(matrix.c1.xyz);
            var scaleZ = math.length(matrix.c2.xyz);
            scale = new float3(scaleX, scaleY, scaleZ);

            var rotationMatrix = new float3x3(
                matrix.c0.x / scale.x, matrix.c0.y / scale.x, matrix.c0.z / scale.x,
                matrix.c1.x / scale.y, matrix.c1.y / scale.y, matrix.c1.z / scale.y,
                matrix.c2.x / scale.z, matrix.c2.y / scale.z, matrix.c2.z / scale.z
            );

            rotation = new quaternion(rotationMatrix);
        }
    }
}
