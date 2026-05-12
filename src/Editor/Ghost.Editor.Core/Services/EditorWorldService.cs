using Ghost.Editor.Core.SceneGraph;
using Ghost.Entities;
using Ghost.Engine.Core;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Core.Services;

[EditorInjection(EditorInjectionAttribute.ServiceLifetime.Singleton, typeof(EditorWorldService))]
public class EditorWorldService : IDisposable
{
    private const int _DEFAULT_ENTITY_CAPACITY = 1024;

    public World EditorWorld
    {
        get;
    }

    public ObservableCollection<SceneNode> RootNodes
    {
        get;
    } = new();

    public EditorWorldService()
    {
        EditorWorld = World.Create(entityCapacity: _DEFAULT_ENTITY_CAPACITY);
    }

    public void CreateDefaultScene()
    {
        var scene = SceneManager.CreateScene();
        var entity = EditorWorld.EntityManager.CreateEntity();
        EditorWorld.EntityManager.AddComponent(entity, new Engine.Components.SceneID
        {
            scene = scene
        });
    }

    public void RebuildSceneGraph()
    {
        RootNodes.Clear();
        var sceneNodes = SceneGraphBuilder.Build(EditorWorld);
        foreach (var node in sceneNodes)
        {
            RootNodes.Add(node);
        }
    }

    public void Dispose()
    {
        World.Destroy(EditorWorld.ID);
        GC.SuppressFinalize(this);
    }
}
