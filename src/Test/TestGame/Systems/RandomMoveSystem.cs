using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.Systems;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace TestGame.Systems;

[UpdateBefore<RenderSystemGroup>]
internal class RandomMoveSystem : SystemBase
{
    private Identifier<EntityQuery> _cameraQueryID;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _cameraQueryID = QueryBuilder.New()
            .WithAll<MoveDst>()
            .WithAllRW<LocalToWorld>()
            .Build(systemAPI.World);

        RequireQueryForUpdate(_cameraQueryID);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float RandomFloat(float min, float max)
    {
        return Random.Shared.NextSingle() * (max - min) + min;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float3 MoveTowards(float3 current, float3 target, float maxDistanceDelta)
    {
        var toVector = target - current;
        var dist = math.length(toVector);
        if (dist <= maxDistanceDelta || dist == 0f)
        {
            return target;
        }
        return current + toVector / dist * maxDistanceDelta;
    }

    protected override void OnUpdate(scoped in SystemAPI systemAPI)
    {
        ref var cameraQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_cameraQueryID);

        foreach ((var moveDst, var ltw) in cameraQuery.GetComponentIterator<MoveDst, LocalToWorld>())
        {
            MathUtility.GetTRS(ltw.Get().matrix, out var position, out var rotation, out var scale);

            var oldDst = moveDst.Get().position;
            var range = moveDst.Get().range;

            float3 dst;
            if (math.distance(position, oldDst) < 0.1f)
            {
                dst = new float3(RandomFloat(-range.x, range.x), RandomFloat(-range.y, range.y), RandomFloat(-range.z, range.z));
                moveDst.Get().position = dst;
            }
            else
            {
                dst = oldDst;
            }

            if (moveDst.Get().updateRotation)
            {
                var lookAt = moveDst.Get().lookAt;
                var direction = math.normalize(lookAt - position);
                rotation = quaternion.LookRotation(direction, math.up());
            }

            position = MoveTowards(position, dst, 1f * systemAPI.Time.DeltaTime);

            var matrix = MathUtility.TRS(position, rotation, scale);
            ltw.Get().matrix = matrix;
        }
    }
}
