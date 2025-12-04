using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;

namespace Ghost.Entities;

public interface IComponent : IIdentifierType
{
}

public struct ComponentInfo
{
    // public FixedText64 stableName; // Do we actually need this?
    public int size;
    public int alignment;
    public Identifier<IComponent> id;
}

public static class ComponentTypeID<T>
    where T : unmanaged, IComponent
{
    public static readonly Identifier<IComponent> value = ComponentRegister.GetOrRegisterComponent<T>();
}

internal static class ComponentRegister
{
    private static int s_nextComponentTypeID = 0;
    private static Dictionary<IntPtr, Identifier<IComponent>> s_typeHandleToID = new();

    private static List<ComponentInfo> s_registeredComponents = new();
    private static Dictionary<string, Identifier<IComponent>> s_nameToRuntimeID = new();

    public unsafe static Identifier<IComponent> GetOrRegisterComponent<T>()
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
                size = sizeof(T),
                alignment = (int)MemoryUtility.AlignOf<T>(),
                id = newID,
            };

            while (s_registeredComponents.Count <= newID.value) s_registeredComponents.Add(default);
            s_registeredComponents[newID.value] = info;

            s_typeHandleToID[typeHandle] = newID;
            s_nameToRuntimeID[stableName] = newID;

            return newID;
        }
    }

    public static ComponentInfo GetComponentInfo(Identifier<IComponent> typeId)
    {
        return s_registeredComponents[typeId];
    }

    public static int GetHashCode(ReadOnlySpan<Identifier<IComponent>> componentTypeIDs)
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
