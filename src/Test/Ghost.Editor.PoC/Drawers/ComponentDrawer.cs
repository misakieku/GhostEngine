using Ghost.Editor.PoC.Nodes;
using Microsoft.UI.Reactor.Core;

namespace Ghost.Editor.PoC.Drawers;

public abstract class ComponentDrawer
{
    public abstract Element Draw(ComponentNode component, DrawerContext context);
}
