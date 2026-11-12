using Ghost.Editor.PoC.Nodes;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Editor.PoC.Drawers;

public class DefaultPropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var val = property.GetValue();
        return HStack(8,
            TextBlock($"{property.Name}:").FontSize(12),
            TextBlock(val?.ToString() ?? "null").FontSize(12)
        );
    }
}
