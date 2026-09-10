using Ghost.Entities;

namespace Ghost.Engine.Systems;

public class RenderSystemGroup : SystemGroup
{
    public RenderSystemGroup()
    {
        AddSystem<CameraRenderSystem>();
        AddSystem<AddGPUViewBufferSystem>();

        AddSystem<RemoveGPUInstanceSystem>();
        AddSystem<UpdateGPUInstanceSystem>();
        AddSystem<AddGPUInstanceSystem>();

        SortSystems();
    }
}
