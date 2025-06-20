using System.Runtime.CompilerServices;

namespace Ghost.Engine.Utilities;

public static class MathUtility
{
    public const float RAD_TO_DEG = 180f / MathF.PI;
    public const float DEG_TO_RAD = MathF.PI / 180f;

    /// <summary>
    /// Converts radians to degrees.
    /// </summary>
    /// <param name="radians">The angle in radians to convert.</param>
    /// <returns>The angle in degrees.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float RadToDeg(float radians)
    {
        return radians * RAD_TO_DEG;
    }

    /// <summary>
    /// Converts degrees to radians.
    /// </summary>
    /// <param name="degrees">The angle in degrees to convert.</param>
    /// <returns>The angle in radians.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DegToRad(float degrees)
    {
        return degrees * DEG_TO_RAD;
    }
}