using Ghost.Editor.PoC.Nodes;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Editor.PoC.Drawers;

public class BoolPropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var cur = property.GetValue() is bool b && b;

        return ToggleSwitch(
            isOn: cur,
            onIsOnChanged: on => context.ModifyProperty(property, on),
            header: property.Name
        );
    }
}
