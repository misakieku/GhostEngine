using Ghost.AssetForge.Core.Attributes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ghost.AssetForge.Views.Inspector;

public class InspectorDrawerRegistry
{
    private readonly Dictionary<Type, IPropertyDrawer> _attributeDrawers = new();
    private readonly Dictionary<Type, ICustomEditor> _customEditors = new();

    private readonly BoolDrawer _boolDrawer = new();
    private readonly EnumDrawer _enumDrawer = new();
    private readonly NumberDrawer _numberDrawer = new();
    private readonly Vector4Drawer _vector4Drawer = new();
    private readonly NestedObjectDrawer _nestedObjectDrawer;

    public InspectorDrawerRegistry(IServiceProvider serviceProvider)
    {
        _nestedObjectDrawer = new NestedObjectDrawer(this);

        // Register attribute-based drawers
        _attributeDrawers[typeof(SliderAttribute)] = new SliderDrawer();

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
                        var editorInstance = (ICustomEditor)Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance(serviceProvider, t);
                        _customEditors[attr.TargetType] = editorInstance;
                    }
                }
            }
            catch { }
        }
    }

    public FrameworkElement? DrawProperty(PropertyInfo property, object target)
    {
        // 1. Check for Custom Attribute drawer
        var drawerAttr = property.GetCustomAttribute<DrawerAttribute>();
        if (drawerAttr != null && _attributeDrawers.TryGetValue(drawerAttr.GetType(), out var attrDrawer))
        {
            return attrDrawer.Draw(property, target);
        }

        // 2. Fallback to default drawers
        var propType = property.PropertyType;
        if (propType == typeof(bool)) return _boolDrawer.Draw(property, target);
        if (propType.IsEnum) return _enumDrawer.Draw(property, target);
        if (IsNumber(propType)) return _numberDrawer.Draw(property, target);
        if (propType == typeof(System.Numerics.Vector4)) return _vector4Drawer.Draw(property, target);

        // Nested objects / classes / structs
        if (propType.IsClass && propType != typeof(string) && !propType.IsPrimitive)
        {
            return _nestedObjectDrawer.Draw(property, target);
        }
        if (propType.IsValueType && !propType.IsPrimitive)
        {
            return _nestedObjectDrawer.Draw(property, target);
        }

        return null;
    }

    public FrameworkElement DrawObject(object? target)
    {
        if (target == null)
        {
            return new TextBlock
            {
                Text = "No custom settings available.",
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        var type = target.GetType();

        // 1. Check for Custom Editor
        if (_customEditors.TryGetValue(type, out var customEditor))
        {
            return customEditor.Draw(target);
        }

        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var container = new StackPanel { Spacing = 12 };

        foreach (var prop in props)
        {
            if (!prop.CanRead || !prop.CanWrite) continue;

            var showWhenAttr = prop.GetCustomAttribute<ShowWhenAttribute>();
            var drawnElement = DrawProperty(prop, target);
            if (drawnElement != null)
            {
                if (showWhenAttr != null)
                {
                    Action updateVisibility = () =>
                    {
                        var condProp = type.GetProperty(showWhenAttr.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                        if (condProp != null)
                        {
                            var condVal = condProp.GetValue(target);
                            var visible = false;
                            if (condVal != null)
                            {
                                var expectedVal = Convert.ChangeType(showWhenAttr.Value, condVal.GetType());
                                visible = condVal.Equals(expectedVal);
                            }
                            drawnElement.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                        }
                    };

                    updateVisibility();

                    if (target is System.ComponentModel.INotifyPropertyChanged npc)
                    {
                        npc.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == showWhenAttr.PropertyName)
                            {
                                drawnElement.DispatcherQueue.TryEnqueue(() => updateVisibility());
                            }
                        };
                    }
                }

                container.Children.Add(drawnElement);
            }
        }

        if (container.Children.Count == 0)
        {
            return new TextBlock
            {
                Text = "No configurable properties.",
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        return container;
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
