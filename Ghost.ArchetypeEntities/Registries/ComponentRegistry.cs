namespace Ghost.Entities.Registries;

internal static class ComponentRegistry
{
    private static readonly Dictionary<Type, ComponentData> _hashCodeToComponentMap = new(64);

    public static unsafe ComponentData GetOrAdd<T>()
        where T : unmanaged, IComponent
    {
        var type = typeof(T);
        if (_hashCodeToComponentMap.TryGetValue(type, out var data))
        {
            return data;
        }

        var id = (ushort)_hashCodeToComponentMap.Count;
        data = new ComponentData(id, sizeof(T));
        _hashCodeToComponentMap.Add(type, data);

        return data;
    }
}