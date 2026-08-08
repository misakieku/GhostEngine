namespace Ghost.AssetForge.Core.Models;

/// <summary>
/// Outcome of a bake run: how many assets were processed, skipped, or failed,
/// plus the virtual paths of every failed asset.
/// </summary>
public sealed record BakeResult(
    int Total,
    int Succeeded,
    int Skipped,
    int Failed,
    IReadOnlyList<string> FailedAssets);
