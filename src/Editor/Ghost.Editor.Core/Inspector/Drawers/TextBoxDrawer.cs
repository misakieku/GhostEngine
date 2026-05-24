using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Ghost.Editor.Core.Inspector.Drawers;

public sealed class TextBoxDrawer : PropertyDrawer
{
    public override FrameworkElement CreateControl(PropertyModel model)
    {
        var textBox = new TextBox
        {
            IsReadOnly = model.Descriptor.IsReadOnly,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var binding = new Binding
        {
            Source = model,
            Path = new PropertyPath("Value"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };

        textBox.SetBinding(TextBox.TextProperty, binding);

        return textBox;
    }
}
