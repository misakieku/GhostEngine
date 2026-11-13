using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

public sealed class IntPropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var current = property.Value is int i ? i : 0;
        var editor = NumberBox(current, v => context.ModifyProperty(property, current, (int)v))
            .SpinButtons(Microsoft.UI.Xaml.Controls.NumberBoxSpinButtonPlacementMode.Hidden)
            .HAlign(HorizontalAlignment.Stretch)
            .AutomationName(property.DisplayName);

        return DrawLabeledRow(property, editor);
    }
}
