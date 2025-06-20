using Ghost.Entities;
using System.Reflection;

namespace Ghost.Editor.Utilities;

public static class TypeCache
{
    private static readonly TypeInfo[] _types;

    static TypeCache()
    {
        _types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.DefinedTypes)
            .ToArray();
    }

    public static Type[] GetTypes()
    {
        return _types;
    }
}

public static class ComponentTypeCache
{
    private static readonly Type?[][] _componentTypes;

    static ComponentTypeCache()
    {
        _componentTypes = new Type[World.WorldCount][];
        for (var i = 0; i < World.WorldCount; i++)
        {
            var world = World.GetWorld(i);
            var typeHandles = world.ComponentStorage.ComponentPools.Keys;
            _componentTypes[i] = typeHandles.Select(handle => Type.GetTypeFromHandle(RuntimeTypeHandle.FromIntPtr(handle))).ToArray();
        }
    }

    public static Type?[] GetComponentTypes(int worldIndex)
    {
        if (worldIndex < 0 || worldIndex >= _componentTypes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(worldIndex), "Invalid world index.");
        }
        return _componentTypes[worldIndex];
    }
}