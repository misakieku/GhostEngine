namespace Ghost.AssetForge.Core.Bakers;

public partial class MaterialBakeSettings : IBakeSettings
{
    public const int VERSION = 1;

    public int Version { get; set; } = VERSION;
}
