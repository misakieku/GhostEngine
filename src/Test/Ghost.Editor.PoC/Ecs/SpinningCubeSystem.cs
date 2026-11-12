using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Entities;

namespace Ghost.Editor.PoC.Ecs;

public class SpinningCubeSystem : SystemBase
{
    private Identifier<EntityQuery> _query;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _query = QueryBuilder.New().WithAll<LocalTransform, SpinningTag>().Build(systemAPI.World);
    }

    protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        ref var query = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_query);
        foreach (ref var transform in query.GetComponentIterator<LocalTransform>())
        {
            transform.position.y = 1.0f + MathF.Sin(systemAPI.Time.ElapsedTime * 3.0f);
        }
    }
}
