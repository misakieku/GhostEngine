using Ghost.Entities;
using Ghost.Graphics.Core;

namespace Ghost.Engine.Components;

/// <summary>
/// Shape of the volume bounding volume for local spatial evaluation.
/// </summary>
public enum VolumeShape : byte
{
    Box,
    Sphere
}

/// <summary>
/// Component defining volume priority, layer mask, global flag, and falloff blend distance.
/// </summary>
public struct RenderVolume : IComponentData
{
    /// <summary>
    /// Layer mask used to filter which cameras are affected by this volume.
    /// </summary>
    public RenderingLayerMask renderingLayerMask;

    /// <summary>
    /// Priority of the volume. Volumes with higher priority override lower-priority volumes.
    /// </summary>
    public float priority;

    /// <summary>
    /// Master weight multiplier [0.0 .. 1.0].
    /// </summary>
    public float weight;

    /// <summary>
    /// Outer falloff distance for local volumes. Distance inside this zone smoothly transitions from 0 to weight.
    /// </summary>
    public float blendDistance;

    /// <summary>
    /// If true, this volume affects the camera globally regardless of spatial position or bounds.
    /// </summary>
    public bool isGlobal;

    /// <summary>
    /// The shape used for local volume bounds testing (Box or Sphere).
    /// </summary>
    public VolumeShape shape;

    public static RenderVolume DefaultGlobal => new()
    {
        renderingLayerMask = RenderingLayerMask.All,
        priority = 0.0f,
        weight = 1.0f,
        blendDistance = 0.0f,
        isGlobal = true,
        shape = VolumeShape.Box
    };
}
