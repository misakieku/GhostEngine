using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.Systems;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.UnitTest.ECS;

[TestClass]
[DoNotParallelize]
public class TransformSystemTests
{
    private JobScheduler _scheduler = null!;
    private World _world = null!;

    [TestInitialize]
    public void Setup()
    {
        var desc = new JobSchedulerDesc
        {
            DependencyChainCapacity = 16,
            ThreadCount = 0,
        };
        _scheduler = new JobScheduler(in desc);
        _world = World.Create(_scheduler, 128);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _world.Dispose();
        _scheduler.Dispose();
    }

    [TestMethod]
    public void TestTransformSystem_StandaloneEntity()
    {
        ISystem system = new TransformSystem();
        var api = new SystemAPI { World = _world };
        var updateApi = new SystemUpdateAPI { World = _world };
        system.Initialize(in api);

        var entity = _world.EntityManager.CreateEntity(
            LocalTransform.FromPosition(new float3(1.0f, 2.0f, 3.0f)),
            new LocalToWorld { matrix = float4x4.identity });

        system.Update(in updateApi);

        ref readonly var ltw = ref _world.EntityManager.GetComponent<LocalToWorld>(entity);
        Assert.AreEqual(1.0f, ltw.matrix.c3.x, 1e-4f);
        Assert.AreEqual(2.0f, ltw.matrix.c3.y, 1e-4f);
        Assert.AreEqual(3.0f, ltw.matrix.c3.z, 1e-4f);

        system.Cleanup(in api);
    }

    [TestMethod]
    public void TestTransformSystem_HierarchyPropagation()
    {
        ISystem system = new TransformSystem();
        var api = new SystemAPI { World = _world };
        var updateApi = new SystemUpdateAPI { World = _world };
        system.Initialize(in api);

        var root = _world.EntityManager.CreateEntity(
            LocalTransform.FromPosition(new float3(10.0f, 0.0f, 0.0f)),
            new LocalToWorld { matrix = float4x4.identity },
            new Hierarchy { parent = Entity.Invalid, firstChild = Entity.Invalid, nextSibling = Entity.Invalid });

        var childA = _world.EntityManager.CreateEntity(
            LocalTransform.FromPosition(new float3(0.0f, 5.0f, 0.0f)),
            new LocalToWorld { matrix = float4x4.identity },
            new Hierarchy { parent = root, firstChild = Entity.Invalid, nextSibling = Entity.Invalid });

        var childB = _world.EntityManager.CreateEntity(
            LocalTransform.FromPosition(new float3(0.0f, -5.0f, 0.0f)),
            new LocalToWorld { matrix = float4x4.identity },
            new Hierarchy { parent = root, firstChild = Entity.Invalid, nextSibling = Entity.Invalid });

        var grandchild = _world.EntityManager.CreateEntity(
            LocalTransform.FromPosition(new float3(0.0f, 0.0f, 2.0f)),
            new LocalToWorld { matrix = float4x4.identity },
            new Hierarchy { parent = childA, firstChild = Entity.Invalid, nextSibling = Entity.Invalid });

        // Link hierarchy
        ref var rootH = ref _world.EntityManager.GetComponent<Hierarchy>(root);
        rootH.firstChild = childA;

        ref var childAH = ref _world.EntityManager.GetComponent<Hierarchy>(childA);
        childAH.nextSibling = childB;
        childAH.firstChild = grandchild;

        system.Update(in updateApi);

        ref readonly var rootLtw = ref _world.EntityManager.GetComponent<LocalToWorld>(root);
        Assert.AreEqual(10.0f, rootLtw.matrix.c3.x, 1e-4f);
        Assert.AreEqual(0.0f, rootLtw.matrix.c3.y, 1e-4f);
        Assert.AreEqual(0.0f, rootLtw.matrix.c3.z, 1e-4f);

        ref readonly var childALtw = ref _world.EntityManager.GetComponent<LocalToWorld>(childA);
        Assert.AreEqual(10.0f, childALtw.matrix.c3.x, 1e-4f);
        Assert.AreEqual(5.0f, childALtw.matrix.c3.y, 1e-4f);
        Assert.AreEqual(0.0f, childALtw.matrix.c3.z, 1e-4f);

        ref readonly var childBLtw = ref _world.EntityManager.GetComponent<LocalToWorld>(childB);
        Assert.AreEqual(10.0f, childBLtw.matrix.c3.x, 1e-4f);
        Assert.AreEqual(-5.0f, childBLtw.matrix.c3.y, 1e-4f);
        Assert.AreEqual(0.0f, childBLtw.matrix.c3.z, 1e-4f);

        ref readonly var grandchildLtw = ref _world.EntityManager.GetComponent<LocalToWorld>(grandchild);
        Assert.AreEqual(10.0f, grandchildLtw.matrix.c3.x, 1e-4f);
        Assert.AreEqual(5.0f, grandchildLtw.matrix.c3.y, 1e-4f);
        Assert.AreEqual(2.0f, grandchildLtw.matrix.c3.z, 1e-4f);

        system.Cleanup(in api);
    }

    [TestMethod]
    public void TestMathUtility_TRS_GetTRS_RoundTrip()
    {
        var pos = new float3(3.5f, -2.0f, 8.25f);
        var rot = quaternion.EulerXYZ(math.radians(new float3(25.0f, 65.0f, -15.0f)));
        var scale = new float3(1.5f, 2.0f, 0.75f);

        var matrix = float4x4.TRS(pos, rot, scale);
        matrix.GetTRS(out var outPos, out var outRot, out var outScale);

        Assert.AreEqual(pos.x, outPos.x, 1e-4f);
        Assert.AreEqual(pos.y, outPos.y, 1e-4f);
        Assert.AreEqual(pos.z, outPos.z, 1e-4f);

        Assert.AreEqual(scale.x, outScale.x, 1e-4f);
        Assert.AreEqual(scale.y, outScale.y, 1e-4f);
        Assert.AreEqual(scale.z, outScale.z, 1e-4f);

        var dot = math.abs(math.dot(rot, outRot));
        Assert.IsTrue(dot > 0.9999f, $"Rotation mismatch: expected {rot}, got {outRot} (|dot|={dot})");
    }
}
