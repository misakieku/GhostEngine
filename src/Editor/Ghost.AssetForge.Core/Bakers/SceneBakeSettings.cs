namespace Ghost.AssetForge.Core.Bakers;

public partial class SceneBakeSettings : IBakeSettings
{
    public const int VERSION = 1;

    public int Version { get; set; } = VERSION;
}
