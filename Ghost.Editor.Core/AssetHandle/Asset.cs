namespace Ghost.Editor.Core.AssetHandle;

/// <summary>
/// The base class for all asset types in the Ghost Editor.
/// </summary>
public abstract class Asset
{
    public abstract string Name
    {
        get; set;
    }

    public Guid ID
    {
        get;
    }

    protected Asset(Guid id)
    {
        ID = id;
    }
}
