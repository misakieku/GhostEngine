using Ghost.Core;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using Misaki.HighPerformance.Utilities;
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
                    // Row 0: Right.x, Up.x, Forward.x, Pos.x
                    R[0][0] * scale.x, R[1][0] * scale.y, R[2][0] * scale.z, position.x,
                    // Row 1: Right.y, Up.y, Forward.y, Pos.y
                    R[0][1] * scale.x, R[1][1] * scale.y, R[2][1] * scale.z, position.y,
                    // Row 2: Right.z, Up.z, Forward.z, Pos.z
                    R[0][2] * scale.x, R[1][2] * scale.y, R[2][2] * scale.z, position.z,
                    // Row 3: 0, 0, 0, 1
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

            var col0 = scaleX > 1e-6f ? matrix.c0.xyz / scaleX : new float3(1.0f, 0.0f, 0.0f);
            var col1 = scaleY > 1e-6f ? matrix.c1.xyz / scaleY : new float3(0.0f, 1.0f, 0.0f);
            var col2 = scaleZ > 1e-6f ? matrix.c2.xyz / scaleZ : new float3(0.0f, 0.0f, 1.0f);

            var rotationMatrix = new float3x3(col0, col1, col2);
            rotation = new quaternion(rotationMatrix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetTranslation()
        {
            return matrix.c3.xyz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetScale()
        {
            var scaleX = math.length(matrix.c0.xyz);
            var scaleY = math.length(matrix.c1.xyz);
            var scaleZ = math.length(matrix.c2.xyz);
            return new float3(scaleX, scaleY, scaleZ);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion GetRotation()
        {
            var scale = matrix.GetScale();
            var rotationMatrix = new float3x3(matrix.c0.xyz / scale.x, matrix.c1.xyz / scale.y, matrix.c2.xyz / scale.z);
            return new quaternion(rotationMatrix);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SphereIntersectFrustum(float3 center, float radius, ReadOnlySpan<float4> planes)
    {
        Logger.DebugAssert(planes.Length == 6, "Frustum planes must contain exactly 6 planes.");

        for (var i = 0; i < 6; ++i)
        {
            var distance = math.dot(planes.GetElementUnsafe(i).xyz, center) + planes.GetElementUnsafe(i).w;
            if (distance < -radius)
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AABB TransformAABB(in AABB localBounds, in float4x4 matrix)
    {
        var center = (localBounds.Min + localBounds.Max) * 0.5f;
        var extents = (localBounds.Max - localBounds.Min) * 0.5f;

        var worldCenter = math.mul(matrix, new float4(center, 1.0f)).xyz;
        var worldExtents = new float3(
            math.abs(matrix.c0.x) * extents.x + math.abs(matrix.c1.x) * extents.y + math.abs(matrix.c2.x) * extents.z,
            math.abs(matrix.c0.y) * extents.x + math.abs(matrix.c1.y) * extents.y + math.abs(matrix.c2.y) * extents.z,
            math.abs(matrix.c0.z) * extents.x + math.abs(matrix.c1.z) * extents.y + math.abs(matrix.c2.z) * extents.z
        );

        return new AABB(worldCenter - worldExtents, worldCenter + worldExtents);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AABBIntersectFrustum(in AABB bounds, ReadOnlySpan<float4> planes)
    {
        Logger.DebugAssert(planes.Length == 6, "Frustum planes must contain exactly 6 planes.");

        for (var i = 0; i < 6; ++i)
        {
            ref readonly var plane = ref planes.GetElementUnsafe(i);
            var p = new float3(
                plane.x >= 0.0f ? bounds.Max.x : bounds.Min.x,
                plane.y >= 0.0f ? bounds.Max.y : bounds.Min.y,
                plane.z >= 0.0f ? bounds.Max.z : bounds.Min.z
            );

            if (math.dot(plane.xyz, p) + plane.w < 0.0f)
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SphereIntersectsAABB(float3 center, float radius, in AABB bounds)
    {
        var closest = math.clamp(center, bounds.Min, bounds.Max);
        return math.lengthsq(center - closest) <= radius * radius;
    }
}
