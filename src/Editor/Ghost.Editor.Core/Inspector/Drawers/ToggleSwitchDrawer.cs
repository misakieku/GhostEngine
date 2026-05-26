using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Inspector.Drawers;

public sealed class ToggleSwitchDrawer : PropertyDrawer<bool>
{
    public override FrameworkElement CreateControlT(Ghost.Editor.Core.SceneGraph.PropertyNode<bool> model)
    {
        var toggle = new ToggleSwitch
        {
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
