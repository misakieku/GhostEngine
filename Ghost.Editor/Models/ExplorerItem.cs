using System.Collections.ObjectModel;

namespace Ghost.Editor.Models;

internal class ExplorerItem(string name, string path, bool isDirectory)
{
    public string Name
    {
        get;
    } = name;

    public string Path
    {
        get;
    } = path;

    public bool IsDirectory
    {
        get;
    } = isDirectory;

    public ObservableCollection<ExplorerItem>? Children
    {
        get;
        set;
    }
}