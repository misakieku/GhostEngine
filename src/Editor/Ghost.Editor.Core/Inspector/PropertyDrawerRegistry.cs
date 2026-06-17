using Ghost.Core;
using Ghost.Editor.Core.Inspector.Drawers;
using Ghost.Editor.Core.Utilities;
using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;
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
            if (s_initialized)
            {
                return;
            }

            // Register built-in drawers
            s_drawers[typeof(float)] = NumberBoxDrawer<float>.CreateFloatingPoint();
            s_drawers[typeof(double)] = NumberBoxDrawer<double>.CreateFloatingPoint();
            s_drawers[typeof(int)] = NumberBoxDrawer<int>.CreateInteger();
            s_drawers[typeof(uint)] = NumberBoxDrawer<uint>.CreateInteger();
            s_drawers[typeof(short)] = NumberBoxDrawer<short>.CreateInteger();
            s_drawers[typeof(ushort)] = NumberBoxDrawer<ushort>.CreateInteger();
            s_drawers[typeof(long)] = NumberBoxDrawer<long>.CreateInteger();
            s_drawers[typeof(ulong)] = NumberBoxDrawer<ulong>.CreateInteger();
            s_drawers[typeof(sbyte)] = NumberBoxDrawer<sbyte>.CreateInteger();
            s_drawers[typeof(byte)] = NumberBoxDrawer<byte>.CreateInteger();
            s_drawers[typeof(bool)] = new ToggleSwitchDrawer();

            s_drawers[typeof(Entity)] = new EntityDrawer();

            // TODO: Use source generator.
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

    public static bool HasCustomDrawer(Type fieldType)
    {
        if (!s_initialized)
        {
            Initialize();
        }

        return s_drawers.ContainsKey(fieldType);
    }

    public static PropertyDrawer GetDrawer(Type fieldType)
    {
        if (!s_initialized)
        {
            Initialize();
        }

        if (s_drawers.TryGetValue(fieldType, out var drawer))
        {
            return drawer;
        }

        if (fieldType.IsEnum)
        {
            var enumDrawerType = typeof(EnumDrawer<>).MakeGenericType(fieldType);
            var enumDrawer = (PropertyDrawer)Activator.CreateInstance(enumDrawerType)!;
            s_drawers[fieldType] = enumDrawer;
            return enumDrawer;
        }

        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Handle<>))
        {
            var argType = fieldType.GetGenericArguments()[0];
            var handleDrawerType = typeof(HandleDrawer<>).MakeGenericType(argType);
            var handleDrawer = (PropertyDrawer)Activator.CreateInstance(handleDrawerType)!;
            s_drawers[fieldType] = handleDrawer;
            return handleDrawer;
        }

        // Fallback for unknown types. If it's an unmanaged struct with fields, we use EmptyDrawer 
        // to let the children render. If it's a primitive or something else, use ReadOnlyDrawer.
        Type genericDrawerType;
        if (fieldType.IsValueType && !fieldType.IsPrimitive && !fieldType.IsEnum)
        {
            genericDrawerType = typeof(EmptyDrawer<>);
        }
        else
        {
            genericDrawerType = typeof(ReadOnlyDrawer<>);
        }

        var drawerType = genericDrawerType.MakeGenericType(fieldType);
        var drawerInstance = (PropertyDrawer)Activator.CreateInstance(drawerType)!;
        s_drawers[fieldType] = drawerInstance;
        return drawerInstance;
    }
}
