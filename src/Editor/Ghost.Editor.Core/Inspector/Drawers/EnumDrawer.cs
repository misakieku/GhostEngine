using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

namespace Ghost.Editor.Core.Inspector.Drawers;

public sealed class EnumDrawer : PropertyDrawer
{
    public static readonly EnumDrawer Instance = new();

    public override FrameworkElement CreateControl(PropertyModel model)
    {
        var enumType = model.Descriptor.FieldType;
        var names = Enum.GetNames(enumType);
        
        var comboBox = new ComboBox
        {
            ItemsSource = names,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            IsEnabled = !model.Descriptor.IsReadOnly
        };

        // TwoWay binding for selection
        var binding = new Microsoft.UI.Xaml.Data.Binding
        {
            Source = model,
            Path = new PropertyPath("Value"),
            Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
            UpdateSourceTrigger = Microsoft.UI.Xaml.Data.UpdateSourceTrigger.PropertyChanged,
            Converter = new EnumStringConverter(enumType)
        };
        
        comboBox.SetBinding(ComboBox.SelectedItemProperty, binding);

        return comboBox;
    }

    private class EnumStringConverter : Microsoft.UI.Xaml.Data.IValueConverter
    {
        private readonly Type _enumType;

        public EnumStringConverter(Type enumType)
        {
            _enumType = enumType;
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return null!;
            return Enum.GetName(_enumType, value)!;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string s)
            {
                return Enum.Parse(_enumType, s);
            }
            return value;
        }
    }
}
