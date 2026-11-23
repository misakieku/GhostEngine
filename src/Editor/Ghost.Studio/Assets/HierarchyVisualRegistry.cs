using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Graphics.Core;
using Ghost.Studio.Backend;
using System.Numerics;

namespace Ghost.Studio.Assets;

/// <summary>
/// Visual presentation descriptor for a <see cref="HierarchyNode"/> row in the Scene Hierarchy panel.
/// </summary>
internal readonly struct HierarchyVisualDescriptor
{
    public readonly string IconGlyph;
    public readonly Vector4 IconColor;
    public readonly Vector4? LabelColor;

    public HierarchyVisualDescriptor(string iconGlyph, Vector4 iconColor, Vector4? labelColor = null)
    {
        IconGlyph = iconGlyph;
        IconColor = iconColor;
        LabelColor = labelColor;
    }
}

/// <summary>
/// Resolves theme-aware icon glyphs and row label colors for <see cref="SceneNode"/> and <see cref="EntityNode"/> entries.
/// </summary>
internal sealed class HierarchyVisualRegistry
{
    public static HierarchyVisualRegistry Default { get; } = new();

    public HierarchyVisualDescriptor Resolve(HierarchyNode node, in EditorColorTokens tokens)
    {
        if (node is SceneNode)
        {
            return new HierarchyVisualDescriptor(EditorIcons.SCENE, tokens.SemanticWarning, tokens.TextPrimary);
        }

        if (node is not EntityNode entity)
        {
            return new HierarchyVisualDescriptor(EditorIcons.ENTITY, tokens.TextSecondary);
        }

        if (entity.PrefabLink != null)
        {
            return new HierarchyVisualDescriptor(EditorIcons.CUBE, tokens.AccentHovered, tokens.AccentHovered);
        }

        if (HasComponent(entity, typeof(DirectionalLight)) || HasComponent(entity, typeof(PunctualLight)))
        {
            return new HierarchyVisualDescriptor(EditorIcons.LIGHT, EditorColorTokens.FromRgba(0xF5, 0xC4, 0x51));
        }

        if (HasComponent(entity, typeof(Camera)))
        {
            return new HierarchyVisualDescriptor(EditorIcons.CAMERA, tokens.Accent);
        }

        if (HasComponent(entity, typeof(RenderVolume)))
        {
            return new HierarchyVisualDescriptor(EditorIcons.LAYERS, EditorColorTokens.FromRgba(0xB8, 0x8A, 0xE8));
        }

        if (HasComponent(entity, typeof(MeshInstance)))
        {
            return new HierarchyVisualDescriptor(EditorIcons.CUBE, EditorColorTokens.FromRgba(0x6C, 0xB8, 0xFF));
        }

        return new HierarchyVisualDescriptor(EditorIcons.ENTITY, tokens.TextSecondary);
    }

    private static bool HasComponent(EntityNode entity, Type componentType)
    {
        for (var i = 0; i < entity.Components.Count; i++)
        {
            if (entity.Components[i].ComponentType == componentType)
            {
                return true;
            }
        }

        return false;
    }
}
