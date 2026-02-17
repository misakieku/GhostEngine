using Ghost.Editor.Core.Contracts;

namespace Ghost.Editor.Core.Services;

internal class PreviewService : IPreviewService
{
    public string GetIconPath(string path, bool isDirectory, IconSize size)
    {
        string iconPath;
        if (isDirectory)
        {
            iconPath = "ms-appx:///Assets/EditorIcons/folder-{0}.png";
        }
        else
        {
            // TODO: Generate preview icons dynamically for known file types like images, meshes, materials, etc.
            var ext = Path.GetExtension(path);
            iconPath = ext switch
            {
                ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".tiff" or ".svg" => "ms-appx:///Assets/EditorIcons/image-{0}.png",
                _ => "ms-appx:///Assets/EditorIcons/document-{0}.png",
            };
        }

        var sizeIndex = size switch
        {
            IconSize.Small => "0",
            IconSize.Large => "1",
            _ => "0"
        };

        iconPath = string.Format(iconPath, sizeIndex);
        return iconPath;
    }
}