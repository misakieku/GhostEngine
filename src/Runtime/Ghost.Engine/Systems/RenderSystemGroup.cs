using Ghost.Entities;

namespace Ghost.Engine.Systems;

public class RenderSystemGroup : SystemGroup
{
    public RenderSystemGroup()
    {
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
