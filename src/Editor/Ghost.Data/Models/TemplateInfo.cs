namespace Ghost.Data.Models;

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
}

public struct TemplateData(string templatePath, TemplateInfo info)
{
    private const string _ICON_NAME = "icon.png";
    private const string _PREVIEW_NAME = "preview.png";

    public string directory = Path.GetDirectoryName(templatePath)!;

    public readonly TemplateInfo Info => info;

    public readonly Uri GetIconURI()
    {
        return new Uri(Path.Combine(directory, _ICON_NAME));
    }

    public readonly Uri GetPreviewURI()
    {
        return new Uri(Path.Combine(directory, _PREVIEW_NAME));
    }
}