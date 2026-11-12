using Ghost.Core;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Misaki.HighPerformance.Mathematics;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

public sealed class TransformComponentDrawer : ComponentDrawer
{
    public override Element Draw(ComponentNode component, DrawerContext context)
    {
        var propertyElements = new List<Element>();
        for (var i = 0; i < component.Properties.Count; i++)
        {
            propertyElements.Add(context.DrawProperty(component.Properties[i]));
        }

        var posProp = component.Properties.FirstOrDefault(p => p.Name.Equals("position", StringComparison.OrdinalIgnoreCase));

        return Border(
            VStack(6,
                HStack(8,
                    TextBlock("\uE7B8").FontSize(12).Foreground(GhostColors.AccentPrimary).VAlign(VerticalAlignment.Center),
                    TextBlock("Transform").FontSize(12).SemiBold().Foreground(GhostColors.TextPrimary).VAlign(VerticalAlignment.Center),
                    Button("Reset", () =>
                    {
                        if (posProp != null)
                        {
                            context.ModifyProperty(posProp, posProp.Value, float3.zero);
                        }
                    }).Height(20).FontSize(10).HAlign(HorizontalAlignment.Right)
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
