using Ghost.Core;
using Ghost.Entities;

namespace Ghost.Editor.Core.Inspector;

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
#if DEBUG || GHOST_EDITOR
        if (ComponentRegistry.s_runtimeIDToType.TryGetValue(componentId.Value, out var type))
        {
            return GetOrCreate(type);
        }
#endif
        throw new InvalidOperationException($"Cannot resolve ComponentDescriptor for component ID {componentId.Value}. Type mapping not available.");
    }
}
