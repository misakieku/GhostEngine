using Ghost.Editor.Core.Sync;
using Microsoft.UI.Reactor.Core;

namespace Ghost.Studio.Drawers;

public sealed class DefaultComponentDrawer : ComponentDrawer
{
    public override Element Draw(ComponentNode component, DrawerContext context)
    {
        return BuildComponentCard(component, component.TypeName, "\uE7B8", DrawPropertyRows(component.Properties, context), context);
    }
}
