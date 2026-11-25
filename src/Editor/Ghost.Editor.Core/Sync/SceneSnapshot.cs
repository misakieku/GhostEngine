namespace Ghost.Editor.Core.Sync;

public sealed class SceneSnapshot
{
    public IReadOnlyList<SceneNode> Scenes { get; }
    public IReadOnlyList<EntityNode> Entities { get; }
    public int EntityCount => Entities.Count;
    public float FrameTimeMs { get; init; }
    public float Fps { get; init; }

    public SceneSnapshot(IReadOnlyList<EntityNode> entities, float frameTimeMs = 16.6f, float fps = 60.0f)
        : this(Array.Empty<SceneNode>(), entities, frameTimeMs, fps)
    {
    }

    public SceneSnapshot(IReadOnlyList<SceneNode> scenes, IReadOnlyList<EntityNode> entities, float frameTimeMs = 16.6f, float fps = 60.0f)
    {
        Scenes = scenes;
        Entities = entities;
        FrameTimeMs = frameTimeMs;
        Fps = fps;
    }
}
