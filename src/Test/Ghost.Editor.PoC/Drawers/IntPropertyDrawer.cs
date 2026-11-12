using Ghost.Editor.PoC.Nodes;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Editor.PoC.Drawers;

public class IntPropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var cur = property.GetValue() is int v ? v : 0;

        return NumberBox(
            value: cur,
            onValueChanged: v =>
            {
                if (double.IsNaN(v)) return;
                context.ModifyProperty(property, (int)v);
            },
            header: property.Name
        );
    }
}
