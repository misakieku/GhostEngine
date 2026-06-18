using Ghost.Core;
using Ghost.Editor.Core.Assets;
using Ghost.Editor.Core.SceneGraph;
using Ghost.Entities;
using Ghost.Engine.Core;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Core.Contracts;

public interface IEditorWorldService : IDisposable
{
    World EditorWorld { get; }
    ObservableCollection<SceneNode> RootNodes { get; }

    event Action<Entity, string, ushort>? EntityCreated;
    event Action<Entity>? EntityDestroyed;
    event Action<Entity, Entity, Entity>? EntityParentChanged;
    event Action<Entity, string>? EntityNameChanged;
    event Action? SceneGraphRebuilt;

    void ChangeEntityScene(Entity entity, ushort sceneID);
    void CreateDefaultScene();
    Task<Scene> OpenSceneAsync(Guid assetGuid);
    void CreateEntity(string name, ushort sceneID, Entity parent = default);
    void Defer(Action action);
    void DestroyEntity(Entity entity);
    void FirePendingEvents();
    void FlushCommands();
    ushort GetEntitySceneID(Entity entity);
    SceneAsset? GetAssetForScene(ushort sceneID);
    void RegisterSceneAsset(ushort sceneID, SceneAsset asset);
    void RebuildSceneGraph(Dictionary<Entity, string>? initialNames = null);
    Error RemoveParent(Entity child);
    void RenameEntity(Entity entity, string newName);
    Error SetParent(Entity child, Entity parent);
}
