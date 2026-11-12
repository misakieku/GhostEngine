using Ghost.Core;
using Ghost.Entities;

namespace Ghost.Editor.PoC.Ecs;

public class CounterWorkerSystem : SystemBase
{
    private Identifier<EntityQuery> _query;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _query = QueryBuilder.New().WithAll<CounterComponent>().Build(systemAPI.World);
    }

    protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        ref var query = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_query);
        foreach (ref var counter in query.GetComponentIterator<CounterComponent>())
        {
            if (counter.AutoIncrement)
            {
                counter.Value += counter.Step > 0 ? counter.Step : 1;
            }
        }
    }
}
