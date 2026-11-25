using Ghost.Core;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Volumes;

/// <summary>
/// Unmanaged descriptor for a registered volume component blender.
/// </summary>
public unsafe struct VolumeBlenderRegistration
{
    public ushort typeId;
    public int size;
    public int offset;
    public delegate*<void*, void*, float, void> pBlendFunc;
    public delegate*<void*, void> pResetFunc;
    public delegate*<void*, uint, in SceneFieldReader, void> pApplyFieldFunc;
    public delegate*<ref UnmanagedTypeMap, void*> pGetOrAddDefaultFunc;
}

/// <summary>
/// Central registry of volume component blenders indexed by component type ID.
/// </summary>
public static unsafe class VolumeRegistry
{
    private static VolumeBlenderRegistration[] s_blenders = new VolumeBlenderRegistration[64];
    private static int s_bytesPerView = 0;
    private static readonly object s_lock = new();

    /// <summary>
    /// Gets the stride in bytes required for one camera view slice to hold all registered volume components.
    /// </summary>
    public static int BytesPerView => s_bytesPerView;

    /// <summary>
    /// Registers a volume component type into the central registry alongside optional AOT serialization callbacks.
    /// </summary>
    /// <typeparam name="T">The volume component type.</typeparam>
    public static void Register<T>(delegate*<void*, uint, in SceneFieldReader, void> pApplyFieldFunc = null, delegate*<ref UnmanagedTypeMap, void*> pGetOrAddDefaultFunc = null)
        where T : unmanaged, IVolumeComponent<T>
    {
        var id = ComponentTypeID<T>.Value.Value;
        lock (s_lock)
        {
            if (id >= s_blenders.Length)
            {
                Array.Resize(ref s_blenders, Math.Max(id + 1, s_blenders.Length * 2));
            }

            if (s_blenders[id].pBlendFunc != null)
            {
                if (pApplyFieldFunc != null)
                {
                    s_blenders[id].pApplyFieldFunc = pApplyFieldFunc;
                }

                if (pGetOrAddDefaultFunc != null)
                {
                    s_blenders[id].pGetOrAddDefaultFunc = pGetOrAddDefaultFunc;
                }

                return;
            }

            var align = MemoryUtility.AlignOf<T>();
            var offset = (int)MemoryUtility.AlignUp((nuint)s_bytesPerView, align);
            s_bytesPerView = (int)MemoryUtility.AlignUp((nuint)(offset + sizeof(T)), 16);

            s_blenders[id] = new VolumeBlenderRegistration
            {
                typeId = (ushort)id,
                size = sizeof(T),
                offset = offset,
                pBlendFunc = VolumeBlenderTrampoline<T>.pBlendFunc,
                pResetFunc = VolumeBlenderTrampoline<T>.pResetFunc,
                pApplyFieldFunc = pApplyFieldFunc,
                pGetOrAddDefaultFunc = pGetOrAddDefaultFunc
            };
        }
    }

    /// <summary>
    /// Gets the byte offset of the specified volume component type within a per-view data slice.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOffset(ushort typeId)
    {
        return s_blenders[typeId].offset;
    }

    /// <summary>
    /// Gets the byte offset of the specified volume component type within a per-view data slice.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOffset<T>()
        where T : unmanaged, IVolumeComponent<T>
    {
        return s_blenders[VolumeBlenderTrampoline<T>.TypeId].offset;
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

    /// <summary>
    /// Attempts to retrieve the blender registration for a registered volume component full type name.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetBlenderByName(string fullName, out VolumeBlenderRegistration registration)
    {
        var compId = ComponentRegistry.GetComponentIDByName(fullName);
        if (compId.IsInvalid)
        {
            registration = default;
            return false;
        }

        return TryGetBlender((ushort)compId.Value, out registration);
    }

    /// <summary>
    /// Ensures a default-initialized entry of volume component <paramref name="typeId"/> exists in <paramref name="storage"/> and returns its pointer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetOrAddDefault(ushort typeId, ref UnmanagedTypeMap storage, out void* ptr)
    {
        if (typeId < s_blenders.Length && s_blenders[typeId].pGetOrAddDefaultFunc != null)
        {
            ptr = s_blenders[typeId].pGetOrAddDefaultFunc(ref storage);
            return ptr != null;
        }

        ptr = null;
        return false;
    }

    /// <summary>
    /// Applies a single baked field override to the unmanaged volume struct at <paramref name="componentPtr"/> using AOT-generated code.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryApplySerializedField(ushort typeId, void* componentPtr, uint fieldNameHash, in SceneFieldReader reader)
    {
        if (typeId < s_blenders.Length && s_blenders[typeId].pApplyFieldFunc != null && componentPtr != null)
        {
            s_blenders[typeId].pApplyFieldFunc(componentPtr, fieldNameHash, in reader);
            return true;
        }

        return false;
    }
}
