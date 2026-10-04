using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Core;

namespace Ghost.AssetForge.Core.Models;

public partial class BakeSettings : ObservableObject
{
    [ObservableProperty]
    public partial CompressionMethod Compression { get; set; } = CompressionMethod.Zstd;

    [ObservableProperty]
    public partial long ChunkSizeThreshold { get; set; } = 1024L * 1024L * 1024L;
}
