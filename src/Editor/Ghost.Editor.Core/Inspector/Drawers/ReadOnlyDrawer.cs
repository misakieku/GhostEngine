using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Inspector.Drawers;

public sealed class ReadOnlyDrawer<T> : PropertyDrawer<T> where T : unmanaged
{
    public override FrameworkElement CreateControlT(PropertyModel<T> model)
    {
        var textBlock = new TextBlock
        {
            Text = model.Value.ToString(),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };

        model.OnValueChanged += (newVal) =>
        {
            textBlock.Text = newVal.ToString();
        };

        return textBlock;
    }
}
