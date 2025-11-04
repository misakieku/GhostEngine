namespace Ghost.Data.Models;

public class ProjectMetadata
{
    public const string PROJECT_EXTENSION = "ghostproj";

    public Guid ID
    {
        get; set;
    }

    public string Name
    {
        get; set;
    }

    public Version EngineVersion
    {
        get; set;
    }

    public DateTime CreatedAt
    {
        get; set;
    }

    public DateTime LastOpened
    {
        get; set;
    }

    public ProjectMetadata(string name, Version engineVersion)
    {
        ID = Guid.NewGuid();
        Name = name;
        EngineVersion = engineVersion;
        CreatedAt = DateTime.UtcNow;
        LastOpened = DateTime.UtcNow;
    }

    // Parameterless constructor for deserialization
    public ProjectMetadata()
    {
    }
}

public readonly struct ProjectMetadataInfo(string path, ProjectMetadata metadata)
{
    public readonly string Path => path;
    public readonly ProjectMetadata Metadata => metadata;
}