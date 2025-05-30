using Microsoft.UI.Xaml.Data;
using System;

namespace Ghost.Editor.Helpers.Converters;

public partial class GetDirectoryNameConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return value is string path ? System.IO.Path.GetDirectoryName(path) : null;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}