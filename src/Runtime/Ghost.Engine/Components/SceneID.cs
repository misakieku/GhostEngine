using Ghost.Core;
using Ghost.Entities;
using Ghost.Engine.Editor;

namespace Ghost.Engine.Components;

public struct SceneID : ISharedComponent
{
    [ReadOnlyInInspector]
    public ushort value;
}
