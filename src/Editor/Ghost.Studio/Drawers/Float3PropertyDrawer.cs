using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor.Core;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Studio.Drawers;

public sealed class Float3PropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var current = property.Value is float3 f ? f : default;

        return DrawAxisNumberRow(
            property,
            new AxisNumberSpec("X", GhostColors.StatusError, current.x, v => context.ModifyProperty(property, current, new float3((float)v, current.y, current.z))),
            new AxisNumberSpec("Y", GhostColors.StatusSuccess, current.y, v => context.ModifyProperty(property, current, new float3(current.x, (float)v, current.z))),
            new AxisNumberSpec("Z", GhostColors.AccentPrimary, current.z, v => context.ModifyProperty(property, current, new float3(current.x, current.y, (float)v)))
        );
    }
}
