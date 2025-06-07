using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Editor.SceneGraph;
using Ghost.Entities;
using System.Collections.ObjectModel;

namespace Ghost.Editor.ViewModels.Pages.EngineEditor;

internal partial class HierarchyViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<WorldNode> SceneList
    {
        get;
        private set;
    } = new();

    public HierarchyViewModel()
    {
        // Test only
        var testWorld = World.Create();
        var entity1 = SceneGraphHelpers.CreateEntityNode(testWorld, "entity 1");
        var entity2 = SceneGraphHelpers.CreateEntityNode(testWorld, "entity 3");
        var entity3 = SceneGraphHelpers.CreateEntityNode(testWorld, "entity 4");
        var entity4 = SceneGraphHelpers.CreateEntityNode(testWorld, "entity 5");
        var entity5 = SceneGraphHelpers.CreateEntityNode(testWorld, "entity 2");

        var testScene = new WorldNode(testWorld, "Test Scene");

        SceneGraphHelpers.AttachChild(testScene, entity1, entity2);
        SceneGraphHelpers.AttachChild(testScene, entity1, entity3);
        SceneGraphHelpers.AttachChild(testScene, entity2, entity4);

        testScene.AddChild(entity1);
        testScene.AddChild(entity5);

        SceneList.Add(testScene);
    }
}