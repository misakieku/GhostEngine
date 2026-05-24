using System.Collections.Generic;

namespace Ghost.Editor.Core.Inspector;

/// <summary>
/// Model for a single component on an entity.
/// Wraps a ComponentDescriptor and provides live sync.
/// </summary>
public sealed class ComponentModel
{
    public ComponentDescriptor Descriptor { get; }
    public IReadOnlyList<PropertyModel> Properties { get; }

    internal ComponentModel(ComponentDescriptor descriptor)
    {
        Descriptor = descriptor;
        
        var properties = new List<PropertyModel>(descriptor.Properties.Length);
        foreach (var propDesc in descriptor.Properties)
        {
            properties.Add(BuildPropertyModel(propDesc));
        }
        Properties = properties;
    }

    private PropertyModel BuildPropertyModel(PropertyDescriptor desc)
    {
        PropertyModel[]? children = null;
        if (desc.Children != null)
        {
            children = new PropertyModel[desc.Children.Length];
            for (int i = 0; i < desc.Children.Length; i++)
            {
                children[i] = BuildPropertyModel(desc.Children[i]);
            }
        }
        return new PropertyModel(desc, children);
    }

    /// <summary>
    /// Sync all field values from ECS memory into PropertyModels.
    /// Returns true if any value actually changed.
    /// </summary>
    public unsafe bool SyncFromECS(void* pComponentData)
    {
        var changed = false;
        foreach (var prop in Properties)
        {
            changed |= SyncPropertyFromECS(prop, pComponentData);
        }
        return changed;
    }

    private unsafe bool SyncPropertyFromECS(PropertyModel prop, void* pComponentData)
    {
        var changed = false;
        var newValue = prop.Descriptor.ReadBoxed(pComponentData);
        
        if (!Equals(prop.Value, newValue))
        {
            prop.SetValueFromECS(newValue);
            changed = true;
        }

        if (prop.Children != null)
        {
            foreach (var child in prop.Children)
            {
                changed |= SyncPropertyFromECS(child, pComponentData);
            }
        }
        
        return changed;
    }

    /// <summary>
    /// Flush all dirty properties back to ECS.
    /// </summary>
    public unsafe void FlushToECS(void* pComponentData)
    {
        foreach (var prop in Properties)
        {
            FlushPropertyToECS(prop, pComponentData);
        }
    }

    private unsafe void FlushPropertyToECS(PropertyModel prop, void* pComponentData)
    {
        prop.FlushToECS(pComponentData);
        if (prop.Children != null)
        {
            foreach (var child in prop.Children)
            {
                FlushPropertyToECS(child, pComponentData);
            }
        }
    }
}
