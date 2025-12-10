using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public interface IComponent : IIdentifierType
{
}

public interface IEnableableComponent : IComponent
{
}

public struct ComponentInfo
{
    // public FixedText64 stableName; // Do we actually need this?
    public Identifier<IComponent> id;
    public int size;
    public int alignment;
    public int lastWriteVersion;
    public bool isEnableable;
}

public static class ComponentTypeID<T>
    where T : unmanaged, IComponent
{
    public static readonly Identifier<IComponent> value = ComponentRegister.GetOrRegisterComponent<T>();
}

internal static class ComponentRegister
{
    private static int s_nextComponentTypeID = 0;

    private static readonly List<ComponentInfo> s_registeredComponents = new();
    private static readonly Dictionary<IntPtr, int> s_typeHandleToID = new();
    private static readonly Dictionary<string, int> s_nameToRuntimeID = new();
#if DEBUG || GHOST_EDITOR
    internal static readonly Dictionary<int, IntPtr> s_runtimeIDToTypeHandle = new();
#endif

    public static unsafe Identifier<IComponent> GetOrRegisterComponent<T>()
        where T : unmanaged, IComponent
    {
        var typeHandle = typeof(T).TypeHandle.Value;

        lock (s_registeredComponents)
        {
            if (s_typeHandleToID.TryGetValue(typeHandle, out var existingID))
            {
                return existingID;
            }

            var newID = new Identifier<IComponent>(s_nextComponentTypeID);
            s_nextComponentTypeID++;

            var stableName = typeof(T).FullName ?? typeof(T).Name;

            var info = new ComponentInfo
            {
                // stableName = new FixedText64(stableName),
                id = newID,
                size = sizeof(T),
                alignment = (int)MemoryUtility.AlignOf<T>(),
                isEnableable = typeof(IEnableableComponent).IsAssignableFrom(typeof(T))
            };

            while (s_registeredComponents.Count <= newID.value) s_registeredComponents.Add(default);
            s_registeredComponents[newID.value] = info;

            s_typeHandleToID[typeHandle] = newID;
            s_nameToRuntimeID[stableName] = newID;
#if DEBUG || GHOST_EDITOR
            s_runtimeIDToTypeHandle[newID.value] = typeHandle;
#endif

            return newID;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Identifier<IComponent> GetComponentID(Type type)
    {
        var typeHandle = type.TypeHandle.Value;
        lock (s_registeredComponents)
        {
            if (s_typeHandleToID.TryGetValue(typeHandle, out var existingID))
            {
                return existingID;
            }
        }

        throw new KeyNotFoundException($"Component type {type} is not registered.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComponentInfo GetComponentInfo(Identifier<IComponent> typeId)
    {
        lock (s_registeredComponents)
        {
            return s_registeredComponents[typeId];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetComponentLastWrite(Identifier<IComponent> typeId, int version)
    {
        lock (s_registeredComponents)
        {
            var info = s_registeredComponents[typeId];
            info.lastWriteVersion = version;
            s_registeredComponents[typeId] = info;
        }
    }

    public static int GetHashCode(params ReadOnlySpan<Identifier<IComponent>> componentTypeIDs)
    {
        var largestID = 0;
        foreach (var id in componentTypeIDs)
        {
            if (id.value > largestID)
            {
                largestID = id.value;
            }
        }

        var length = UnsafeBitSet.RequiredLength(largestID + 1);
        var bits = (Span<uint>)stackalloc uint[length];
        bits.Clear();

        var bitSet = new SpanBitSet(bits);
        foreach (var id in componentTypeIDs)
        {
            bitSet.SetBit(id.value);
        }

        return bitSet.GetHashCode();
    }
}
