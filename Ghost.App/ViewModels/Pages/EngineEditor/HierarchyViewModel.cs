using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Editor.SceneGraph;
using System;
using System.Collections.ObjectModel;

namespace Ghost.Editor.ViewModels.Pages.EngineEditor;

internal partial class HierarchyViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    public partial ObservableCollection<WorldNode> SceneList
    {
        get;
        private set;
    } = new(EditorWorldManager.LoadedWorlds);

    public HierarchyViewModel()
    {
        EditorWorldManager.OnWorldLoaded += OnWorldLoaded;
        EditorWorldManager.OnWorldUnloaded += OnWorldUnloaded;
    }

    private void OnWorldLoaded(WorldNode node)
    {
        SceneList.Add(node);
    }

    private void OnWorldUnloaded(WorldNode node)
    {
        SceneList.Remove(node);
    }

    public void Dispose()
    {
        EditorWorldManager.OnWorldLoaded -= OnWorldLoaded;
        EditorWorldManager.OnWorldUnloaded -= OnWorldUnloaded;
    }
}