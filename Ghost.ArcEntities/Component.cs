using Misaki.HighPerformance.LowLevel.Utilities;

namespace Ghost.ArcEntities;

public struct ComponentInfo
{
    // public FixedText64 stableName; // Do we actually need this?
    public int size;
    public int alignment;
    public int id;
}

internal static unsafe class ComponentTypeID<T>
    where T : unmanaged
{
    public static readonly int value = ComponentRegister.GetOrRegisterComponent<T>();
}

internal static class ComponentRegister
{
    private static int s_nextComponentTypeID = 0;
    private static Dictionary<IntPtr, int> s_typeHandleToID = new();

    internal static List<ComponentInfo> s_registeredComponents = new();
    internal static Dictionary<string, int> s_nameToRuntimeID = new();

    internal unsafe static int GetOrRegisterComponent<T>()
        where T : unmanaged
    {
        var typeHandle = typeof(T).TypeHandle.Value;

        lock (s_registeredComponents)
        {
            if (s_typeHandleToID.TryGetValue(typeHandle, out int existingID))
            {
                return existingID;
            }

            int newID = s_nextComponentTypeID++;
            string stableName = typeof(T).FullName ?? typeof(T).Name;

            var info = new ComponentInfo
            {
                // stableName = new FixedText64(stableName),
                size = sizeof(T),
                alignment = (int)MemoryUtility.AlignOf<T>(),
                id = newID,
            };

            while (s_registeredComponents.Count <= newID) s_registeredComponents.Add(default);
            s_registeredComponents[newID] = info;

            s_typeHandleToID[typeHandle] = newID;
            s_nameToRuntimeID[stableName] = newID;

            return newID;
        }
    }

    internal static ComponentInfo GetComponentInfo(int typeId)
    {
        return s_registeredComponents[typeId];
    }
}
