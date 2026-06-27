using System;
using System.Reflection;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Ghost.AssetForge.Core.Attributes;

namespace Ghost.AssetForge.Views.Inspector;

public class SliderDrawer : IPropertyDrawer
{
    public FrameworkElement Draw(PropertyInfo property, object target)
    {
        var attr = property.GetCustomAttribute<SliderAttribute>();
        if (attr == null) return new Grid();

        var val = property.GetValue(target);
        double doubleVal = val != null ? Convert.ToDouble(val) : 0.0;

        var panel = new StackPanel { Spacing = 4, Margin = new Thickness(0, 4, 0, 4) };
        var label = new TextBlock
        {
            Text = FormatDisplayName(property.Name),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
        };
        panel.Children.Add(label);

        var slider = new Slider
        {
            Minimum = attr.Min,
            Maximum = attr.Max,
            Value = doubleVal,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        slider.ValueChanged += (s, e) =>
        {
            try
            {
                property.SetValue(target, Convert.ChangeType(slider.Value, property.PropertyType));
            }
            catch { }
        };
        panel.Children.Add(slider);

        return panel;
    }

    private string FormatDisplayName(string name) => XamlInspectorHelper.FormatDisplayName(name);
}

public class BoolDrawer : IPropertyDrawer
{
    public FrameworkElement Draw(PropertyInfo property, object target)
    {
        var val = property.GetValue(target);
        var toggle = new ToggleSwitch
        {
            Header = FormatDisplayName(property.Name),
            IsOn = (bool)(val ?? false),
            OffContent = "",
            OnContent = "",
            Margin = new Thickness(0, 4, 0, 4)
        };
        toggle.Toggled += (s, e) =>
        {
            property.SetValue(target, toggle.IsOn);
        };
        return toggle;
    }

    private string FormatDisplayName(string name) => XamlInspectorHelper.FormatDisplayName(name);
}

public class EnumDrawer : IPropertyDrawer
{
    public FrameworkElement Draw(PropertyInfo property, object target)
    {
        var val = property.GetValue(target);
        var enumNames = Enum.GetNames(property.PropertyType);
        var enumValues = Enum.GetValues(property.PropertyType);
        int selectedIndex = val != null ? Array.IndexOf(enumValues, val) : 0;
        if (selectedIndex == -1) selectedIndex = 0;

        var panel = new StackPanel { Spacing = 4, Margin = new Thickness(0, 4, 0, 4) };
        var label = new TextBlock
        {
            Text = FormatDisplayName(property.Name),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
        };
        panel.Children.Add(label);

        var combo = new ComboBox
        {
            ItemsSource = enumNames,
            SelectedIndex = selectedIndex,
            Height = 36,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        combo.SelectionChanged += (s, e) =>
        {
            if (combo.SelectedIndex >= 0 && combo.SelectedIndex < enumValues.Length)
            {
                property.SetValue(target, enumValues.GetValue(combo.SelectedIndex));
            }
        };
        panel.Children.Add(combo);

        return panel;
    }

    private string FormatDisplayName(string name) => XamlInspectorHelper.FormatDisplayName(name);
}

public class NumberDrawer : IPropertyDrawer
{
    public FrameworkElement Draw(PropertyInfo property, object target)
    {
        var val = property.GetValue(target);

        var panel = new StackPanel { Spacing = 4, Margin = new Thickness(0, 4, 0, 4) };
        var label = new TextBlock
        {
            Text = FormatDisplayName(property.Name),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
        };
        panel.Children.Add(label);

        var numBox = new NumberBox
        {
            Value = val != null ? Convert.ToDouble(val) : 0.0,
            Height = 36,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SmallChange = 1,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
        };
        numBox.ValueChanged += (s, e) =>
        {
            if (!double.IsNaN(numBox.Value))
            {
                try
                {
                    property.SetValue(target, Convert.ChangeType(numBox.Value, property.PropertyType));
                }
                catch { }
            }
        };
        panel.Children.Add(numBox);

        return panel;
    }

    private string FormatDisplayName(string name) => XamlInspectorHelper.FormatDisplayName(name);
}

public class Vector4Drawer : IPropertyDrawer
{
    public FrameworkElement Draw(PropertyInfo property, object target)
    {
        var val = property.GetValue(target);
        var vec = val == null ? new Vector4() : (Vector4)val;

        var panel = new StackPanel { Spacing = 4, Margin = new Thickness(0, 4, 0, 4) };
        var label = new TextBlock
        {
            Text = FormatDisplayName(property.Name),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
        };
        panel.Children.Add(label);

        var grid = new Grid { ColumnSpacing = 4 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var boxX = new NumberBox { Value = vec.X, PlaceholderText = "X", SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden };
        var boxY = new NumberBox { Value = vec.Y, PlaceholderText = "Y", SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden };
        var boxZ = new NumberBox { Value = vec.Z, PlaceholderText = "Z", SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden };
        var boxW = new NumberBox { Value = vec.W, PlaceholderText = "W", SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden };

        Action updateVector = () =>
        {
            if (!double.IsNaN(boxX.Value) && !double.IsNaN(boxY.Value) && !double.IsNaN(boxZ.Value) && !double.IsNaN(boxW.Value))
            {
                property.SetValue(target, new Vector4((float)boxX.Value, (float)boxY.Value, (float)boxZ.Value, (float)boxW.Value));
            }
        };

        boxX.ValueChanged += (s, e) => updateVector();
        boxY.ValueChanged += (s, e) => updateVector();
        boxZ.ValueChanged += (s, e) => updateVector();
        boxW.ValueChanged += (s, e) => updateVector();

        grid.Children.Add(boxX); Grid.SetColumn(boxX, 0);
        grid.Children.Add(boxY); Grid.SetColumn(boxY, 1);
        grid.Children.Add(boxZ); Grid.SetColumn(boxZ, 2);
        grid.Children.Add(boxW); Grid.SetColumn(boxW, 3);

        panel.Children.Add(grid);

        return panel;
    }

    private string FormatDisplayName(string name) => XamlInspectorHelper.FormatDisplayName(name);
}

public class NestedObjectDrawer : IPropertyDrawer
{
    private readonly InspectorDrawerRegistry _registry;

    public NestedObjectDrawer(InspectorDrawerRegistry registry)
    {
        _registry = registry;
    }

    public FrameworkElement Draw(PropertyInfo property, object target)
    {
        var val = property.GetValue(target);
        if (val == null) return new Grid();

        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 8, 0, 8) };
        var label = new TextBlock
        {
            Text = FormatDisplayName(property.Name),
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"]
        };
        panel.Children.Add(label);

        var border = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12),
            Child = _registry.DrawObject(val)
        };
        panel.Children.Add(border);

        return panel;
    }

    private string FormatDisplayName(string name) => XamlInspectorHelper.FormatDisplayName(name);
}

public static class XamlInspectorHelper
{
    public static string FormatDisplayName(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        var result = new System.Text.StringBuilder();
        result.Append(name[0]);
        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
            {
                result.Append(' ');
            }
            result.Append(name[i]);
        }
        return result.ToString();
    }
}
