using System.Text.Json.Serialization;

namespace Ghost.AssetForge.Core.Models;

public record Project
{
    public string Name { get; init; } = "New Project";

    public BakeSettings BakeSettings { get; init; } = new BakeSettings();

    // Non-serialized property
    [JsonIgnore]
    public string RootPath { get; set; } = string.Empty;
}
