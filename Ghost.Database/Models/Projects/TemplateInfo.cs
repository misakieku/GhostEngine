namespace Ghost.Database.Models.Projects;

public class TemplateInfo
{
    public required string Name
    {
        get; set;
    }

    public string? Description
    {
        get; set;
    }

    public required Version TemplateVersion
    {
        get; set;
    }

    public required Version EngineVersion
    {
        get; set;
    }

    public Dictionary<string, Version>? Packages
    {
        get; set;
    }
}