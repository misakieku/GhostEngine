using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Ghost.Editor.Core.Inspector.Drawers;

public sealed class ToggleSwitchDrawer : PropertyDrawer
{
    public override FrameworkElement CreateControl(PropertyModel model)
    {
        var toggle = new ToggleSwitch
        {
            IsEnabled = !model.Descriptor.IsReadOnly,
            OnContent = "",
            OffContent = ""
        };

        var binding = new Binding
        {
            Source = model,
            Path = new PropertyPath("Value"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };

        toggle.SetBinding(ToggleSwitch.IsOnProperty, binding);

        return toggle;
    }
}
