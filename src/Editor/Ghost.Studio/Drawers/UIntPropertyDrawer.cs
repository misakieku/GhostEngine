using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

public sealed class UIntPropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var current = property.Value is uint u ? u : 0u;
        var editor = NumberBox(current, v =>
        {
            var clamped = v < 0 ? 0u : (uint)v;
            context.ModifyProperty(property, current, clamped);
        })
        .SpinButtons(Microsoft.UI.Xaml.Controls.NumberBoxSpinButtonPlacementMode.Hidden)
        .HAlign(HorizontalAlignment.Stretch)
        .AutomationName(property.DisplayName);

        return DrawLabeledRow(property, editor);
    }
}
