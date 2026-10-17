using Ghost.Entities;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Volumes;

/// <summary>
/// Unmanaged descriptor for a registered volume component blender.
/// </summary>
public unsafe struct VolumeBlenderRegistration
{
    public ushort typeId;
    public int size;
    public delegate*<void*, void*, float, void> pBlendFunc;
    public delegate*<void*, void> pResetFunc;
}

/// <summary>
/// Central registry of volume component blenders indexed by component type ID.
/// </summary>
public static unsafe class VolumeRegistry
{
    private static VolumeBlenderRegistration[] s_blenders = new VolumeBlenderRegistration[64];
    private static readonly object s_lock = new();

    /// <summary>
    /// Registers a volume component type into the central registry.
    /// </summary>
    /// <typeparam name="T">The volume component type.</typeparam>
    public static void Register<T>()
        where T : unmanaged, IVolumeComponent<T>
    {
        var id = ComponentTypeID<T>.Value.Value;
        lock (s_lock)
        {
            if (id >= s_blenders.Length)
            {
                Array.Resize(ref s_blenders, Math.Max(id + 1, s_blenders.Length * 2));
            }

            s_blenders[id] = new VolumeBlenderRegistration
            {
                typeId = (ushort)id,
                size = sizeof(T),
                pBlendFunc = VolumeBlenderTrampoline<T>.pBlendFunc,
                pResetFunc = VolumeBlenderTrampoline<T>.pResetFunc
            };
        }
    }

    /// <summary>
    /// Attempts to retrieve the blender registration for a component type ID.
    /// </summary>
    /// <param name="typeId">The component type ID.</param>
    /// <param name="registration">The retrieved blender registration.</param>
    /// <returns>True if a blender is registered for this type ID, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetBlender(ushort typeId, out VolumeBlenderRegistration registration)
    {
        if (typeId < s_blenders.Length && s_blenders[typeId].pBlendFunc != null)
        {
            registration = s_blenders[typeId];
            return true;
        }

        registration = default;
        return false;
    }
}
