using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.SceneGraph;
using System.Collections.ObjectModel;

namespace Ghost.Editor.ViewModels.Pages.EngineEditor;

internal partial class HierarchyViewModel : ObservableObject, INavigationAware
{
    //[ObservableProperty]
    //public partial ObservableCollection<SceneNode> SceneList
    //{
    //    get;
    //    private set;
    //} = new(EditorSceneManager.LoadedWorlds);

    //private void OnWorldLoaded(SceneNode node)
    //{
    //    SceneList.Add(node);
    //}

    //private void OnWorldUnloaded(SceneNode node)
    //{
    //    SceneList.Remove(node);
    //}

    public void OnNavigatedTo(object? parameter)
    {
        //EditorSceneManager.OnWorldLoaded += OnWorldLoaded;
        //EditorSceneManager.OnWorldUnloaded += OnWorldUnloaded;
    }

    public void OnNavigatedFrom()
    {
        //EditorSceneManager.OnWorldLoaded -= OnWorldLoaded;
        //EditorSceneManager.OnWorldUnloaded -= OnWorldUnloaded;
    }
}
