using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Ghost.Editor.Core.Inspector.Drawers;

public sealed class NumberBoxDrawer : PropertyDrawer
{
    private readonly int _fractionDigits;

    public NumberBoxDrawer(int fractionDigits)
    {
        _fractionDigits = fractionDigits;
    }

    public override FrameworkElement CreateControl(PropertyModel model)
    {
        var box = new NumberBox
        {
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            IsEnabled = !model.Descriptor.IsReadOnly,
            MaxWidth = double.PositiveInfinity // To fill PropertyField
        };
        
        var formatter = new Windows.Globalization.NumberFormatting.DecimalFormatter
        {
            FractionDigits = _fractionDigits
        };
        box.NumberFormatter = formatter;

        // NumberBox uses Value property for its double value.
        // We bind Mode=TwoWay so typing updates the model.
        // Convert back and forth between box's double and the model's actual type.
        var binding = new Binding
        {
            Source = model,
            Path = new PropertyPath("Value"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Converter = new NumericConverter(model.Descriptor.FieldType)
        };
        
        box.SetBinding(NumberBox.ValueProperty, binding);
        
        return box;
    }

    private class NumericConverter : IValueConverter
    {
        private readonly System.Type _targetType;

        public NumericConverter(System.Type targetType)
        {
            _targetType = targetType;
        }

        public object Convert(object value, System.Type targetType, object parameter, string language)
        {
            if (value == null) return double.NaN;
            return System.Convert.ToDouble(value);
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, string language)
        {
            if (value is double d)
            {
                if (double.IsNaN(d)) return 0; // Or whatever default is appropriate
                return System.Convert.ChangeType(d, _targetType);
            }
            return value;
        }
    }
}
