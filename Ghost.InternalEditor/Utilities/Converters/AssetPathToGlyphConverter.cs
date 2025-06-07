using Microsoft.UI.Xaml.Data;
using System;
using System.IO;

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
            ".fbx" or ".obj" => "\uF158",
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => "\uE91B", // Image icon
            ".mp3" or ".wav" or ".ogg" => "\uE767", // Audio icon
            ".mp4" or ".avi" or ".mkv" => "\uE714", // Video icon
            ".txt" or ".md" => "\uF000", // Text file icon
            ".cs" or ".hlsl" => "\uE943", // Code file icon
            _ => "\uE8A5", // Default file icon
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
