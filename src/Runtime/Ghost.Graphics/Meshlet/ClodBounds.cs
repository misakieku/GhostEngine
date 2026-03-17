using System.Numerics;

namespace Ghost.Graphics.Meshlet;

/// <summary>
/// Represents the bounding sphere and simplification error for a LOD cluster.
/// </summary>
public struct ClodBounds
{
    /// <summary> The center of the bounding sphere. </summary>
    public Vector3 center;
    /// <summary> The radius of the bounding sphere. </summary>
    public float radius;
    /// <summary> The simplification error associated with this LOD level. </summary>
    public float error;
}
