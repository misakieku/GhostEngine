using Ghost.Engine.Systems;
using Ghost.Studio.Rendering;

namespace Ghost.Studio.Systems;

internal sealed class EditorRenderSystemGroup : CoreRenderSystemGroup
{
    public EditorRenderSystemGroup(SceneViewportTarget viewportTarget) : base(false, false)
    {
        AddSystem(new EditorTransformSystem());
        AddSystem(new EditorUpdateGPUInstanceSystem());
        AddSystem(new EditorCameraRenderSystem(viewportTarget));

        SortSystems();
    }
}
