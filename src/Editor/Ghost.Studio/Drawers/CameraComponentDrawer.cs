using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

/// <summary>
/// Specialized inspector drawer for <see cref="Ghost.Engine.Components.Camera"/> grouping clipping, optics, and output properties.
/// </summary>
public sealed class CameraComponentDrawer : ComponentDrawer
{
    private static readonly HashSet<string> s_clippingFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "nearClipPlane", "farClipPlane"
    };

    private static readonly HashSet<string> s_opticsFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "focalLength", "sensorSize", "gateFit", "aperture", "shutterSpeed", "iso", "focusDistance"
    };

    public override Element Draw(ComponentNode component, DrawerContext context)
    {
        var clipping = new List<PropertyNode>();
        var optics = new List<PropertyNode>();
        var output = new List<PropertyNode>();

        for (var i = 0; i < component.Properties.Count; i++)
        {
            var prop = component.Properties[i];
            if (s_clippingFields.Contains(prop.Name))
            {
                clipping.Add(prop);
            }
            else if (s_opticsFields.Contains(prop.Name))
            {
                optics.Add(prop);
            }
            else
            {
                output.Add(prop);
            }
        }

        var sections = new List<Element>();
        if (clipping.Count > 0)
        {
            sections.Add(DrawSection("Clipping Planes", clipping, context));
        }

        if (optics.Count > 0)
        {
            sections.Add(DrawSection("Physical Camera", optics, context));
        }

        if (output.Count > 0)
        {
            sections.Add(DrawSection("Target & Priority", output, context));
        }

        return BuildComponentCard(component, "Camera", "\uE722", VStack(GhostLayout.SECTION_GAP, sections.ToArray()), context);
    }
}
