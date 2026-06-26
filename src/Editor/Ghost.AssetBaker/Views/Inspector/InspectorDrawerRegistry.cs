using Ghost.AssetBaker.Attributes;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using System.Reflection;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.AssetBaker.Views.Inspector;

public class InspectorDrawerRegistry
{
    private static readonly Lazy<InspectorDrawerRegistry> s_instance = new(() => new InspectorDrawerRegistry());
    public static InspectorDrawerRegistry Instance => s_instance.Value;

    private readonly Dictionary<Type, IPropertyDrawer> _attributeDrawers = new();
    private readonly Dictionary<Type, ICustomEditor> _customEditors = new();

    // Core default drawers
    private readonly Drawers.BoolDrawer _boolDrawer = new();
    private readonly Drawers.EnumDrawer _enumDrawer = new();
    private readonly Drawers.NumberDrawer _numberDrawer = new();
    private readonly Drawers.Vector4Drawer _vector4Drawer = new();
    private readonly Drawers.NestedObjectDrawer _nestedStructDrawer = new();

    private InspectorDrawerRegistry()
    {
        // Register attribute-based drawers
        _attributeDrawers[typeof(SliderAttribute)] = new Drawers.SliderDrawer();

        // Scan for Custom Editors
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var t in assembly.GetTypes())
                {
                    var attr = t.GetCustomAttribute<CustomEditorAttribute>();
                    if (attr != null && typeof(ICustomEditor).IsAssignableFrom(t) && !t.IsAbstract)
                    {
                        var editorInstance = (ICustomEditor)Activator.CreateInstance(t)!;
                        _customEditors[attr.TargetType] = editorInstance;
                    }
                }
            }
            catch { }
        }
    }

    public Element? DrawProperty(PropertyInfo property, object target, Action<object> onUpdate)
    {
        // 1. Check for Custom Attribute
        var drawerAttr = property.GetCustomAttribute<DrawerAttribute>();
        if (drawerAttr != null && _attributeDrawers.TryGetValue(drawerAttr.GetType(), out var attrDrawer))
        {
            return attrDrawer.Draw(property, target, onUpdate);
        }

        // 2. Fallback to default drawers
        var propType = property.PropertyType;
        if (propType == typeof(bool)) return _boolDrawer.Draw(property, target, onUpdate);
        if (propType.IsEnum) return _enumDrawer.Draw(property, target, onUpdate);
        if (IsNumber(propType)) return _numberDrawer.Draw(property, target, onUpdate);
        if (propType == typeof(System.Numerics.Vector4)) return _vector4Drawer.Draw(property, target, onUpdate);

        if (propType.IsValueType && !propType.IsPrimitive) return _nestedStructDrawer.Draw(property, target, onUpdate);

        return null; // skip
    }

    public Element DrawObject(object? target, Action<object> onUpdate)
    {
        if (target == null)
            return Caption("No custom settings available.").Foreground(Theme.DisabledText);

        var type = target.GetType();

        // 1. Check for Custom Editor
        if (_customEditors.TryGetValue(type, out var customEditor))
        {
            return customEditor.Draw(target, onUpdate);
        }

        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var children = new List<Element>();

        foreach (var prop in props)
        {
            if (!prop.CanRead || !prop.CanWrite) continue;

            // Handle ShowWhen
            var showWhenAttr = prop.GetCustomAttribute<ShowWhenAttribute>();
            if (showWhenAttr != null)
            {
                var condProp = type.GetProperty(showWhenAttr.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                if (condProp != null)
                {
                    var condVal = condProp.GetValue(target);
                    // Standard equality comparison
                    if (condVal == null && showWhenAttr.Value != null) continue;
                    if (condVal != null && !condVal.Equals(Convert.ChangeType(showWhenAttr.Value, condVal.GetType()))) continue;
                }
            }

            var drawnElement = DrawProperty(prop, target, onUpdate);
            if (drawnElement != null)
            {
                children.Add(drawnElement);
            }
        }

        if (children.Count == 0)
            return Caption("No configurable properties.").Foreground(Theme.DisabledText);

        return FlexColumn(children.ToArray()) with { RowGap = 12 };
    }

    private static bool IsNumber(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) || type == typeof(decimal);
    }
}
