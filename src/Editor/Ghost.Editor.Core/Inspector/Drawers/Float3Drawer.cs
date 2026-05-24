using Ghost.Core;
using Ghost.Editor.Core.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Ghost.Editor.Core.Inspector.Drawers;

public sealed class Float3Drawer : PropertyDrawer
{
    public override FrameworkElement CreateControl(PropertyModel model)
    {
        var field = new Float3Field
        {
            IsEnabled = !model.Descriptor.IsReadOnly
        };

        var binding = new Binding
        {
            Source = model,
            Path = new PropertyPath("Value"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };

        field.SetBinding(Float3Field.ValueProperty, binding);

        return field;
    }
}
