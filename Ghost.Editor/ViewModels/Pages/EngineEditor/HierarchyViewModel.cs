using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.SceneGraph;
using System.Collections.ObjectModel;

namespace Ghost.Editor.ViewModels.Pages.EngineEditor;

internal partial class HierarchyViewModel : ObservableObject, INavigationAware
{
    [ObservableProperty]
    public partial ObservableCollection<WorldNode> SceneList
    {
        get;
        private set;
    } = new(EditorWorldManager.LoadedWorlds);

    private void OnWorldLoaded(WorldNode node)
    {
        SceneList.Add(node);
    }

    private void OnWorldUnloaded(WorldNode node)
    {
        SceneList.Remove(node);
    }

    public void OnNavigatedTo(object? parameter)
    {
        EditorWorldManager.OnWorldLoaded += OnWorldLoaded;
        EditorWorldManager.OnWorldUnloaded += OnWorldUnloaded;
    }

    public void OnNavigatedFrom()
    {
        EditorWorldManager.OnWorldLoaded -= OnWorldLoaded;
        EditorWorldManager.OnWorldUnloaded -= OnWorldUnloaded;
    }
}