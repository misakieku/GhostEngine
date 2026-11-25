using Ghost.Studio.Backend;
using Hexa.NET.ImGui;
using System.Numerics;

namespace Ghost.Studio.Assets;

/// <summary>
/// Display layout mode for the Project browser.
/// </summary>
internal enum AssetViewMode : byte
{
    List = 0,
    Grid = 1,
}

/// <summary>
/// Visual presentation descriptor for a Project browser entry, supporting both icon glyphs and future thumbnail previews.
/// </summary>
internal readonly struct AssetVisualDescriptor
{
    public readonly string IconGlyph;
    public readonly Vector4 IconColor;
    public readonly ImTextureRef? PreviewTexture;
    public readonly string TypeLabel;

    public bool HasPreviewTexture => PreviewTexture.HasValue;

    public AssetVisualDescriptor(string iconGlyph, Vector4 iconColor, ImTextureRef? previewTexture, string typeLabel)
    {
        IconGlyph = iconGlyph;
        IconColor = iconColor;
        PreviewTexture = previewTexture;
        TypeLabel = typeLabel;
    }
}

/// <summary>
/// Extension point for supplying live GPU thumbnail previews (e.g., textures, materials, meshes) in Grid view.
/// </summary>
internal interface IAssetPreviewProvider
{
    bool CanPreview(string extension);

    bool TryGetPreview(string fullPath, string extension, out ImTextureRef previewTexture);
}

/// <summary>
/// Maps file extensions and directories to Nerd Font icons and optional preview providers for List and Grid views.
/// </summary>
internal sealed class AssetVisualRegistry
{
    private readonly struct ExtensionRule
    {
        public readonly string IconGlyph;
        public readonly Func<EditorColorTokens, Vector4> ColorSelector;
        public readonly string TypeLabel;

        public ExtensionRule(string iconGlyph, Func<EditorColorTokens, Vector4> colorSelector, string typeLabel)
        {
            IconGlyph = iconGlyph;
            ColorSelector = colorSelector;
            TypeLabel = typeLabel;
        }
    }

    private readonly Dictionary<string, ExtensionRule> _extensionRules = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IAssetPreviewProvider> _previewProviders = new();

    public static AssetVisualRegistry CreateDefault()
    {
        var registry = new AssetVisualRegistry();
        registry.RegisterExtension(".gmat", EditorIcons.ASSET_MATERIAL, static t => EditorColorTokens.FromRgba(0xD4, 0x8A, 0xE8), "Material");
        registry.RegisterExtension(".gscene", EditorIcons.LAYERS, static t => t.SemanticWarning, "Scene");

        registry.RegisterExtensions(new[] { ".gshdr", ".gcomp", ".ggraph", ".hlsl" }, EditorIcons.ASSET_SHADER, static t => t.AccentHovered, "Shader");
        registry.RegisterExtensions(new[] { ".obj", ".fbx", ".gltf", ".glb" }, EditorIcons.ASSET_MESH, static t => EditorColorTokens.FromRgba(0x6C, 0xB8, 0xFF), "Mesh");
        registry.RegisterExtensions(new[] { ".png", ".jpg", ".jpeg", ".tga", ".dds", ".hdr", ".exr" }, EditorIcons.ASSET_TEXTURE, static t => t.SemanticSuccess, "Texture");
        registry.RegisterExtensions(new[] { ".wav", ".ogg", ".mp3", ".bank" }, EditorIcons.ASSET_AUDIO, static t => t.SemanticWarning, "Audio");
        registry.RegisterExtensions(new[] { ".json", ".xml" }, EditorIcons.ASSET_CONFIG, static t => t.TextSecondary, "Config");
        registry.RegisterExtension(".meta", EditorIcons.ASSET_META, static t => t.TextDisabled, "Meta");
        return registry;
    }

    public void RegisterExtension(string extension, string iconGlyph, Func<EditorColorTokens, Vector4> colorSelector, string typeLabel)
    {
        _extensionRules[extension] = new ExtensionRule(iconGlyph, colorSelector, typeLabel);
    }

    public void RegisterExtensions(ReadOnlySpan<string> extensions, string iconGlyph, Func<EditorColorTokens, Vector4> colorSelector, string typeLabel)
    {
        for (var i = 0; i < extensions.Length; i++)
        {
            _extensionRules[extensions[i]] = new ExtensionRule(iconGlyph, colorSelector, typeLabel);
        }
    }

    public void RegisterPreviewProvider(IAssetPreviewProvider provider)
    {
        _previewProviders.Add(provider);
    }

    public AssetVisualDescriptor Resolve(string fullPath, string extension, bool isDirectory, AssetViewMode viewMode, in EditorColorTokens tokens, bool isExpanded = false)
    {
        if (isDirectory)
        {
            var folderGlyph = isExpanded ? EditorIcons.FOLDER_OPEN : EditorIcons.FOLDER;
            return new AssetVisualDescriptor(folderGlyph, tokens.SemanticWarning, null, "Folder");
        }

        var iconGlyph = EditorIcons.FILE;
        var iconColor = tokens.TextSecondary;
        var typeLabel = "Asset";

        if (!string.IsNullOrEmpty(extension) && _extensionRules.TryGetValue(extension, out var rule))
        {
            iconGlyph = rule.IconGlyph;
            iconColor = rule.ColorSelector(tokens);
            typeLabel = rule.TypeLabel;
        }

        ImTextureRef? previewTexture = null;
        if (viewMode == AssetViewMode.Grid && !string.IsNullOrEmpty(extension))
        {
            for (var i = 0; i < _previewProviders.Count; i++)
            {
                var provider = _previewProviders[i];
                if (provider.CanPreview(extension) && provider.TryGetPreview(fullPath, extension, out var texRef))
                {
                    previewTexture = texRef;
                    break;
                }
            }
        }

        return new AssetVisualDescriptor(iconGlyph, iconColor, previewTexture, typeLabel);
    }
}
