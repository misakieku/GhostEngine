namespace Ghost.Data.Models;

public class ProjectMetadata
{
    public const string PROJECT_FILE_EXTENSION_NAME = "gproj";

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
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ProjectMetadata()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }
}

public readonly struct ProjectMetadataInfo(string path, ProjectMetadata metadata)
{
    public readonly string Path => path;
    public readonly ProjectMetadata Metadata => metadata;
}