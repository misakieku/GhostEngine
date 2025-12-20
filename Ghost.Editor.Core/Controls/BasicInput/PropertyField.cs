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

    private object? _sourceObject;
    internal FieldInfo? _propertyInfo;
    internal Type? _fieldType;

    private object? _lastValue;

    public event Action<PropertyField>? OnValueChanged;

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
        propertyField._sourceObject = sourceObject;
        propertyField._propertyInfo = fieldInfo;
        propertyField._fieldType = typeof(TField);

        var field = factory();

        var dp = GetValueProperty(typeof(TField));
        field.SetBinding(dp, new Binding
        {
            Source = sourceObject,
            Path = new PropertyPath(fieldInfo.Name),
            Mode = BindingMode.TwoWay,
        });

        field.RegisterPropertyChangedCallback(dp, (s, e) =>
        {
            propertyField.OnValueChanged?.Invoke(propertyField);
        });

        return field;
    }

    public static PropertyField Create(string label, FieldInfo fieldInfo, object sourceObject)
    {
        var propertyField = new PropertyField
        {
            Label = label
        };

        FrameworkElement content = fieldInfo.FieldType switch
        {
            Type t when t == typeof(string) => ConfigureField(propertyField, fieldInfo, sourceObject, () => new TextBox()),
            Type t when t == typeof(int) || t == typeof(float) || t == typeof(double) => ConfigureField(propertyField, fieldInfo, sourceObject, () => new NumberBox
            {
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
                AcceptsExpression = true,
                NumberFormatter = new DecimalFormatter
                {
                    FractionDigits = t == typeof(int) ? 0 : 9,
                }
            }),
            Type t when t == typeof(bool) => ConfigureField(propertyField, fieldInfo, sourceObject, () => new ToggleSwitch()),
            Type t when t == typeof(Enum) => ConfigureField(propertyField, fieldInfo, sourceObject, () => new ComboBox
            {
                ItemsSource = Enum.GetValues(t),
                SelectedValuePath = "Value",
            }),
            _ => new TextBlock
            {
                Text = $"Unsupported type: {fieldInfo.FieldType.Name}",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red)
            },
        };

        propertyField.Content = content;
        return propertyField;
    }

    public void UpdateValue()
    {
        if (_sourceObject == null || _propertyInfo == null || _fieldType == null)
        {
            return;
        }

        var currentValue = _propertyInfo.GetValue(_sourceObject);
        if (Equals(currentValue, _lastValue))
        {
            return;
        }

        var dp = GetValueProperty(_fieldType);
        if (dp != null)
        {
            SetValue(dp, _propertyInfo.GetValue(_sourceObject));
            _lastValue = currentValue;
        }
    }
}