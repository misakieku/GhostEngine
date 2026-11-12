using Ghost.Editor.Core.Sync;
using Microsoft.UI.Reactor.Core;

namespace Ghost.Studio.Drawers;

public abstract class ComponentDrawer
{
    public abstract Element Draw(ComponentNode component, DrawerContext context);
}
