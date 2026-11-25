using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.Volumes;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.UnitTest.ECS;

[TestClass]
[DoNotParallelize]
public class GeneratedFieldApplierTests
{
    private sealed class MockReferenceResolver : ISceneReferenceResolver
    {
        public Handle<Mesh> MeshHandle { get; init; }
        public Handle<GPUTexture> TextureHandle { get; init; }
        public int PaletteIndex { get; init; }
        public Entity TargetEntity { get; init; }

        public Handle<T> ResolveAssetHandle<T>(int assetDependencyIndex)
        {
            if (assetDependencyIndex < 0)
            {
                return Handle<T>.Invalid;
            }

            if (typeof(T) == typeof(Mesh))
            {
                return (Handle<T>)(object)MeshHandle;
            }

            if (typeof(T) == typeof(GPUTexture))
            {
                return (Handle<T>)(object)TextureHandle;
            }

            return Handle<T>.Invalid;
        }

        public int ResolveMaterialPalette(ReadOnlySpan<int> materialDependencyIndices)
        {
            return PaletteIndex;
        }

        public void ResolveEntityTarget(SceneEntityTarget target, out int entityId, out int entityGeneration)
        {
            if (target.kind == SceneTargetKind.Standalone && target.index == 0)
            {
                entityId = TargetEntity.ID;
                entityGeneration = TargetEntity.Generation;
                return;
            }

            entityId = -1;
            entityGeneration = 0;
        }
    }

    [TestMethod]
    public unsafe void ComponentRegistry_GeneratedFieldAppliers_PopulateScalarsEnumsVectorsHandlesAndEntities()
    {
        using var scheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 1, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 64 });
        using var world = World.Create(scheduler, 32);
        var em = world.EntityManager;

        var parentTarget = em.CreateEntity();
        var resolver = new MockReferenceResolver
        {
            MeshHandle = new Handle<Mesh>(7, 2),
            TextureHandle = new Handle<GPUTexture>(15, 3),
            PaletteIndex = 42,
            TargetEntity = parentTarget
        };

        var posInts = new long[] { 10, -4, 7 };
        var rotFloats = new float[] { 0.0f, 0.70710677f, 0.0f, 0.70710677f };
        var blobList = new List<byte>();
        var posOffset = AppendBytes(blobList, MemoryMarshal.AsBytes(posInts.AsSpan()));
        var rotOffset = AppendBytes(blobList, MemoryMarshal.AsBytes(rotFloats.AsSpan()));
        var spotOffset = AppendUtf8(blobList, "Spot");
        var gateOffset = AppendUtf8(blobList, "Horizontal");
        var blob = blobList.ToArray();
        var matDeps = new[] { 1, 2 };

        var transform = LocalTransform.Identity;
        var ltwId = ComponentRegistry.GetComponentIDByName(typeof(LocalTransform).FullName!);
        Assert.IsFalse(ltwId.IsInvalid);
        Assert.IsTrue(ComponentRegistry.HasFieldApplier(ltwId));

        ApplyField(ltwId, &transform, "position", new SceneFieldRecord { kind = SceneFieldValueKind.IntArray, payloadOffset = posOffset, payloadCount = 3 }, blob, matDeps, resolver);
        ApplyField(ltwId, &transform, "rotation", new SceneFieldRecord { kind = SceneFieldValueKind.FloatArray, payloadOffset = rotOffset, payloadCount = 4 }, blob, matDeps, resolver);
        Assert.AreEqual(new float3(10.0f, -4.0f, 7.0f), transform.position);
        Assert.AreEqual(0.70710677f, transform.rotation.value.y, 1e-5f);

        var light = default(PunctualLight);
        var lightId = ComponentRegistry.GetComponentIDByName(typeof(PunctualLight).FullName!);
        ApplyField(lightId, &light, "type", new SceneFieldRecord { kind = SceneFieldValueKind.String, payloadOffset = spotOffset.Offset, payloadCount = spotOffset.Length }, blob, matDeps, resolver);
        ApplyField(lightId, &light, "intensity", new SceneFieldRecord { kind = SceneFieldValueKind.Double, scalarValue = BitConverter.DoubleToInt64Bits(1250.5) }, blob, matDeps, resolver);
        Assert.AreEqual(PunctualLightType.Spot, light.type);
        Assert.AreEqual(1250.5f, light.intensity, 1e-3f);

        var camera = Camera.Default;
        var camId = ComponentRegistry.GetComponentIDByName(typeof(Camera).FullName!);
        ApplyField(camId, &camera, "gateFit", new SceneFieldRecord { kind = SceneFieldValueKind.String, payloadOffset = gateOffset.Offset, payloadCount = gateOffset.Length }, blob, matDeps, resolver);
        ApplyField(camId, &camera, "renderingLayerMask", new SceneFieldRecord { kind = SceneFieldValueKind.UInt64, scalarValue = 0x55L }, blob, matDeps, resolver);
        ApplyField(camId, &camera, "colorTarget", new SceneFieldRecord { kind = SceneFieldValueKind.AssetHandle, payloadOffset = 0 }, blob, matDeps, resolver);
        Assert.AreEqual(GateFit.Horizontal, camera.gateFit);
        Assert.AreEqual(0x55u, (uint)camera.renderingLayerMask);
        Assert.AreEqual(resolver.TextureHandle, camera.colorTarget);

        var meshInst = default(MeshInstance);
        var meshInstId = ComponentRegistry.GetComponentIDByName(typeof(MeshInstance).FullName!);
        ApplyField(meshInstId, &meshInst, "mesh", new SceneFieldRecord { kind = SceneFieldValueKind.AssetHandle, payloadOffset = 0 }, blob, matDeps, resolver);
        ApplyField(meshInstId, &meshInst, "materialPalette", new SceneFieldRecord { kind = SceneFieldValueKind.MaterialPalette, payloadOffset = 0, payloadCount = 2 }, blob, matDeps, resolver);
        ApplyField(meshInstId, &meshInst, "staticShadowCaster", new SceneFieldRecord { kind = SceneFieldValueKind.Bool, scalarValue = 1L }, blob, matDeps, resolver);
        Assert.AreEqual(resolver.MeshHandle, meshInst.mesh);
        Assert.AreEqual(42, meshInst.materialPalette.Value);
        Assert.IsTrue(meshInst.staticShadowCaster);

        var hier = Hierarchy.Root;
        var hierId = ComponentRegistry.GetComponentIDByName(typeof(Hierarchy).FullName!);
        var parentRefRec = new SceneFieldRecord
        {
            kind = SceneFieldValueKind.EntityRef,
            entityTarget = new SceneEntityTarget { kind = SceneTargetKind.Standalone, index = 0, subIndex = -1 }
        };
        ApplyField(hierId, &hier, "parent", in parentRefRec, blob, matDeps, resolver);
        Assert.AreEqual(parentTarget, hier.parent);

        Assert.IsFalse(ComponentRegistry.HasFieldApplier(ComponentTypeID<SceneID>.Value));
        Assert.IsFalse(ComponentRegistry.HasFieldApplier(ComponentTypeID<GPUInstanceRef>.Value));
        Assert.IsFalse(ComponentRegistry.HasFieldApplier(ComponentTypeID<GPUViewRef>.Value));
    }

    [TestMethod]
    public unsafe void VolumeRegistry_GeneratedFieldAppliers_PopulateNullableOverridesCleanly()
    {
        var tintFloats = new float[] { 0.8f, 0.4f, 0.2f };
        var blobList = new List<byte>();
        var tintOffset = AppendBytes(blobList, MemoryMarshal.AsBytes(tintFloats.AsSpan()));
        var (expOffset, expLen) = AppendUtf8(blobList, "AutomaticHistogram");
        var blob = blobList.ToArray();

        var resolver = new MockReferenceResolver
        {
            TextureHandle = new Handle<GPUTexture>(99, 1)
        };

        var container = RenderVolumeDataContainer.Create();
        try
        {
            Assert.IsTrue(VolumeRegistry.TryGetBlenderByName(typeof(BloomSettings).FullName!, out var bloomReg));
            Assert.IsTrue(container.TryGetOrAddDefault(bloomReg.typeId, out var bloomPtr));

            var thresholdRec = new SceneFieldRecord { fieldNameHash = CacheFileHeader.Fnv1a("threshold"), kind = SceneFieldValueKind.Double, scalarValue = BitConverter.DoubleToInt64Bits(0.45) };
            var tintRec = new SceneFieldRecord { fieldNameHash = CacheFileHeader.Fnv1a("tint"), kind = SceneFieldValueKind.FloatArray, payloadOffset = tintOffset, payloadCount = 3 };
            var hqRec = new SceneFieldRecord { fieldNameHash = CacheFileHeader.Fnv1a("highQuality"), kind = SceneFieldValueKind.Bool, scalarValue = 1L };

            var thresholdReader = new SceneFieldReader(in thresholdRec, blob, ReadOnlySpan<int>.Empty, resolver);
            Assert.IsTrue(VolumeRegistry.TryApplySerializedField(bloomReg.typeId, bloomPtr, thresholdRec.fieldNameHash, in thresholdReader));
            Assert.IsTrue(VolumeRegistry.TryApplySerializedField(bloomReg.typeId, bloomPtr, tintRec.fieldNameHash, new SceneFieldReader(in tintRec, blob, ReadOnlySpan<int>.Empty, resolver)));
            Assert.IsTrue(VolumeRegistry.TryApplySerializedField(bloomReg.typeId, bloomPtr, hqRec.fieldNameHash, new SceneFieldReader(in hqRec, blob, ReadOnlySpan<int>.Empty, resolver)));

            Assert.IsTrue(container.TryGet<BloomSettings>(out var bloom));
            Assert.AreEqual(0.45f, bloom.threshold);
            Assert.AreEqual(new float3(0.8f, 0.4f, 0.2f), bloom.tint);
            Assert.IsTrue(bloom.highQuality);
            Assert.IsNull(bloom.scatter);
            Assert.IsNull(bloom.clamp);

            Assert.IsTrue(VolumeRegistry.TryGetBlenderByName(typeof(HDRISettings).FullName!, out var hdriReg));
            Assert.IsTrue(container.TryGetOrAddDefault(hdriReg.typeId, out var hdriPtr));
            var texRec = new SceneFieldRecord { fieldNameHash = CacheFileHeader.Fnv1a("texture"), kind = SceneFieldValueKind.AssetHandle, payloadOffset = 0 };
            Assert.IsTrue(VolumeRegistry.TryApplySerializedField(hdriReg.typeId, hdriPtr, texRec.fieldNameHash, new SceneFieldReader(in texRec, blob, ReadOnlySpan<int>.Empty, resolver)));
            Assert.IsTrue(container.TryGet<HDRISettings>(out var hdri));
            Assert.AreEqual(resolver.TextureHandle, hdri.texture);

            Assert.IsTrue(VolumeRegistry.TryGetBlenderByName(typeof(ExposureSettings).FullName!, out var expReg));
            Assert.IsTrue(container.TryGetOrAddDefault(expReg.typeId, out var expPtr));
            var modeRec = new SceneFieldRecord { fieldNameHash = CacheFileHeader.Fnv1a("mode"), kind = SceneFieldValueKind.String, payloadOffset = expOffset, payloadCount = expLen };
            Assert.IsTrue(VolumeRegistry.TryApplySerializedField(expReg.typeId, expPtr, modeRec.fieldNameHash, new SceneFieldReader(in modeRec, blob, ReadOnlySpan<int>.Empty, resolver)));
            Assert.IsTrue(container.TryGet<ExposureSettings>(out var exp));
            Assert.AreEqual(ExposureMode.AutomaticHistogram, exp.mode);
        }
        finally
        {
            container.Dispose();
        }
    }

    private static unsafe void ApplyField(Identifier<IComponent> compId, void* compPtr, string fieldName, in SceneFieldRecord record, byte[] blob, int[] matDeps, ISceneReferenceResolver resolver)
    {
        var rec = record;
        rec.fieldNameHash = CacheFileHeader.Fnv1a(fieldName);
        var reader = new SceneFieldReader(in rec, blob, matDeps, resolver);
        Assert.IsTrue(ComponentRegistry.TryApplySerializedField(compId, compPtr, rec.fieldNameHash, in reader));
    }

    private static int AppendBytes(List<byte> blob, ReadOnlySpan<byte> bytes)
    {
        var offset = blob.Count;
        for (var i = 0; i < bytes.Length; i++)
        {
            blob.Add(bytes[i]);
        }

        return offset;
    }

    private static (int Offset, int Length) AppendUtf8(List<byte> blob, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        return (AppendBytes(blob, bytes), bytes.Length);
    }
}
