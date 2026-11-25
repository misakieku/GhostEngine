using Ghost.Core;
using Ghost.Entities;

namespace Ghost.Engine.Components;

[TransientComponent]
public struct SceneID : ISharedComponent
{
    [ReadOnlyInInspector]
    public ushort value;
}
