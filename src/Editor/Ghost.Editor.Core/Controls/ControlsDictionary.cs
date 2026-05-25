using Microsoft.UI.Xaml;

namespace Ghost.Editor.Core.Controls;

public partial class ControlsDictionary : ResourceDictionary
{
    private const string DICTIONARY_PATH = "ms-appx:///Ghost.Editor.Core/Controls/ControlsDictionary.xaml";

    public ControlsDictionary()
    {
        Source = new Uri(DICTIONARY_PATH, UriKind.Absolute);
    }
}