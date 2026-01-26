namespace Ghost.Editor.Core.AssetHandle;

internal class AssetMeta
{
    public Guid Guid
    {
        get;
        set;
    }

    public ImporterSettings? Settings
    {
        get;
        set;
    }
}
