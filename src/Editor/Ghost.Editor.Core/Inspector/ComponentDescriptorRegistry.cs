using Ghost.Core;
using Ghost.Entities;

namespace Ghost.Editor.Core.Inspector;

// TODO: We can use source generator to directly generate ComponentDescriptor on each component type and avoid reflection and caching altogether. This is just a quick solution for now.

/// <summary>
/// Thread-safe cache of ComponentDescriptor per component type.
/// </summary>
public static class ComponentDescriptorRegistry
{
    private static readonly Dictionary<nint, ComponentDescriptor> s_cache = new();
    private static readonly Lock s_lock = new();

    public static ComponentDescriptor GetOrCreate(Type componentType)
    {
        var handle = componentType.TypeHandle.Value;

        lock (s_lock)
        {
            if (s_cache.TryGetValue(handle, out var descriptor))
            {
                return descriptor;
            }

            descriptor = ComponentDescriptor.Create(componentType);
            s_cache[handle] = descriptor;
            return descriptor;
        }
    }

    public static ComponentDescriptor GetOrCreate(Identifier<IComponent> componentId)
    {
        return GetOrCreate(ComponentRegistry.s_runtimeIDToType[componentId.Value]);
    }
}
