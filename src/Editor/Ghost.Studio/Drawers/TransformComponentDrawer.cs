using Ghost.Editor.Core.Sync;
using Microsoft.UI.Reactor.Core;
using Misaki.HighPerformance.Mathematics;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

public sealed class TransformComponentDrawer : ComponentDrawer
{
    public override Element Draw(ComponentNode component, DrawerContext context)
    {
        var posProp = component.Properties.FirstOrDefault(p => p.Name.Equals("position", StringComparison.OrdinalIgnoreCase));
        var resetBtn = Button("Reset", () =>
        {
            if (posProp != null)
            {
                context.ModifyProperty(posProp, posProp.Value, float3.zero);
            }
        });

        return BuildComponentCard(component, "Transform", "\uE7B8", DrawPropertyRows(component.Properties, context), context, resetBtn);
    }
}
