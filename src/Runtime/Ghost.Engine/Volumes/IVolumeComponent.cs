using Ghost.Entities;

namespace Ghost.Engine.Volumes;

/// <summary>
/// Defines the contract for an unmanaged volume component capable of being blended across volumes.
/// </summary>
/// <typeparam name="T">The unmanaged volume component struct type.</typeparam>
public interface IVolumeComponent<T> : IComponent
    where T : unmanaged, IVolumeComponent<T>
{
    /// <summary>
    /// Gets the authoritative default value of the component when not overridden by any volume.
    /// </summary>
    static abstract T Default { get; }

    /// <summary>
    /// Blends the source component into the target component using the specified volume weight.
    /// </summary>
    /// <param name="target">The target component being accumulated.</param>
    /// <param name="source">The source component from the volume being evaluated.</param>
    /// <param name="weight">The normalized effective volume weight [0.0 .. 1.0].</param>
    static abstract void Blend(ref T target, in T source, float weight);
}
