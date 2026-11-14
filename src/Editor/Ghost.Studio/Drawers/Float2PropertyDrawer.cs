using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor.Core;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Studio.Drawers;

public sealed class Float2PropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var current = property.Value is float2 f ? f : default;

        return DrawAxisNumberRow(
            property,
            new AxisNumberSpec("X", GhostColors.StatusError, current.x, v => context.ModifyProperty(property, current, new float2((float)v, current.y))),
            new AxisNumberSpec("Y", GhostColors.StatusSuccess, current.y, v => context.ModifyProperty(property, current, new float2(current.x, (float)v)))
        );
    }
}
