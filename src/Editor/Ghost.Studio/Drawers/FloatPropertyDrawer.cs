using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

public sealed class FloatPropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var current = property.Value is float f ? f : (property.Value is double d ? (float)d : 0f);

        return HStack(8,
            TextBlock(property.Name).Foreground(GhostColors.TextSecondary).FontSize(11).VAlign(VerticalAlignment.Center).Width(80),
            NumberBox(current, v => context.ModifyProperty(property, current, (float)v))
                .SpinButtons(Microsoft.UI.Xaml.Controls.NumberBoxSpinButtonPlacementMode.Hidden)
                .Height(22).FontSize(11).Width(100)
        ).Margin(top: 2, bottom: 2);
    }
}
