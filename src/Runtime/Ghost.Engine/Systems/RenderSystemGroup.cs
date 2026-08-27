using Ghost.Entities;

namespace Ghost.Engine.Systems;

public class RenderSystemGroup : SystemGroup
{
    public RenderSystemGroup()
    {
        AddSystem<RemoveGPUInstanceSystem>();
        AddSystem<UpdateGPUInstanceSystem>();
        AddSystem<AddGPUInstanceSystem>();
        AddSystem<CameraRenderSystem>();
        SortSystems();
    }
}
