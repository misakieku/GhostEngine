using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

/// <summary>
/// Specialized inspector drawer for <see cref="Ghost.Engine.Components.MeshInstance"/>.
/// </summary>
public sealed class MeshInstanceDrawer : ComponentDrawer
{
    public override Element Draw(ComponentNode component, DrawerContext context)
    {
        var assetProps = new List<PropertyNode>();
        var lightingProps = new List<PropertyNode>();

        for (var i = 0; i < component.Properties.Count; i++)
        {
            var prop = component.Properties[i];
            if (prop.Name.Equals("mesh", StringComparison.OrdinalIgnoreCase) || prop.Name.Equals("materialPalette", StringComparison.OrdinalIgnoreCase))
            {
                assetProps.Add(prop);
            }
            else
            {
                lightingProps.Add(prop);
            }
        }

        var sections = new List<Element>();
        if (assetProps.Count > 0)
        {
            sections.Add(DrawSection("Mesh & Materials", assetProps, context));
        }

        if (lightingProps.Count > 0)
        {
            sections.Add(DrawSection("Rendering & Shadows", lightingProps, context));
        }

        return BuildComponentCard(component, "Mesh Instance", "\uE7C3", VStack(GhostLayout.SECTION_GAP, sections.ToArray()), context);
    }
}
