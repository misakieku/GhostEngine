using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Inspector.Drawers;

public sealed class ReadOnlyDrawer : PropertyDrawer
{
    public static readonly ReadOnlyDrawer Instance = new();

    public override FrameworkElement CreateControl(PropertyModel model)
    {
        var textBlock = new TextBlock
        {
            Text = model.Value?.ToString() ?? "null",
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };

        // Note: For a strictly read-only drawer, we might still want to bind it one-way 
        // so it updates when the ECS value changes.
        var binding = new Microsoft.UI.Xaml.Data.Binding
        {
            Source = model,
            Path = new PropertyPath("Value"),
            Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay
        };
        textBlock.SetBinding(TextBlock.TextProperty, binding);

        return textBlock;
    }
}
