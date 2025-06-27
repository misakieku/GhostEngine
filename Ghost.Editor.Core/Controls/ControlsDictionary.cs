using Microsoft.UI.Xaml;

namespace Ghost.Editor.Core.Controls;

public partial class ControlsDictionary : ResourceDictionary
{
    private const string _DICTIONARY_PATH = "ms-appx:///Ghost.Editor.Core/Controls/ControlsDictionary.xaml";

    public ControlsDictionary()
    {
        Source = new Uri(_DICTIONARY_PATH, UriKind.Absolute);
    }
}