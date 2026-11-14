using Ghost.Entities;

namespace Ghost.Engine.Systems;

public class CoreRenderSystemGroup : SystemGroup
{
    public CoreRenderSystemGroup() : this(true, true)
    {
    }

    protected CoreRenderSystemGroup(bool autoSort, bool includeSimulationChangeSystems = true)
    {
        if (includeSimulationChangeSystems)
        {
            AddSystem<TransformSystem>();
            AddSystem<UpdateGPUInstanceSystem>();
        }

        AddSystem<RenderVolumeCleanupSystem>();
        AddSystem<RemoveGPUInstanceSystem>();
        AddSystem<AddGPUInstanceSystem>();
        AddSystem<LightGatherSystem>();

        if (autoSort)
        {
            SortSystems();
        }
    }
}
