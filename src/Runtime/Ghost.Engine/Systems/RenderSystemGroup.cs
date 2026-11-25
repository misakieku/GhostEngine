namespace Ghost.Engine.Systems;

public class RenderSystemGroup : CoreRenderSystemGroup
{
    public RenderSystemGroup() : base(false)
    {
        AddSystem<RenderVolumeSystem>();
        AddSystem<CameraRenderSystem>();
        AddSystem<AddGPUViewSystem>();
        AddSystem<RemoveGPUViewSystem>();

        SortSystems();
    }
}
