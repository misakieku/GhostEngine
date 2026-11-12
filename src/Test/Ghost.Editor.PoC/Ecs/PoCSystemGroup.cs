using Ghost.Entities;

namespace Ghost.Editor.PoC.Ecs;

public sealed class PoCSystemGroup : SystemGroup
{
    public PoCSystemGroup()
    {
        AddSystem<CounterWorkerSystem>();
        SortSystems();
    }
}
