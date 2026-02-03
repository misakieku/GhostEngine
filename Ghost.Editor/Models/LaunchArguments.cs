namespace Ghost.Editor.Models;

[AttributeUsage(AttributeTargets.Property)]
internal sealed class ArgumentNameAttribute : Attribute
{
    public string Name
    {
        get; 
    }

    public ArgumentNameAttribute(string name)
    {
        Name = name;
    }
}

internal class LaunchArguments
{
    [ArgumentName("project-path")]
    public string ProjectPath
    {
        get; set;
    } = string.Empty;

    [ArgumentName("project-name")]
    public string ProjectName
    {
        get; set;
    } = string.Empty;

    public bool IsValid()
    {
        return Directory.Exists(ProjectPath) && !string.IsNullOrWhiteSpace(ProjectName);
    }
}