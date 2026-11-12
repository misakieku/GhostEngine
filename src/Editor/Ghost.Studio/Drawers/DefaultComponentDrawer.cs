using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

public sealed class DefaultComponentDrawer : ComponentDrawer
{
    public override Element Draw(ComponentNode component, DrawerContext context)
    {
        var propertyElements = new List<Element>();
        for (var i = 0; i < component.Properties.Count; i++)
        {
            propertyElements.Add(context.DrawProperty(component.Properties[i]));
        }

        return Border(
            VStack(6,
                HStack(8,
                    TextBlock("\uE7B8").FontSize(12).Foreground(GhostColors.AccentPrimary).VAlign(VerticalAlignment.Center),
                    TextBlock(component.TypeName).FontSize(12).SemiBold().Foreground(GhostColors.TextPrimary).VAlign(VerticalAlignment.Center)
                ),
                VStack(4, propertyElements.ToArray())
            )
        )
        .Background(GhostColors.CardBackground)
        .WithBorder(GhostColors.SubtleBorder, 1)
        .CornerRadius(4)
        .Padding(8)
        .Margin(bottom: 6);
    }
}
