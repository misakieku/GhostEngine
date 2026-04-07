using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Graphics.Test.Systems;

internal class CameraMovingSystem : ISystem
{
    private Identifier<EntityQuery> _cameraQueryID;

    private random _random;
    private float3 _target;

    public void Initialize(ref readonly SystemAPI systemAPI)
    {
        _cameraQueryID = QueryBuilder.Create()
            .WithAll<Camera, LocalToWorld>()
            .Build(systemAPI.World, true);

        _random = new random(123456);
        _target = _random.NextFloat3(-20, 20);
    }

    public void Update(ref readonly SystemAPI systemAPI)
    {
        ref var cameraQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_cameraQueryID);

        foreach (ref var ltw in cameraQuery.GetComponentIterator<LocalToWorld>())
        {
            var position = ltw.matrix.c3.xyz;
            if (math.distance(position, _target) < 0.1f)
            {
                _target = _random.NextFloat3(-20, 20);
            }

            var newPosition = math.lerp(position, _target, 0.025f);
            var forward = math.normalize(new float3(0f, 0.5f, 0f) - newPosition);

            var rotation = quaternion.LookRotation(forward, math.up());
            var matrix = float4x4.TRS(newPosition, rotation, float3.one);
            ltw.matrix = matrix;
        }
    }

    public void Cleanup(ref readonly SystemAPI systemAPI)
    {
    }
}
