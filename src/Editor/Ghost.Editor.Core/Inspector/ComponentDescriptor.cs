using Ghost.Core;
using Ghost.Core.Attributes;
using Ghost.Entities;
using System.Reflection;

namespace Ghost.Editor.Core.Inspector;

/// <summary>
/// Metadata for an entire ECS component type, including all its editable fields.
/// </summary>
public sealed class ComponentDescriptor
{
    public Type ComponentType { get; }
    public Identifier<IComponent> ComponentId { get; }
    public string DisplayName { get; }
    public int Size { get; }
    public bool IsShared { get; }
    public PropertyDescriptor[] Properties { get; }

    private ComponentDescriptor(Type componentType, Identifier<IComponent> componentId, string displayName, int size, bool isShared, PropertyDescriptor[] properties)
    {
        ComponentType = componentType;
        ComponentId = componentId;
        DisplayName = displayName;
        Size = size;
        IsShared = isShared;
        Properties = properties;
    }

    public static ComponentDescriptor Create(Type componentType)
    {
        var componentId = ComponentRegistry.GetComponentID(componentType);
        var info = ComponentRegistry.GetComponentInfo(componentId);

        var nameAttr = componentType.GetCustomAttribute<InspectorNameAttribute>();
        var displayName = nameAttr?.Name ?? componentType.Name;

        var properties = new List<PropertyDescriptor>();
        var fields = componentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (field.GetCustomAttribute<HideInInspectorAttribute>() != null)
            {
                continue;
            }

            // TODO: Exclude internal/private fields unless they have a specific attribute, but for now we just show public or specifically included.
            if (!field.IsPublic && field.GetCustomAttribute<InspectorNameAttribute>() == null)
            {
                // In GhostEngine we often use public fields for component data, or private fields with [InspectorName].
                // We'll just include public fields by default, and any non-public with specific attributes.
                if (field.GetCustomAttribute<ReadOnlyInInspectorAttribute>() == null &&
                    field.GetCustomAttribute<InspectorGroupAttribute>() == null)
                {
                    continue; // Skip normal private fields
                }
            }

            properties.Add(new PropertyDescriptor(field, 0));
        }

        return new ComponentDescriptor(componentType, componentId, displayName, info.size, info.isShared, properties.ToArray());
    }
}
