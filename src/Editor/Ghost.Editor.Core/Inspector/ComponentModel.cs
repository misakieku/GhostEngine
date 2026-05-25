using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ghost.Editor.Core.Inspector;

/// <summary>
/// Model for a single component on an entity.
/// Wraps a ComponentDescriptor and provides live sync.
/// </summary>
public sealed class ComponentModel
{
    private byte[]? _previousData;
    private bool _isFirstSync = true;

    public ComponentDescriptor Descriptor { get; }
    public IReadOnlyList<IPropertyModel> Properties { get; }

    internal ComponentModel(ComponentDescriptor descriptor)
    {
        Descriptor = descriptor;
        
        var properties = new List<IPropertyModel>(descriptor.Properties.Length);
        foreach (var propDesc in descriptor.Properties)
        {
            properties.Add(BuildPropertyModel(propDesc));
        }
        Properties = properties;
    }

    private static IPropertyModel BuildPropertyModel(PropertyDescriptor desc)
    {
        IPropertyModel[]? children = null;
        if (desc.Children != null)
        {
            children = new IPropertyModel[desc.Children.Length];
            for (int i = 0; i < desc.Children.Length; i++)
            {
                children[i] = BuildPropertyModel(desc.Children[i]);
            }
        }
        return desc.ModelFactory(desc, children);
    }

    /// <summary>
    /// Sync all field values from ECS memory into PropertyModels.
    /// Returns true if any value actually changed.
    /// </summary>
    public unsafe void SyncFromECS(void* pComponentData)
    {
        if (Descriptor.Size == 0)
        {
            return;
        }

        if (_previousData == null)
        {
            _previousData = new byte[Descriptor.Size];
        }

        fixed (byte* pPrev = _previousData)
        {
            if (!_isFirstSync)
            {
                var prevSpan = new System.ReadOnlySpan<byte>(pPrev, Descriptor.Size);
                var currSpan = new System.ReadOnlySpan<byte>(pComponentData, Descriptor.Size);

                if (prevSpan.SequenceEqual(currSpan))
                {
                    // No memory change, skip all reflection and boxing!
                    return;
                }
            }

            // Memory changed (or first sync), copy current data to cache
            Unsafe.CopyBlock(pPrev, pComponentData, (uint)Descriptor.Size);
            _isFirstSync = false;
        }

        foreach (var prop in Properties)
        {
            prop.SyncFromECS(pComponentData);
        }
    }

    private static unsafe void SyncPropertyFromECS(IPropertyModel prop, void* pComponentData)
    {
        prop.SyncFromECS(pComponentData);

        if (prop.Children != null)
        {
            foreach (var child in prop.Children)
            {
                SyncPropertyFromECS(child, pComponentData);
            }
        }
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

        if (_previousData != null && Descriptor.Size > 0)
        {
            var newData = new ReadOnlySpan<byte>(pComponentData, Descriptor.Size);
            newData.CopyTo(_previousData);
        }
    }

    private static unsafe void FlushPropertyToECS(IPropertyModel prop, void* pComponentData)
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
