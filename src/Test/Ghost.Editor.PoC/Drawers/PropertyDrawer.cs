using Ghost.Editor.PoC.Nodes;
using Microsoft.UI.Reactor.Core;

namespace Ghost.Editor.PoC.Drawers;

public abstract class PropertyDrawer
{
    public abstract Element Draw(PropertyNode property, DrawerContext context);
}
