using Ghost.Editor.PoC.Nodes;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Editor.PoC.Drawers;

public class FloatPropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var cur = property.GetValue() is float v ? v : 0f;

        return NumberBox(
            value: cur,
            onValueChanged: v =>
            {
                if (double.IsNaN(v)) return;
                context.ModifyProperty(property, (float)v);
            },
            header: property.Name
        );
    }
}
