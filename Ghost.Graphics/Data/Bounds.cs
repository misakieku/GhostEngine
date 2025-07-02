using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Data;

public struct Bounds : IEquatable<Bounds>
{
    /// <summary>
    /// The minimum point contained by the AABB.
    /// </summary>
    /// <remarks>
    /// If any component of <see cref="Min"/> is greater than <see cref="Max"/> then this AABB is invalid.
    /// </remarks>
    /// <seealso cref="IsValid"/>
    public Vector3 Min
    {
        get;
        set;
    }

    /// <summary>
    /// The maximum point contained by the AABB.
    /// </summary>
    /// <remarks>
    /// If any component of <see cref="Max"/> is less than <see cref="Min"/> then this AABB is invalid.
    /// </remarks>
    /// <seealso cref="IsValid"/>
    public Vector3 Max
    {
        get;
        set;
    }

    /// <summary>
    /// Constructs the AABB with the given minimum and maximum.
    /// </summary>
    /// <remarks>
    /// If you have a center and extents, you can call <see cref="CreateFromCenterAndExtents"/> or <see cref="CreateFromCenterAndHalfExtents"/>
    /// to create the AABB.
    /// </remarks>
    /// <param name="min">Minimum point inside AABB.</param>
    /// <param name="max">Maximum point inside AABB.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bounds(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Creates the AABB from a center and extents.
    /// </summary>
    /// <remarks>
    /// This function takes full extents. It is the distance between <see cref="Min"/> and <see cref="Max"/>.
    /// If you have half extents, you can call <see cref="CreateFromCenterAndHalfExtents"/>.
    /// </remarks>
    /// <param name="center">Center of AABB.</param>
    /// <param name="extents">Full extents of AABB.</param>
    /// <returns>AABB created from inputs.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bounds CreateFromCenterAndExtents(Vector3 center, Vector3 extents)
    {
        return CreateFromCenterAndHalfExtents(center, extents * 0.5f);
    }

    /// <summary>
    /// Creates the AABB from a center and half extents.
    /// </summary>
    /// <remarks>
    /// This function takes half extents. It is half the distance between <see cref="Min"/> and <see cref="Max"/>.
    /// If you have full extents, you can call <see cref="CreateFromCenterAndExtents"/>.
    /// </remarks>
    /// <param name="center">Center of AABB.</param>
    /// <param name="halfExtents">Half extents of AABB.</param>
    /// <returns>AABB created from inputs.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bounds CreateFromCenterAndHalfExtents(Vector3 center, Vector3 halfExtents)
    {
        return new Bounds(center - halfExtents, center + halfExtents);
    }

    /// <summary>
    /// Creates a new AABB with zero extents, centered at the origin.
    /// </summary>
    public static Bounds Zero
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return new Bounds(Vector3.Zero, Vector3.Zero);
        }
    }

    /// <summary>
    /// Computes the extents of the AABB.
    /// </summary>
    /// <remarks>
    /// Extents is the componentwise distance between min and max.
    /// </remarks>
    public readonly Vector3 Extents => Max - Min;

    /// <summary>
    /// Computes the half extents of the AABB.
    /// </summary>
    /// <remarks>
    /// HalfExtents is half of the componentwise distance between min and max. Subtracting HalfExtents from Center
    /// gives Min and adding HalfExtents to Center gives Max.
    /// </remarks>
    public readonly Vector3 HalfExtents => (Max - Min) * 0.5f;

    /// <summary>
    /// Computes the center of the AABB.
    /// </summary>
    public readonly Vector3 Center => (Max + Min) * 0.5f;

    /// <summary>
    /// Check if the AABB is valid.
    /// </summary>
    /// <remarks>
    /// An AABB is considered valid if <see cref="Min"/> is componentwise less than or equal to <see cref="Max"/>.
    /// </remarks>
    /// <returns>True if <see cref="Min"/> is componentwise less than or equal to <see cref="Max"/>.</returns>
    public readonly bool IsValid => Vector3.Dot(Min, Min) <= Vector3.Dot(Max, Max);

    /// <summary>
    /// Computes the surface area for this axis aligned bounding box.
    /// </summary>
    public readonly float SurfaceArea
    {
        get
        {
            var diff = Max - Min;
            return 2 * Vector3.Dot(diff, new Vector3(diff.Y, diff.Z, diff.X));
        }
    }

    /// <summary>
    /// Tests if the input point is contained by the AABB.
    /// </summary>
    /// <param name="point">Point to test.</param>
    /// <returns>True if AABB contains the input point.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(Vector3 point) => Vector3.Dot(point, point) >= Vector3.Dot(Min, Min) && Vector3.Dot(point, point) <= Vector3.Dot(Max, Max);

    /// <summary>
    /// Tests if the input AABB is contained entirely by this AABB.
    /// </summary>
    /// <param name="aabb">AABB to test.</param>
    /// <returns>True if input AABB is contained entirely by this AABB.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(Bounds aabb) => Vector3.Dot(Min, Min) <= Vector3.Dot(aabb.Min, aabb.Min) && Vector3.Dot(Max, Max) >= Vector3.Dot(aabb.Max, aabb.Max);

    /// <summary>
    /// Tests if the input AABB overlaps this AABB.
    /// </summary>
    /// <param name="aabb">AABB to test.</param>
    /// <returns>True if input AABB overlaps with this AABB.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Overlaps(Bounds aabb)
    {
        return Vector3.Dot(Max, Max) >= Vector3.Dot(aabb.Min, aabb.Min) && Vector3.Dot(Min, Min) <= Vector3.Dot(aabb.Max, aabb.Max);
    }

    /// <summary>
    /// Expands the AABB by the given signed distance.
    /// </summary>
    /// <remarks>
    /// Positive distance expands the AABB while negative distance shrinks the AABB.
    /// </remarks>
    /// <param name="signedDistance">Signed distance to expand the AABB with.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Expand(float signedDistance)
    {
        Min -= new Vector3(signedDistance);
        Max += new Vector3(signedDistance);
    }

    /// <summary>
    /// Encapsulates the given AABB.
    /// </summary>
    /// <remarks>
    /// Modifies this AABB so that it contains the given AABB. If the given AABB is already contained by this AABB,
    /// then this AABB doesn't change.
    /// </remarks>
    /// <seealso cref="Contains(Unity.Mathematics.Geometry.MinMaxAABB)"/>
    /// <param name="aabb">AABB to encapsulate.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encapsulate(Bounds aabb)
    {
        Min = Vector3.Min(Min, aabb.Min);
        Max = Vector3.Max(Max, aabb.Max);
    }

    /// <summary>
    /// Encapsulate the given point.
    /// </summary>
    /// <remarks>
    /// Modifies this AABB so that it contains the given point. If the given point is already contained by this AABB,
    /// then this AABB doesn't change.
    /// </remarks>
    /// <seealso cref="Contains(Unity.Mathematics.float3)"/>
    /// <param name="point">Point to encapsulate.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encapsulate(Vector3 point)
    {
        Min = Vector3.Min(Min, point);
        Max = Vector3.Max(Max, point);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Bounds other)
    {
        return Min.Equals(other.Min) && Max.Equals(other.Max);
    }

    public override bool Equals(object? obj)
    {
        if (obj is Bounds bounds)
        {
            return Equals(bounds);
        }

        return false;
    }
    public static bool operator ==(Bounds left, Bounds right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Bounds left, Bounds right)
    {
        return !(left == right);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Min.GetHashCode();
            hash = hash * 31 + Max.GetHashCode();
            return hash;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly override string ToString()
    {
        return string.Format("Bounds({0}, {1})", Min, Max);
    }
}