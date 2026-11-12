using Ghost.Entities;

namespace Ghost.Editor.PoC.Ecs;

public struct CounterComponent : IComponentData
{
    public int Value;
    public int Step;
    public bool AutoIncrement;
}
