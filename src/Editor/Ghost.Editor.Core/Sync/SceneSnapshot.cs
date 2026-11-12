namespace Ghost.Editor.Core.Sync;

public sealed class SceneSnapshot
{
    public IReadOnlyList<EntityNode> Entities { get; }
    public int EntityCount => Entities.Count;
    public float FrameTimeMs { get; init; }
    public float Fps { get; init; }

    public SceneSnapshot(IReadOnlyList<EntityNode> entities, float frameTimeMs = 16.6f, float fps = 60.0f)
    {
        Entities = entities;
        FrameTimeMs = frameTimeMs;
        Fps = fps;
    }
}
