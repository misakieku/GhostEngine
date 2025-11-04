using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using System.Reflection;
using Windows.Globalization.NumberFormatting;

namespace Ghost.Editor.Core.Controls;

public sealed partial class PropertyField : ContentControl
{
    private static readonly Dictionary<Type, DependencyProperty> _valueProperties = new()
    {
        { typeof(TextBox), TextBox.TextProperty },
        { typeof(NumberBox), NumberBox.ValueProperty },
        { typeof(ToggleButton), ToggleButton.IsCheckedProperty },
        { typeof(ToggleSwitch), ToggleSwitch.IsOnProperty },
        { typeof(ComboBox), Selector.SelectedValueProperty },
        { typeof(RangeBase), RangeBase.ValueProperty },
    };

    private object? sourceObject;
    private FieldInfo? propertyInfo;
    private Type? _fieldType;

    private object? _lastValue;

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(PropertyField),
        new PropertyMetadata(default(string)));

    public PropertyField()
    {
        DefaultStyleKey = typeof(PropertyField);
    }

    private static DependencyProperty? GetValueProperty(Type? fieldType)
    {
        while (fieldType != null)
        {
            if (_valueProperties.TryGetValue(fieldType, out var dp))
            {
                return dp;
            }
            fieldType = fieldType.BaseType;
        }

        return null;
    }

    private static TField ConfigureField<TField>(PropertyField propertyField, FieldInfo fieldInfo, object sourceObject, Func<TField> factory)
        where TField : FrameworkElement
    {
        propertyField.sourceObject = sourceObject;
        propertyField.propertyInfo = fieldInfo;
        propertyField._fieldType = typeof(TField);

        var field = factory();

        var dp = GetValueProperty(typeof(TField));
        field.SetBinding(dp, new Binding
        {
            Source = sourceObject,
            Path = new PropertyPath(fieldInfo.Name),
            Mode = BindingMode.TwoWay,
        });
        return field;
    }

    public static PropertyField Create(string label, FieldInfo fieldInfo, object sourceObject)
    {
        var propertyField = new PropertyField
        {
            Label = label
        };

        FrameworkElement content;
        switch (fieldInfo.FieldType)
        {
            case Type t when t == typeof(string):
                content = ConfigureField(propertyField, fieldInfo, sourceObject, () => new TextBox());
                break;
            case Type t when t == typeof(int) || t == typeof(float) || t == typeof(double):
                content = ConfigureField(propertyField, fieldInfo, sourceObject, () => new NumberBox
                {
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
                    AcceptsExpression = true,
                    NumberFormatter = new DecimalFormatter
                    {
                        FractionDigits = t == typeof(int) ? 0 : 9,
                    }
                });
                break;
            case Type t when t == typeof(bool):
                content = ConfigureField(propertyField, fieldInfo, sourceObject, () => new ToggleSwitch());
                break;
            case Type t when t == typeof(Enum):
                content = ConfigureField(propertyField, fieldInfo, sourceObject, () => new ComboBox
                {
                    ItemsSource = Enum.GetValues(t),
                    SelectedValuePath = "Value",
                });
                break;
            default:
                content = new TextBlock
                {
                    Text = $"Unsupported type: {fieldInfo.FieldType.Name}",
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red)
                };
                break;
        }

        propertyField.Content = content;
        return propertyField;
    }

    public void UpdateValue()
    {
        if (sourceObject == null || propertyInfo == null || _fieldType == null)
        {
            return;
        }

        var currentValue = propertyInfo.GetValue(sourceObject);
        if (Equals(currentValue, _lastValue))
        {
            return;
        }

        var dp = GetValueProperty(_fieldType);
        if (dp != null)
        {
            SetValue(dp, propertyInfo.GetValue(sourceObject));
            _lastValue = currentValue;
        }
    }
}