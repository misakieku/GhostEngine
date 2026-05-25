using Ghost.Editor.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ghost.Editor.Core.Inspector;

/// <summary>
/// Registry mapping ECS component types to their custom UI editor types.
/// </summary>
public static class ComponentEditorRegistry
{
    private static readonly Dictionary<Type, Type> s_editors = new();

    static ComponentEditorRegistry()
    {
        var editorTypes = TypeCache.GetTypesWithAttribute<CustomEditorAttribute>();
        foreach (var editorType in editorTypes)
        {
            var attr = editorType.GetCustomAttribute<CustomEditorAttribute>();
            if (attr != null && attr.TargetType != null)
            {
                if (typeof(ComponentEditor).IsAssignableFrom(editorType))
                {
                    s_editors[attr.TargetType] = editorType;
                }
            }
        }
    }

    /// <summary>
    /// Checks if a custom editor exists for the given component type.
    /// </summary>
    public static bool HasCustomEditor(Type componentType)
    {
        return s_editors.ContainsKey(componentType);
    }

    /// <summary>
    /// Instantiates the custom editor for the given component type, or null if none exists.
    /// </summary>
    public static ComponentEditor? CreateCustomEditor(Type componentType)
    {
        if (s_editors.TryGetValue(componentType, out var editorType))
        {
            return (ComponentEditor?)Activator.CreateInstance(editorType);
        }
        return null;
    }
}
