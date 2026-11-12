using Ghost.Editor.Core.Sync;
using Microsoft.UI.Reactor.Core;

namespace Ghost.Studio.Drawers;

public abstract class PropertyDrawer
{
    public abstract Element Draw(PropertyNode property, DrawerContext context);
}
