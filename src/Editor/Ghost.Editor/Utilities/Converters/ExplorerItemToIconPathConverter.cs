using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Models;
using Microsoft.UI.Xaml.Data;

namespace Ghost.Editor.Utilities.Converters;

public partial class ExplorerItemToIconUriConverter : IValueConverter
{
    private readonly IPreviewService _previewService = App.GetService<IPreviewService>();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ExplorerItem item)
        {
            var path = _previewService.GetIconPath(item.Path, item.IsDirectory, IconSize.Small);
            return new Uri(path);
        }

        throw new NotSupportedException();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
