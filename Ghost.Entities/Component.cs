using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public interface IComponent
{
}

public interface IEnableableComponent : IComponent
{
}

internal struct ComponentInfo
{
    // public string stableName; // Do we actually need this?
    public int id;
    public int size;
    public int alignment;
    public bool isEnableable;
}

public static class ComponentTypeID<T>
    where T : unmanaged, IComponent
{
    public static readonly Identifier<IComponent> value = ComponentRegister.GetOrRegisterComponent<T>();
}

internal static class ComponentRegister
{
    private static readonly List<ComponentInfo> s_registeredComponents = new();
    private static readonly Dictionary<IntPtr, int> s_typeHandleToID = new();
    private static readonly Dictionary<string, int> s_nameToRuntimeID = new();
#if DEBUG || GHOST_EDITOR
    internal static readonly Dictionary<int, Type> s_runtimeIDToType = new();
#endif

    public static unsafe Identifier<IComponent> GetOrRegisterComponent<T>()
        where T : unmanaged, IComponent
    {
        var type = typeof(T);
        var typeHandle = type.TypeHandle.Value;

        lock (s_registeredComponents)
        {
            if (s_typeHandleToID.TryGetValue(typeHandle, out var existingID))
            {
                return existingID;
            }

            var newID = new Identifier<IComponent>(s_registeredComponents.Count);
            var stableName = typeof(T).FullName ?? typeof(T).Name;
            var info = new ComponentInfo
            {
                // stableName = new FixedText64(stableName),
                id = newID,
                size = sizeof(T),
                alignment = (int)MemoryUtility.AlignOf<T>(),
                isEnableable = typeof(IEnableableComponent).IsAssignableFrom(type),
                // isManaged = typeof(IManagedWrapper).IsAssignableFrom(type),
            };

            s_registeredComponents.Add(info);

            s_typeHandleToID[typeHandle] = newID;
            s_nameToRuntimeID[stableName] = newID;
#if DEBUG || GHOST_EDITOR
            s_runtimeIDToType[newID.value] = typeof(T);
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

        return Identifier<IComponent>.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComponentInfo GetComponentInfo(Identifier<IComponent> typeId)
    {
        lock (s_registeredComponents)
        {
            return s_registeredComponents[typeId];
        }
    }

    // TODO: A ComponentSet structure to cache the hashcode for better performance.
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
