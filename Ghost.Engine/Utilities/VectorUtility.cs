using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Utilities;

public static class VectorUtility
{
    /// <summary>
    /// Converts a Vector3 representing Euler angles (in degrees) to a Quaternion.
    /// </summary>
    /// <param name="v">The Vector3 containing Euler angles (X: Pitch, Y: Yaw, Z: Roll) in degrees.</param>
    /// <returns>A Quaternion representing the rotation defined by the Euler angles.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion ToQuaternion(this Vector3 v)
    {
        return Quaternion.CreateFromYawPitchRoll(MathUtility.DegToRad(v.Y), MathUtility.DegToRad(v.X), MathUtility.DegToRad(v.Z));
    }

    public static Vector3 CreateFromQuaternion(Quaternion quaternion)
    {
        // Convert quaternion to Euler angles (Yaw, Pitch, Roll)
        quaternion = Quaternion.Normalize(quaternion);

        // Extract pitch (X), yaw (Y), roll (Z)
        var ysqr = quaternion.Y * quaternion.Y;

        // Pitch (X-axis rotation)
        var t0 = +2.0 * (quaternion.W * quaternion.X + quaternion.Y * quaternion.Z);
        var t1 = +1.0 - 2.0 * (quaternion.X * quaternion.X + ysqr);
        var pitch = Math.Atan2(t0, t1);

        // Yaw (Y-axis rotation)
        var t2 = +2.0 * (quaternion.W * quaternion.Y - quaternion.Z * quaternion.X);
        t2 = Math.Clamp(t2, -1.0, 1.0);
        var yaw = Math.Asin(t2);

        // Roll (Z-axis rotation)
        var t3 = +2.0 * (quaternion.W * quaternion.Z + quaternion.X * quaternion.Y);
        var t4 = +1.0 - 2.0 * (ysqr + quaternion.Z * quaternion.Z);
        var roll = Math.Atan2(t3, t4);

        const float radToDeg = 180f / MathF.PI;
        return new Vector3((float)pitch, (float)yaw, (float)roll) * radToDeg;
    }
}