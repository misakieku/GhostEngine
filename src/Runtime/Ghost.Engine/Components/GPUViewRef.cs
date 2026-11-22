using Ghost.Entities;

namespace Ghost.Engine.Components;

[TransientComponent]
public struct GPUViewRef : IComponentData
{
    public uint viewId;
}
