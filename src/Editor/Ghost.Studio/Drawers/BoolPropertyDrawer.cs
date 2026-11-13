using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

public sealed class BoolPropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var current = property.Value is bool b && b;
        var editor = ToggleSwitch(current, on => context.ModifyProperty(property, current, on)).AutomationName(property.DisplayName);
        return DrawLabeledRow(property, editor);
    }
}
