using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Controls;

public sealed partial class PropertyField : ContentControl
{
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
}