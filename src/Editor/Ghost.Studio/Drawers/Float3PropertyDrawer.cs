using Ghost.Core;
using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Misaki.HighPerformance.Mathematics;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

public sealed class Float3PropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var current = property.Value is float3 f ? f : default;

        return HStack(6,
            TextBlock(property.Name).Foreground(GhostColors.TextSecondary).FontSize(11).VAlign(VerticalAlignment.Center).Width(70),

            TextBlock("X").Foreground("#EF4444").FontSize(10).SemiBold().VAlign(VerticalAlignment.Center),
            NumberBox(current.x, v => context.ModifyProperty(property, current, new float3((float)v, current.y, current.z)))
                .SpinButtons(Microsoft.UI.Xaml.Controls.NumberBoxSpinButtonPlacementMode.Hidden)
                .Height(22).FontSize(11).Width(55),

            TextBlock("Y").Foreground("#10B981").FontSize(10).SemiBold().VAlign(VerticalAlignment.Center),
            NumberBox(current.y, v => context.ModifyProperty(property, current, new float3(current.x, (float)v, current.z)))
                .SpinButtons(Microsoft.UI.Xaml.Controls.NumberBoxSpinButtonPlacementMode.Hidden)
                .Height(22).FontSize(11).Width(55),

            TextBlock("Z").Foreground("#3B82F6").FontSize(10).SemiBold().VAlign(VerticalAlignment.Center),
            NumberBox(current.z, v => context.ModifyProperty(property, current, new float3(current.x, current.y, (float)v)))
                .SpinButtons(Microsoft.UI.Xaml.Controls.NumberBoxSpinButtonPlacementMode.Hidden)
                .Height(22).FontSize(11).Width(55)
        ).Margin(top: 2, bottom: 2);
    }
}
