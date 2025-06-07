using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Controls.Internal;

internal sealed partial class InspectorView : ContentControl
{
    public UIElement? Header
    {
        get => (UIElement)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header),
        typeof(UIElement),
        typeof(InspectorView),
        new PropertyMetadata(null));

    public InspectorView()
    {
        DefaultStyleKey = typeof(InspectorView);
    }
}
