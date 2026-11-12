using Ghost.Editor.PoC.Nodes;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Misaki.HighPerformance.Mathematics;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Editor.PoC.Drawers;

public class TransformComponentDrawer : ComponentDrawer
{
    public override Element Draw(ComponentNode component, DrawerContext context)
    {
        var posProp = component.Properties.Find(p => p.Name == "position");
        var scaleProp = component.Properties.Find(p => p.Name == "scale");

        var livePos = posProp?.GetValue() is float3 p ? p : float3.zero;

        var propElements = new List<Element?>();

        // Live Readout Bar
        propElements.Add(Border(HStack(16,
            TextBlock("Live Position:").FontSize(12),
            TextBlock($"X: {livePos.x:F2}").FontSize(12),
            TextBlock($"Y: {livePos.y:F2}").FontSize(12),
            TextBlock($"Z: {livePos.z:F2}").FontSize(12)
        )).Padding(8).BorderBrush("#40808080").BorderThickness(1).CornerRadius(4));

        if (posProp != null)
        {
            propElements.Add(context.DrawProperty(posProp));
        }

        if (scaleProp != null)
        {
            propElements.Add(context.DrawProperty(scaleProp));
        }

        // Quick Preset Buttons (All routed through CommandHistory for Undo/Redo!)
        propElements.Add(HStack(8,
            Button("Reset Pos (0,0,0)", () =>
            {
                if (posProp != null)
                {
                    context.ModifyProperty(posProp, float3.zero);
                }
            }),
            Button("Reset Scale (1,1,1)", () =>
            {
                if (scaleProp != null)
                {
                    context.ModifyProperty(scaleProp, new float3(1f));
                }
            })
        ));

        return Border(VStack(10,
            HStack(8,
                TextBlock("LocalTransform (struct IComponentData)").FontSize(15),
                TextBlock("— Undo/Redo enabled").FontSize(11)
            ),
            VStack(8, propElements.ToArray())
        )).Padding(12).BorderBrush("#40808080").BorderThickness(1).CornerRadius(6);
    }
}
