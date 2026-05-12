using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Engine.Streaming;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Models;

internal partial class ExplorerItem : ObservableObject
{
    public string Name
    {
        get;
    }

    public string Path
    {
        get;
    }

    public bool IsDirectory
    {
        get;
    }


    public AssetType AssetType
    {
        get;
    }

    [ObservableProperty]
    public partial ObservableCollection<ExplorerItem>? Children
    {
        get; set;
    }

    [ObservableProperty]
    public partial string IconGlyph
    {
        get; set;
    }

    public ExplorerItem(string name, string path, bool isDirectory, AssetType assetType = AssetType.Unknown)
    {
        Name = name;
        Path = path;
        IsDirectory = isDirectory;
        AssetType = assetType;

        if (IsDirectory)
        {
            IconGlyph = "\uE8B7"; // Folder icon
        }
        else
        {
            IconGlyph = GetIconGlyphForAssetType(assetType);
        }
    }

    private string GetIconGlyphForAssetType(AssetType assetType)
    {
        return assetType switch
        {
            AssetType.Texture => "\uEB9F", // Image icon
            AssetType.Material => "\uE943", // Document/Material
            AssetType.Shader => "\uE9E9", // Code
            AssetType.Mesh => "\uE8B3", // 3D icon
            AssetType.Audio => "\uE8D6", // Audio
            _ => "\uE7C3" // Default file icon
        };
    }
}