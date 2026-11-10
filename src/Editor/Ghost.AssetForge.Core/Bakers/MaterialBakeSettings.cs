using CommunityToolkit.Mvvm.ComponentModel;

namespace Ghost.AssetForge.Core.Bakers;

public partial class MaterialBakeSettings : ObservableObject, IBakeSettings
{
    public const int VERSION = 1;

    public int Version { get; set; } = VERSION;
}
