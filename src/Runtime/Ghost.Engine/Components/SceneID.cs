using Ghost.Core.Attributes;
using Ghost.Entities;

namespace Ghost.Engine.Components;

public struct SceneID : ISharedComponent
{
    [ReadOnlyInInspector]
    public ushort value;
}
