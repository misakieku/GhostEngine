using Microsoft.UI.Xaml.Data;

namespace Ghost.Editor.Utilities.Converters;

public partial class AssetPathToGlyphConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string path)
        {
            return null;
        }

        if (Directory.Exists(path))
        {
            return "\uE8B7";
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();

        // TODO: Use resource dictionary for icons.
        return extension switch
        {
            ".ghostscene" => "\uF159",
            ".fbx" or ".obj" => "\uF158",
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => "\uE91B",
            ".mp3" or ".wav" or ".ogg" => "\uE767",
            ".mp4" or ".avi" or ".mkv" => "\uE714",
            ".txt" or ".md" => "\uF000",
            ".cs" or ".hlsl" => "\uE943",
            _ => "\uE8A5",
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
