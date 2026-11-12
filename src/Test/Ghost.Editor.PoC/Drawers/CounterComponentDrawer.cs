using Ghost.Editor.PoC.Nodes;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Editor.PoC.Drawers;

public class CounterComponentDrawer : ComponentDrawer
{
    public override Element Draw(ComponentNode component, DrawerContext context)
    {
        var valProp = component.Properties.Find(p => p.Name == "Value");
        var stepProp = component.Properties.Find(p => p.Name == "Step");
        var autoProp = component.Properties.Find(p => p.Name == "AutoIncrement");

        var currentVal = valProp?.GetValue() is int v ? v : 0;

        var propElements = new List<Element?>();

        // Live Worker Readout Bar
        propElements.Add(Border(HStack(12,
            TextBlock("Live Worker Value:").FontSize(13),
            TextBlock($"{currentVal}").FontSize(22)
        )).Padding(8).BorderBrush("#40808080").BorderThickness(1).CornerRadius(4));

        if (valProp != null)
        {
            propElements.Add(context.DrawProperty(valProp));
        }

        if (stepProp != null)
        {
            propElements.Add(context.DrawProperty(stepProp));
        }

        if (autoProp != null)
        {
            propElements.Add(context.DrawProperty(autoProp));
        }

        // Quick Preset Buttons
        propElements.Add(HStack(8,
            Button("Set 0", () =>
            {
                if (valProp != null)
                {
                    context.ModifyProperty(valProp, 0);
                }
            }),
            Button("Set 100", () =>
            {
                if (valProp != null)
                {
                    context.ModifyProperty(valProp, 100);
                }
            }),
            Button("Stress 9999", () =>
            {
                if (valProp != null)
                {
                    context.ModifyProperty(valProp, 9999);
                }
            })
        ));

        return Border(VStack(10,
            HStack(8,
                TextBlock("CounterComponent (Worker DAG Simulation)").FontSize(15),
                TextBlock("— Undo/Redo enabled").FontSize(11)
            ),
            VStack(8, propElements.ToArray())
        )).Padding(12).BorderBrush("#40808080").BorderThickness(1).CornerRadius(6);
    }
}
