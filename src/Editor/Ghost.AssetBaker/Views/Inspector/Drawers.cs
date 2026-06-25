using System;
using System.Reflection;
using Ghost.AssetBaker.Attributes;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.AssetBaker.Views.Inspector.Drawers;

public class SliderDrawer : IPropertyDrawer
{
    public Element Draw(PropertyInfo property, object target, Action<object> onUpdate)
    {
        var attr = property.GetCustomAttribute<SliderAttribute>();
        if (attr == null) return Empty();

        var val = property.GetValue(target);
        var doubleVal = val != null ? Convert.ToDouble(val) : 0.0;

        return FlexColumn(
            BodyStrong(property.Name).Margin(bottom: 4),
            Slider(doubleVal, attr.Min, attr.Max, newVal =>
            {
                try
                {
                    property.SetValue(target, Convert.ChangeType(newVal, property.PropertyType));
                    onUpdate(target);
                }
                catch { }
            }).AutomationName(property.Name)
        );
    }
}

public class BoolDrawer : IPropertyDrawer
{
    public Element Draw(PropertyInfo property, object target, Action<object> onUpdate)
    {
        var val = property.GetValue(target);
        return CheckBox((bool)(val ?? false), v =>
        {
            property.SetValue(target, v);
            onUpdate(target);
        }, property.Name).AutomationName(property.Name);
    }
}

public class EnumDrawer : IPropertyDrawer
{
    public Element Draw(PropertyInfo property, object target, Action<object> onUpdate)
    {
        var val = property.GetValue(target);
        var enumNames = Enum.GetNames(property.PropertyType);
        var enumValues = Enum.GetValues(property.PropertyType);
        var selectedIndex = val != null ? Array.IndexOf(enumValues, val) : 0;
        if (selectedIndex == -1) selectedIndex = 0;
        
        return FlexColumn(
            BodyStrong(property.Name).Margin(bottom: 4),
            ComboBox(enumNames, selectedIndex, idx =>
            {
                if (idx >= 0 && idx < enumValues.Length)
                {
                    property.SetValue(target, enumValues.GetValue(idx));
                    onUpdate(target);
                }
            }).AutomationName(property.Name)
        );
    }
}

public class NumberDrawer : IPropertyDrawer
{
    public Element Draw(PropertyInfo property, object target, Action<object> onUpdate)
    {
        var val = property.GetValue(target);
        return FlexColumn(
            BodyStrong(property.Name).Margin(bottom: 4),
            TextBox(val?.ToString() ?? "", text =>
            {
                try
                {
                    var parsed = Convert.ChangeType(text, property.PropertyType);
                    property.SetValue(target, parsed);
                    onUpdate(target);
                }
                catch { /* Ignore invalid parse */ }
            }).AutomationName(property.Name)
        );
    }
}

public class Vector4Drawer : IPropertyDrawer
{
    public Element Draw(PropertyInfo property, object target, Action<object> onUpdate)
    {
        var val = property.GetValue(target);
        var vec = val == null ? new System.Numerics.Vector4() : (System.Numerics.Vector4)val;
        
        return FlexColumn(
            BodyStrong(property.Name).Margin(bottom: 4),
            FlexRow(
                TextBox(vec.X.ToString(), text => { if (float.TryParse(text, out var f)) { vec.X = f; property.SetValue(target, vec); onUpdate(target); } }).AutomationName($"{property.Name}_X"),
                TextBox(vec.Y.ToString(), text => { if (float.TryParse(text, out var f)) { vec.Y = f; property.SetValue(target, vec); onUpdate(target); } }).AutomationName($"{property.Name}_Y"),
                TextBox(vec.Z.ToString(), text => { if (float.TryParse(text, out var f)) { vec.Z = f; property.SetValue(target, vec); onUpdate(target); } }).AutomationName($"{property.Name}_Z"),
                TextBox(vec.W.ToString(), text => { if (float.TryParse(text, out var f)) { vec.W = f; property.SetValue(target, vec); onUpdate(target); } }).AutomationName($"{property.Name}_W")
            ) with { ColumnGap = 4 }
        );
    }
}

public class NestedObjectDrawer : IPropertyDrawer
{
    public Element Draw(PropertyInfo property, object target, Action<object> onUpdate)
    {
        var val = property.GetValue(target);
        if (val == null) return Empty();

        return FlexColumn(
            BodyStrong(property.Name).Margin(bottom: 8, top: 8).Foreground(Theme.AccentText),
            Border(InspectorDrawerRegistry.Instance.DrawObject(val, updatedVal =>
            {
                property.SetValue(target, updatedVal);
                onUpdate(target);
            })).Padding(left: 12)
        ) with { RowGap = 4 };
    }
}
