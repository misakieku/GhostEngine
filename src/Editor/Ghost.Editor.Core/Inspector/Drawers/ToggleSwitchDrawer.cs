using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Ghost.Editor.Core.Inspector.Drawers;

public sealed class ToggleSwitchDrawer : PropertyDrawer<bool>
{
    public override FrameworkElement CreateControlT(PropertyModel<bool> model)
    {
        var toggle = new ToggleSwitch
        {
            IsEnabled = !model.Descriptor.IsReadOnly,
            OnContent = "",
            OffContent = "",
            IsOn = model.Value
        };

        toggle.Toggled += (s, e) =>
        {
            model.SetValueFromUI(toggle.IsOn);
        };

        model.OnValueChanged += (newVal) =>
        {
            toggle.IsOn = newVal;
        };

        return toggle;
    }
}
