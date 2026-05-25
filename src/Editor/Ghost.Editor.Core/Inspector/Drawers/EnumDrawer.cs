using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

namespace Ghost.Editor.Core.Inspector.Drawers;

public sealed class EnumDrawer<T> : PropertyDrawer<T>
    where T : unmanaged, Enum
{
    public override FrameworkElement CreateControlT(PropertyModel<T> model)
    {
        var names = Enum.GetNames(typeof(T));
        
        var comboBox = new ComboBox
        {
            ItemsSource = names,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            IsEnabled = !model.Descriptor.IsReadOnly,
            SelectedItem = model.Value.ToString()
        };

        comboBox.SelectionChanged += (s, e) =>
        {
            if (comboBox.SelectedItem is string str)
            {
                if (Enum.TryParse<T>(str, out var parsed))
                {
                    model.SetValueFromUI(parsed);
                }
            }
        };

        model.OnValueChanged += (newVal) =>
        {
            comboBox.SelectedItem = newVal.ToString();
        };

        return comboBox;
    }
}
