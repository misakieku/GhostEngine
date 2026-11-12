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

        return HStack(8,
            TextBlock(property.Name).Foreground(GhostColors.TextSecondary).FontSize(11).VAlign(VerticalAlignment.Center).Width(80),
            ToggleSwitch(current, on => context.ModifyProperty(property, current, on))
        ).Margin(top: 2, bottom: 2);
    }
}
