using Ghost.Entities;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Volumes;

/// <summary>
/// Provides internal function pointers and metadata for unmanaged volume component blending.
/// </summary>
/// <typeparam name="T">The unmanaged volume component struct type.</typeparam>
internal static unsafe class VolumeBlenderTrampoline<T>
    where T : unmanaged, IVolumeComponent<T>
{
    public static readonly T CachedDefault = T.Default;
    public static readonly delegate*<void*, void*, float, void> pBlendFunc = &RawBlend;
    public static readonly delegate*<void*, void> pResetFunc = &RawReset;
    public static readonly ushort TypeId = (ushort)ComponentTypeID<T>.Value.Value;
    public static readonly int Size = sizeof(T);

    static VolumeBlenderTrampoline()
    {
        VolumeRegistry.Register<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureInitialized()
    {
        // Touching this ensures the static constructor has executed.
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RawBlend(void* target, void* source, float weight)
    {
        if (weight <= 0.0f)
        {
            return;
        }

        ref var targetRef = ref *(T*)target;
        ref readonly var sourceRef = ref *(T*)source;
        T.Blend(ref targetRef, in sourceRef, weight);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RawReset(void* target)
    {
        *(T*)target = T.Default;
    }
}
