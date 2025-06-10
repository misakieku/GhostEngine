namespace Ghost.Editor.Models;

public abstract class Asset
{
    /// <summary>
    /// Get the Guid of the asset.
    /// </summary>
    public Guid GUID
    {
        get;
    } = Guid.NewGuid();

    /// <summary>
    /// True if the asset is a folder, false if it is a file.
    /// </summary>
    public bool IsFolder
    {
        get;
    }

    internal void GenerateMetadata()
    {
    }
}