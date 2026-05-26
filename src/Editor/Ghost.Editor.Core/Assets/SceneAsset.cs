using System.Runtime.InteropServices;

namespace Ghost.Editor.Core.Assets;

[Guid(GUID)]
public sealed class SceneAsset : IAsset
{
    public const string GUID = "1B5E3F2A-8D91-4C67-BE32-A0F9C6D4E781";

    private static readonly Guid s_typeID = Guid.Parse(GUID);

    public Guid ID
    {
        get;
    }

    public Guid TypeID => s_typeID;

    public IAssetSettings? Settings
    {
        get;
    }

    public string SceneName
    {
        get; set;
    }

    public int EntityCount
    {
        get; set;
    }

    public SceneAsset(Guid id, IAssetSettings? settings)
    {
        ID = id;
        Settings = settings;
        SceneName = string.Empty;
        EntityCount = 0;
    }

    public void Dispose()
    {
    }
}

public sealed class SceneAssetSettings : IAssetSettings
{
}
