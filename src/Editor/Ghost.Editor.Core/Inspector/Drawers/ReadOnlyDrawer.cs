using Ghost.Editor.Core.SceneGraph;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Inspector.Drawers;

public sealed class ReadOnlyDrawer<T> : PropertyDrawer<T> where T : unmanaged
{
    public override FrameworkElement CreateControlT(PropertyNode<T> model)
    {
        var box = new TextBox
        {
            Text = model.Value.ToString(),
            IsReadOnly = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };

        model.OnValueChanged += (newVal) =>
        {
            box.Text = newVal.ToString();
        };

        return box;
    }
}
