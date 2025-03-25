using Ghost.Database.Models.Projects;
using System;
using System.IO;

namespace Ghost.Editor.Models.Warpers;

internal class TemplateInfoWarper(string templatePath, TemplateInfo info)
{
    private const string _ICON_NAME = "icon.png";
    private const string _PREVIEW_NAME = "preview.png";

    public string directory = Path.GetDirectoryName(templatePath)!;

    public TemplateInfo Info => info;

    public Uri GetIconURI()
    {
        return new Uri(Path.Combine(directory, _ICON_NAME));
    }

    public Uri GetPreviewURI()
    {
        return new Uri(Path.Combine(directory, _PREVIEW_NAME));
    }
}