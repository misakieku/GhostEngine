using Ghost.Editor.Core.SceneGraph;
using Ghost.Entities;

namespace Ghost.Editor.Core.Inspector;

public sealed class PropertyDrawerContext
{
    public required World World { get; init; }
    public required Entity Entity { get; init; }
    public required EntityNode EntityNode { get; init; }
    public required ComponentNode ComponentNode { get; init; }
}
