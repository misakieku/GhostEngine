using Ghost.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ghost.AssetForge.Core.Models;

public partial class BakeSettings : ObservableObject
{
    [ObservableProperty]
    private CompressionMethod _compression = CompressionMethod.LZ4;

    [ObservableProperty]
    private long _chunkSizeThreshold = 1024L * 1024L * 1024L; // Default 1GB
}
