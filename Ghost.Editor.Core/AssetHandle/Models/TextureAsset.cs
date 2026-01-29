namespace Ghost.Editor.Core.AssetHandle;

/// <summary>
/// Represents a texture asset.
/// </summary>
public class TextureAsset : Asset
{
    public override string Name
    {
        get;
        set;
    }

    /// <summary>
    /// Width of the texture in pixels.
    /// </summary>
    public uint Width
    {
        get;
        set;
    }

    /// <summary>
    /// Height of the texture in pixels.
    /// </summary>
    public uint Height
    {
        get;
        set;
    }

    /// <summary>
    /// Number of mipmap levels.
    /// </summary>
    public uint MipLevels
    {
        get;
        set;
    }

    /// <summary>
    /// Texture format (e.g., "RGBA8", "BC1", "BC7").
    /// </summary>
    public string Format
    {
        get;
        set;
    }

    /// <summary>
    /// Whether the texture uses sRGB color space.
    /// </summary>
    public bool IsSRGB
    {
        get;
        set;
    }

    /// <summary>
    /// Relative path to the source image file.
    /// </summary>
    public string SourcePath
    {
        get;
        set;
    }

    public TextureAsset(Guid id, string name) : base(id)
    {
        Name = name;
        Format = "RGBA8";
        IsSRGB = true;
        SourcePath = string.Empty;
    }
}
