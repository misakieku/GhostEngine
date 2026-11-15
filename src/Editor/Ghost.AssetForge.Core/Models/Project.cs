using System.Text.Json.Serialization;

namespace Ghost.AssetForge.Core.Models;

public record Project
{
    public const string EXTENSION = ".ghostproj";

    public int SchemaVersion { get; init; } = 1;

    public string Name { get; init; } = "New Project";

    public string CsprojPath { get; init; } = string.Empty;

    public string[] AssetDirectories { get; init; } = new[] { "Assets" };

    public string CacheDirectory { get; init; } = "obj/AssetCache";

    public string BuildDirectory { get; init; } = "bin/Assets";

    public string DefaultScene { get; init; } = string.Empty;

    public BakeSettings BakeSettings { get; init; } = new BakeSettings();

    // Non-serialized runtime properties
    [JsonIgnore]
    public string RootPath { get; set; } = string.Empty;

    [JsonIgnore]
    public string ProjectFilePath { get; set; } = string.Empty;
}
