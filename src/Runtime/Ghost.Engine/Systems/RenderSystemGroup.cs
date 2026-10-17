using Ghost.Entities;

namespace Ghost.Engine.Systems;

public class RenderSystemGroup : SystemGroup
{
    public RenderSystemGroup()
    {
        AddSystem<RenderVolumeCleanupSystem>();
        AddSystem<RenderVolumeSystem>();
        AddSystem<CameraRenderSystem>();
        AddSystem<AddGPUViewSystem>();
        AddSystem<RemoveGPUViewSystem>();

        AddSystem<RemoveGPUInstanceSystem>();
        AddSystem<UpdateGPUInstanceSystem>();
        AddSystem<AddGPUInstanceSystem>();

        AddSystem<LightGatherSystem>();

        SortSystems();
    }
}
