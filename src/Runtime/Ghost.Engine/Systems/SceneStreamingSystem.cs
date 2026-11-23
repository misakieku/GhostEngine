using Ghost.Engine.Core;
using Ghost.Engine.RenderPipeline;
using Ghost.Entities;

namespace Ghost.Engine.Systems;

/// <summary>
/// Advances pending asynchronous scene loads within per-frame entity and time budgets before transform evaluation.
/// </summary>
[RenderPipelineSystem<GhostRenderPipelineSettings>]
[UpdateBefore<TransformSystem>]
public class SceneStreamingSystem : SystemBase
{
    /// <summary>
    /// Per-frame budget used when materializing queued scenes into this world.
    /// </summary>
    public SceneMaterializeBudget Budget { get; set; } = SceneMaterializeBudget.Default;

    protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        SceneManager.MaterializePendingScenes(systemAPI.World, Budget);
    }

    protected override void OnCleanup(scoped in SystemAPI systemAPI)
    {
        SceneManager.ReleaseWorldScenes(systemAPI.World);
    }
}
