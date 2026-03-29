using Microsoft.UI.Xaml.Data;

namespace Ghost.Editor.Utilities.Converters;

public partial class GetDirectoryNameConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return value is string path ? Path.GetDirectoryName(path) : null;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}