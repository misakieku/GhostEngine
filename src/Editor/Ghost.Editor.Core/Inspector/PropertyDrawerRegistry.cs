using Ghost.Core;
using Ghost.Editor.Core.Inspector.Drawers;
using Ghost.Editor.Core.Utilities;
using Ghost.Editor.Core.Controls;
using Misaki.HighPerformance.Mathematics;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ghost.Editor.Core.Inspector;

/// <summary>
/// Discovers PropertyDrawer subclasses and maps field types to drawers.
/// </summary>
public static class PropertyDrawerRegistry
{
    private static readonly Dictionary<Type, PropertyDrawer> s_drawers = new();
    private static bool s_initialized;
    private static readonly Lock s_lock = new();

    public static void Initialize()
    {
        lock (s_lock)
        {
            if (s_initialized) return;

            // Register built-in drawers
            s_drawers[typeof(float)] = new NumberBoxDrawer(fractionDigits: 3);
            s_drawers[typeof(int)] = new NumberBoxDrawer(fractionDigits: 0);
            s_drawers[typeof(double)] = new NumberBoxDrawer(fractionDigits: 6);
            s_drawers[typeof(bool)] = new ToggleSwitchDrawer();
            s_drawers[typeof(string)] = new TextBoxDrawer();
            s_drawers[typeof(float3)] = new Float3Drawer();

            // Discover user-defined drawers via TypeCache
            var customDrawers = TypeCache.GetTypesWithAttribute<CustomPropertyDrawerAttribute>();
            if (customDrawers != null)
            {
                foreach (var typeInfo in customDrawers)
                {
                    var type = typeInfo.AsType();
                    var attr = type.GetCustomAttribute<CustomPropertyDrawerAttribute>();
                    if (attr != null && typeof(PropertyDrawer).IsAssignableFrom(type))
                    {
                        if (Activator.CreateInstance(typeInfo) is PropertyDrawer drawer)
                        {
                            s_drawers[attr.TargetFieldType] = drawer;
                        }
                    }
                }
            }

            s_initialized = true;
        }
    }

    public static PropertyDrawer GetDrawer(Type fieldType)
    {
        if (!s_initialized) Initialize();

        if (s_drawers.TryGetValue(fieldType, out var drawer))
        {
            return drawer;
        }

        if (fieldType.IsEnum)
        {
            return EnumDrawer.Instance;
        }

        // Fallback for unknown types (we could return a ReadOnlyTextDrawer, or a FoldoutDrawer if it's a nested struct)
        return ReadOnlyDrawer.Instance;
    }
}
