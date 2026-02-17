namespace Ghost.Editor.Core.Contracts;

public enum IconSize
{
    Small,
    Large
}

public interface IPreviewService
{
    string GetIconPath(string path, bool isDirectory, IconSize size);
}