using Ghost.Editor.PoC.Nodes;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Editor.PoC.Drawers;

public class DefaultComponentDrawer : ComponentDrawer
{
    public override Element Draw(ComponentNode component, DrawerContext context)
    {
        var propertyElements = component.Properties.Select(p => context.DrawProperty(p)).ToArray();

        return Border(VStack(8,
            HStack(8,
                TextBlock(component.DisplayName).FontSize(14),
                TextBlock($"({component.ComponentType.Name})").FontSize(11)
            ),
            VStack(6, propertyElements)
        )).Padding(10).BorderBrush("#40808080").BorderThickness(1).CornerRadius(6);
    }
}
