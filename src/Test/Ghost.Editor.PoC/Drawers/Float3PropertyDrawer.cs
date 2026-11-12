using Ghost.Editor.PoC.Nodes;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Misaki.HighPerformance.Mathematics;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Editor.PoC.Drawers;

public class Float3PropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var cur = property.GetValue() is float3 p ? p : float3.zero;

        return VStack(4,
            TextBlock(property.Name).FontSize(13),
            HStack(8,
                NumberBox(value: cur.x, onValueChanged: v =>
                {
                    if (double.IsNaN(v)) return;
                    var live = property.GetValue() is float3 val ? val : float3.zero;
                    context.ModifyProperty(property, new float3((float)v, live.y, live.z));
                }, header: "X"),
                NumberBox(value: cur.y, onValueChanged: v =>
                {
                    if (double.IsNaN(v)) return;
                    var live = property.GetValue() is float3 val ? val : float3.zero;
                    context.ModifyProperty(property, new float3(live.x, (float)v, live.z));
                }, header: "Y"),
                NumberBox(value: cur.z, onValueChanged: v =>
                {
                    if (double.IsNaN(v)) return;
                    var live = property.GetValue() is float3 val ? val : float3.zero;
                    context.ModifyProperty(property, new float3(live.x, live.y, (float)v));
                }, header: "Z")
            )
        );
    }
}
