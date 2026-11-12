using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

public sealed class DefaultPropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var valStr = property.Value?.ToString() ?? "null";

        return HStack(8,
            TextBlock(property.Name).Foreground(GhostColors.TextSecondary).FontSize(11).VAlign(VerticalAlignment.Center).Width(80),
            TextBlock(valStr).Foreground(GhostColors.TextPrimary).FontSize(11).VAlign(VerticalAlignment.Center)
        ).Margin(top: 2, bottom: 2);
    }
}
