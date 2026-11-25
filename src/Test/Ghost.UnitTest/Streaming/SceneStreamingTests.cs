using Ghost.Core;
using Ghost.Engine;
using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Engine.Streaming;
using Ghost.Engine.Systems;
using Ghost.Engine.Volumes;
using Ghost.Entities;
using Ghost.Graphics.Services;
using Ghost.UnitTest.MockingEnvironment;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;
using System.Text;

namespace Ghost.UnitTest.Streaming;

[TestClass]
[DoNotParallelize]
public unsafe class SceneStreamingTests
{
    private sealed class TestRig : IDisposable
    {
        public MockingGraphicsEngine Engine { get; }
        public MockingRenderDevice RenderDevice { get; }
        public MockingResourceDatabase ResourceDatabase { get; }
        public MockingPipelineLibrary PipelineLibrary { get; }
        public MockingResourceAllocator ResourceAllocator { get; }
        public ShaderLibrary ShaderLibrary { get; }
        public ResourceManager ResourceManager { get; }
        public JobScheduler JobScheduler { get; }
        public MockingContentProvider ContentProvider { get; }
        public ResourceStreamingProcessor StreamingProcessor { get; }
        public AssetManager AssetManager { get; }

        public TestRig()
        {
            Engine = new MockingGraphicsEngine();
            RenderDevice = new MockingRenderDevice();
            ResourceDatabase = new MockingResourceDatabase();
            PipelineLibrary = new MockingPipelineLibrary();
            ResourceAllocator = new MockingResourceAllocator(ResourceDatabase);
            ShaderLibrary = new ShaderLibrary(null, PipelineLibrary, null);
            ResourceManager = new ResourceManager(RenderDevice, ResourceAllocator, ResourceDatabase);
            JobScheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 2, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 1024 });
            ContentProvider = new MockingContentProvider();
            StreamingProcessor = new ResourceStreamingProcessor();
            AssetManager = new AssetManager(ResourceDatabase, ResourceManager, ContentProvider, StreamingProcessor, JobScheduler, Engine, ShaderLibrary, 2);
        }

        public void Dispose()
        {
            AssetManager.Dispose();
            ContentProvider.Dispose();
            JobScheduler.Dispose();
            ResourceManager.Dispose();
            ShaderLibrary.Dispose();
            ResourceAllocator.Dispose();
            PipelineLibrary.Dispose();
            ResourceDatabase.Dispose();
            RenderDevice.Dispose();
            Engine.Dispose();
        }
    }

    private sealed class ScenePayloadBuilder
    {
        private readonly List<string> _strings = new();
        private readonly Dictionary<string, int> _stringMap = new(StringComparer.Ordinal);
        private readonly List<SceneEntityHeader> _entities = new();
        private readonly List<ScenePrefabSpawnHeader> _prefabs = new();
        private readonly List<SceneComponentHeader> _components = new();
        private readonly List<SceneFieldRecord> _fields = new();
        private readonly List<SceneVolumeOverrideHeader> _volumes = new();
        private readonly List<int> _materialDepIndices = new();
        private readonly List<ScenePrefabNameOverrideHeader> _nameOverrides = new();
        private readonly List<SceneAddedComponentHeader> _addedComponents = new();
        private readonly List<SceneRemovedComponentHeader> _removedComponents = new();
        private readonly List<ScenePropertyOverrideHeader> _propertyOverrides = new();
        private readonly List<ScenePrefabVolumeOverrideHeader> _prefabVolumeOverrides = new();
        private readonly List<byte> _dataBlob = new();

        public int Intern(string s)
        {
            if (_stringMap.TryGetValue(s, out var idx))
            {
                return idx;
            }

            idx = _strings.Count;
            _strings.Add(s);
            _stringMap[s] = idx;
            return idx;
        }

        public SceneFieldRecord FloatArrayField(string name, params float[] values)
        {
            Align(sizeof(float));
            var offset = _dataBlob.Count;
            var bytes = MemoryMarshal.AsBytes(values.AsSpan());
            for (var i = 0; i < bytes.Length; i++)
            {
                _dataBlob.Add(bytes[i]);
            }

            return new SceneFieldRecord
            {
                fieldNameHash = CacheFileHeader.Fnv1a(name),
                kind = SceneFieldValueKind.FloatArray,
                payloadOffset = offset,
                payloadCount = values.Length,
                entityTarget = SceneEntityTarget.Invalid
            };
        }

        public static SceneFieldRecord DoubleField(string name, double value)
        {
            return new SceneFieldRecord
            {
                fieldNameHash = CacheFileHeader.Fnv1a(name),
                kind = SceneFieldValueKind.Double,
                scalarValue = BitConverter.DoubleToInt64Bits(value),
                payloadOffset = -1,
                entityTarget = SceneEntityTarget.Invalid
            };
        }

        public static SceneFieldRecord BoolField(string name, bool value)
        {
            return new SceneFieldRecord
            {
                fieldNameHash = CacheFileHeader.Fnv1a(name),
                kind = SceneFieldValueKind.Bool,
                scalarValue = value ? 1L : 0L,
                payloadOffset = -1,
                entityTarget = SceneEntityTarget.Invalid
            };
        }

        public SceneFieldRecord StringField(string name, string value)
        {
            var offset = _dataBlob.Count;
            var bytes = Encoding.UTF8.GetBytes(value);
            _dataBlob.AddRange(bytes);
            return new SceneFieldRecord
            {
                fieldNameHash = CacheFileHeader.Fnv1a(name),
                kind = SceneFieldValueKind.String,
                payloadOffset = offset,
                payloadCount = bytes.Length,
                entityTarget = SceneEntityTarget.Invalid
            };
        }

        public static SceneFieldRecord AssetField(string name, int depIndex)
        {
            return new SceneFieldRecord
            {
                fieldNameHash = CacheFileHeader.Fnv1a(name),
                kind = SceneFieldValueKind.AssetHandle,
                payloadOffset = depIndex,
                entityTarget = SceneEntityTarget.Invalid
            };
        }

        public static SceneFieldRecord EntityRefField(string name, SceneEntityTarget target)
        {
            return new SceneFieldRecord
            {
                fieldNameHash = CacheFileHeader.Fnv1a(name),
                kind = SceneFieldValueKind.EntityRef,
                payloadOffset = -1,
                entityTarget = target
            };
        }

        public ScenePrefabVolumeOverrideHeader BuildPrefabVolumeOverride(int templateEntityIndex, string volumeType, bool removed, params SceneFieldRecord[] fields)
        {
            var start = _fields.Count;
            _fields.AddRange(fields);
            return new ScenePrefabVolumeOverrideHeader
            {
                templateEntityIndex = templateEntityIndex,
                volumeTypeNameIndex = Intern(volumeType),
                removed = removed ? (byte)1 : (byte)0,
                fieldStartIndex = start,
                fieldCount = fields.Length
            };
        }

        public SceneComponentHeader AddComponent(string typeName, ReadOnlySpan<SceneFieldRecord> fields, ReadOnlySpan<(string VolType, SceneFieldRecord[] VolFields)> volumes = default)
        {
            var fStart = _fields.Count;
            for (var i = 0; i < fields.Length; i++)
            {
                _fields.Add(fields[i]);
            }

            var vStart = _volumes.Count;
            for (var v = 0; v < volumes.Length; v++)
            {
                var vfStart = _fields.Count;
                for (var f = 0; f < volumes[v].VolFields.Length; f++)
                {
                    _fields.Add(volumes[v].VolFields[f]);
                }

                _volumes.Add(new SceneVolumeOverrideHeader
                {
                    typeNameIndex = Intern(volumes[v].VolType),
                    fieldStartIndex = vfStart,
                    fieldCount = volumes[v].VolFields.Length
                });
            }

            return new SceneComponentHeader
            {
                typeNameIndex = Intern(typeName),
                fieldStartIndex = fStart,
                fieldCount = fields.Length,
                volumeOverrideStartIndex = vStart,
                volumeOverrideCount = volumes.Length
            };
        }

        public int AddEntity(SceneEntityTarget parentTarget, params SceneComponentHeader[] comps)
        {
            var idx = _entities.Count;
            var start = _components.Count;
            _components.AddRange(comps);
            _entities.Add(new SceneEntityHeader
            {
                parentTarget = parentTarget,
                componentStartIndex = start,
                componentCount = comps.Length
            });
            return idx;
        }

        public void AddPrefabInstance(int depIndex, SceneEntityTarget parentTarget, SceneAddedComponentHeader[]? added = null, SceneRemovedComponentHeader[]? removed = null, ScenePropertyOverrideHeader[]? overrides = null, ScenePrefabVolumeOverrideHeader[]? volumeOverrides = null)
        {
            var aStart = _addedComponents.Count;
            if (added != null)
            {
                _addedComponents.AddRange(added);
            }

            var rStart = _removedComponents.Count;
            if (removed != null)
            {
                _removedComponents.AddRange(removed);
            }

            var pStart = _propertyOverrides.Count;
            if (overrides != null)
            {
                _propertyOverrides.AddRange(overrides);
            }

            var vStart = _prefabVolumeOverrides.Count;
            if (volumeOverrides != null)
            {
                _prefabVolumeOverrides.AddRange(volumeOverrides);
            }

            _prefabs.Add(new ScenePrefabSpawnHeader
            {
                prefabDependencyIndex = depIndex,
                parentTarget = parentTarget,
                addedComponentStartIndex = aStart,
                addedComponentCount = added?.Length ?? 0,
                removedComponentStartIndex = rStart,
                removedComponentCount = removed?.Length ?? 0,
                propertyOverrideStartIndex = pStart,
                propertyOverrideCount = overrides?.Length ?? 0,
                volumeOverrideStartIndex = vStart,
                volumeOverrideCount = volumeOverrides?.Length ?? 0
            });
        }

        public byte[] BuildBytes()
        {
            using var strStream = new MemoryStream();
            for (var i = 0; i < _strings.Count; i++)
            {
                var b = Encoding.UTF8.GetBytes(_strings[i]);
                var len = (ushort)b.Length;
                strStream.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref len, 1)));
                strStream.Write(b);
            }
            var strBytes = strStream.ToArray();

            var offset = (long)sizeof(SceneContentHeader);
            var strOff = offset; offset += strBytes.Length;
            var entOff = offset; offset += (long)_entities.Count * sizeof(SceneEntityHeader);
            var prfOff = offset; offset += (long)_prefabs.Count * sizeof(ScenePrefabSpawnHeader);
            var cmpOff = offset; offset += (long)_components.Count * sizeof(SceneComponentHeader);
            var fldOff = offset; offset += (long)_fields.Count * sizeof(SceneFieldRecord);
            var volOff = offset; offset += (long)_volumes.Count * sizeof(SceneVolumeOverrideHeader);
            var matOff = offset; offset += (long)_materialDepIndices.Count * sizeof(int);
            var namOff = offset; offset += (long)_nameOverrides.Count * sizeof(ScenePrefabNameOverrideHeader);
            var addOff = offset; offset += (long)_addedComponents.Count * sizeof(SceneAddedComponentHeader);
            var remOff = offset; offset += (long)_removedComponents.Count * sizeof(SceneRemovedComponentHeader);
            var ovrOff = offset; offset += (long)_propertyOverrides.Count * sizeof(ScenePropertyOverrideHeader);
            var pvOff = offset; offset += (long)_prefabVolumeOverrides.Count * sizeof(ScenePrefabVolumeOverrideHeader);
            var blobOff = offset;

            var header = new SceneContentHeader
            {
                magic = SceneContentHeader.MAGIC,
                version = SceneContentHeader.VERSION,
                stringCount = _strings.Count,
                entityCount = _entities.Count,
                prefabInstanceCount = _prefabs.Count,
                componentCount = _components.Count,
                fieldCount = _fields.Count,
                volumeOverrideCount = _volumes.Count,
                materialDependencyIndexCount = _materialDepIndices.Count,
                nameOverrideCount = _nameOverrides.Count,
                addedComponentCount = _addedComponents.Count,
                removedComponentCount = _removedComponents.Count,
                propertyOverrideCount = _propertyOverrides.Count,
                prefabVolumeOverrideCount = _prefabVolumeOverrides.Count,
                dataBlobSize = _dataBlob.Count,
                stringsOffset = strOff,
                entitiesOffset = entOff,
                prefabInstancesOffset = prfOff,
                componentsOffset = cmpOff,
                fieldsOffset = fldOff,
                volumeOverridesOffset = volOff,
                materialDependencyIndicesOffset = matOff,
                nameOverridesOffset = namOff,
                addedComponentsOffset = addOff,
                removedComponentsOffset = remOff,
                propertyOverridesOffset = ovrOff,
                prefabVolumeOverridesOffset = pvOff,
                dataBlobOffset = blobOff
            };

            using var dst = new MemoryStream();
            dst.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)));
            dst.Write(strBytes);
            WriteList(dst, _entities);
            WriteList(dst, _prefabs);
            WriteList(dst, _components);
            WriteList(dst, _fields);
            WriteList(dst, _volumes);
            WriteList(dst, _materialDepIndices);
            WriteList(dst, _nameOverrides);
            WriteList(dst, _addedComponents);
            WriteList(dst, _removedComponents);
            WriteList(dst, _propertyOverrides);
            WriteList(dst, _prefabVolumeOverrides);
            if (_dataBlob.Count > 0)
            {
                dst.Write(CollectionsMarshal.AsSpan(_dataBlob));
            }

            return dst.ToArray();
        }

        private void Align(int align)
        {
            var rem = _dataBlob.Count % align;
            if (rem != 0)
            {
                for (var i = 0; i < align - rem; i++)
                {
                    _dataBlob.Add(0);
                }
            }
        }

        private static void WriteList<T>(Stream dst, List<T> list)
            where T : unmanaged
        {
            if (list.Count > 0)
            {
                dst.Write(MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(list)));
            }
        }
    }

    private static byte[] BuildTwoNodePrefabBytes(Guid meshGuid, out float3 defaultChildPos)
    {
        defaultChildPos = new float3(0, 2, 0);
        var rootTransform = LocalTransform.Identity;
        var childTransform = LocalTransform.FromPosition(defaultChildPos);
        var identityLtw = new LocalToWorld { matrix = float4x4.identity };
        var rootHier = new PrefabEntityHeader { parentIndex = -1, firstChildIndex = 1, nextSiblingIndex = -1, componentCount = 3 };
        var childHier = new PrefabEntityHeader { parentIndex = 0, firstChildIndex = -1, nextSiblingIndex = -1, componentCount = 4 };

        var typeNames = new[]
        {
            typeof(LocalTransform).FullName!,
            typeof(LocalToWorld).FullName!,
            typeof(Hierarchy).FullName!,
            typeof(MeshInstance).FullName!
        };

        using var strMs = new MemoryStream();
        foreach (var t in typeNames)
        {
            var b = Encoding.UTF8.GetBytes(t);
            var l = (ushort)b.Length;
            strMs.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref l, 1)));
            strMs.Write(b);
        }
        var strBytes = strMs.ToArray();

        using var entMs = new MemoryStream();
        WriteEntityHeaderAndComp(entMs, rootHier, 0, rootTransform);
        WriteComp(entMs, 1, identityLtw);
        WriteComp(entMs, 2, Hierarchy.Root);

        WriteEntityHeaderAndComp(entMs, childHier, 0, childTransform);
        WriteComp(entMs, 1, identityLtw);
        WriteComp(entMs, 2, Hierarchy.Root);
        WriteComp(entMs, 3, default(MeshInstance));
        var entBytes = entMs.ToArray();

        var fixup = new PrefabMeshMaterialFixup
        {
            entityIndex = 1,
            meshDependencyIndex = 0,
            materialCount = 0,
            materialDependencyStartIndex = 0
        };

        var typesOff = (long)sizeof(PrefabContentHeader);
        var entsOff = typesOff + strBytes.Length;
        var fixupsOff = entsOff + entBytes.Length;

        var header = new PrefabContentHeader
        {
            magic = PrefabContentHeader.MAGIC,
            version = PrefabContentHeader.VERSION,
            entityCount = 2,
            materialDependencyIndexCount = 0,
            componentTypeCount = typeNames.Length,
            meshMaterialFixupCount = 1,
            materialDependencyIndicesOffset = typesOff,
            componentTypesOffset = typesOff,
            entitiesOffset = entsOff,
            meshMaterialFixupsOffset = fixupsOff
        };

        using var dst = new MemoryStream();
        dst.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)));
        dst.Write(strBytes);
        dst.Write(entBytes);
        dst.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref fixup, 1)));
        return dst.ToArray();

        static void WriteEntityHeaderAndComp<T>(Stream s, PrefabEntityHeader eh, int compTypeIdx, T val)
            where T : unmanaged
        {
            s.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref eh, 1)));
            WriteComp(s, compTypeIdx, val);
        }

        static void WriteComp<T>(Stream s, int compTypeIdx, T val)
            where T : unmanaged
        {
            var ch = new PrefabComponentHeader { componentTypeIndex = compTypeIdx, dataSize = sizeof(T) };
            s.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref ch, 1)));
            s.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref val, 1)));
        }
    }

    [TestMethod]
    public void SceneManager_LoadSceneAsync_WaitsOnGpuGateFlattensPrefabsAndAppliesOverrides()
    {
        using var rig = new TestRig();
        using var world = World.Create(rig.JobScheduler, 64);

        var meshGuid = Guid.NewGuid();
        rig.ContentProvider.AddMockMesh(meshGuid);

        var prefabGuid = Guid.NewGuid();
        var prefabBytes = BuildTwoNodePrefabBytes(meshGuid, out _);
        rig.ContentProvider.AddMockAsset(prefabGuid, new MockingContentProvider.MockAssetData
        {
            type = AssetType.EntityPrefab,
            data = prefabBytes,
            dependencies = new[] { meshGuid }
        });

        var sceneGuid = Guid.NewGuid();
        var sb = new ScenePayloadBuilder();

        // Standalone 0: RenderVolume with Bloom threshold = 0.65
        var rvComp = sb.AddComponent(typeof(RenderVolume).FullName!, new[]
        {
            ScenePayloadBuilder.BoolField("isGlobal", true),
            ScenePayloadBuilder.DoubleField("priority", 1.0),
            ScenePayloadBuilder.DoubleField("weight", 1.0)
        });
        var volDataComp = sb.AddComponent(
            typeof(RenderVolumeDataContainer).FullName!,
            ReadOnlySpan<SceneFieldRecord>.Empty,
            new[] { (typeof(BloomSettings).FullName!, new[] { ScenePayloadBuilder.DoubleField("threshold", 0.65) }) });
        sb.AddEntity(SceneEntityTarget.Invalid, rvComp, volDataComp);

        // Standalone 1: Child of Prefab node 1 (templateEntityIndex=1)
        var childCompLT = sb.AddComponent(typeof(LocalTransform).FullName!, new[]
        {
            sb.FloatArrayField("position", 5.0f, 0.0f, 0.0f),
            sb.FloatArrayField("rotation", 0.0f, 0.0f, 0.0f, 1.0f),
            sb.FloatArrayField("scale", 1.0f, 1.0f, 1.0f)
        });
        var childCompLTW = sb.AddComponent(typeof(LocalToWorld).FullName!, ReadOnlySpan<SceneFieldRecord>.Empty);
        sb.AddEntity(new SceneEntityTarget { kind = SceneTargetKind.PrefabNode, index = 0, subIndex = 1 }, childCompLT, childCompLTW);

        // Prefab Instance 0 with added PunctualLight on node 1 + LocalTransform.position override on node 0
        var lightAdded = sb.AddComponent(typeof(PunctualLight).FullName!, new[]
        {
            sb.StringField("type", "Spot"),
            ScenePayloadBuilder.DoubleField("intensity", 600.0)
        });
        var ovrField = sb.FloatArrayField("position", 10.0f, 20.0f, 30.0f);
        sb.AddPrefabInstance(
            depIndex: 0,
            parentTarget: SceneEntityTarget.Invalid,
            added: new[] { new SceneAddedComponentHeader { templateEntityIndex = 1, component = lightAdded } },
            removed: null,
            overrides: new[]
            {
                new ScenePropertyOverrideHeader
                {
                    templateEntityIndex = 0,
                    componentTypeNameIndex = sb.Intern(typeof(LocalTransform).FullName!),
                    field = ovrField
                }
            });

        rig.ContentProvider.AddMockAsset(sceneGuid, new MockingContentProvider.MockAssetData
        {
            type = AssetType.Scene,
            data = sb.BuildBytes(),
            dependencies = new[] { prefabGuid }
        });
        rig.ContentProvider.RegisterPath("Scenes/Battle", sceneGuid);

        var loadRes = SceneManager.LoadSceneAsync(world, rig.AssetManager, "Scenes/Battle", SceneLoadingType.Single);
        Assert.IsTrue(loadRes.IsSuccess, loadRes.Message);
        var op = loadRes.Value;

        Assert.IsTrue(rig.AssetManager.TryGetEntry(sceneGuid, out var sceneEntry));
        rig.JobScheduler.Wait(sceneEntry.LoadJobHandle);

        // GPU dependency gate: mesh is Loaded (waiting upload), so scene stays in WaitingForDependencies.
        Assert.IsTrue(rig.AssetManager.TryGetEntry(meshGuid, out var meshEntry));
        Assert.AreEqual(AssetState.Loaded, meshEntry.State);
        SceneManager.MaterializePendingScenes(world, SceneMaterializeBudget.Unlimited);
        Assert.AreEqual(SceneLoadStatus.WaitingForDependencies, op.Status);
        Assert.IsLessThan(0.80f, op.Progress);

        // Complete upload for mock mesh -> GPU dependency gate opens -> Materialize runs cleanly.
        meshEntry.State = AssetState.Ready;
        SceneManager.MaterializePendingScenes(world, SceneMaterializeBudget.Unlimited);

        Assert.IsTrue(op.IsMaterialized);
        Assert.AreEqual(SceneLoadStatus.Completed, op.Status);
        Assert.AreEqual(1.0f, op.Progress);

        using var scope = AllocationManager.CreateStackScope();
        using var sceneEntities = SceneManager.GetSceneEntities(world, op.Scene, scope.AllocationHandle);
        Assert.AreEqual(4, sceneEntities.Count);

        var em = world.EntityManager;
        var foundSpotLight = false;
        var foundOverriddenRoot = false;
        var foundVolumeBloom = false;
        var foundParentedChild = false;

        for (var i = 0; i < sceneEntities.Count; i++)
        {
            var e = sceneEntities[i];
            if (em.HasComponent<PunctualLight>(e))
            {
                foundSpotLight = true;
                var l = em.GetComponent<PunctualLight>(e);
                Assert.AreEqual(PunctualLightType.Spot, l.type);
                Assert.AreEqual(600.0f, l.intensity);
            }

            if (em.HasComponent<LocalTransform>(e))
            {
                var lt = em.GetComponent<LocalTransform>(e);
                if (math.all(lt.position == new float3(10.0f, 20.0f, 30.0f)))
                {
                    foundOverriddenRoot = true;
                }

                if (math.all(lt.position == new float3(5.0f, 0.0f, 0.0f)))
                {
                    Assert.IsTrue(em.HasComponent<Hierarchy>(e));
                    var parent = em.GetComponent<Hierarchy>(e).parent;
                    Assert.IsTrue(parent.IsValid);
                    Assert.IsTrue(em.HasComponent<PunctualLight>(parent));
                    Assert.AreEqual(new float3(15.0f, 22.0f, 30.0f), em.GetComponent<LocalToWorld>(e).matrix.c3.xyz);
                    foundParentedChild = true;
                }
            }

            if (em.HasComponent<RenderVolumeDataContainer>(e))
            {
                var rvdc = em.GetComponent<RenderVolumeDataContainer>(e);
                Assert.IsTrue(rvdc.TryGet<BloomSettings>(out var bloom));
                Assert.AreEqual(0.65f, bloom.threshold);
                foundVolumeBloom = true;
            }
        }

        Assert.IsTrue(foundSpotLight);
        Assert.IsTrue(foundOverriddenRoot);
        Assert.IsTrue(foundVolumeBloom);
        Assert.IsTrue(foundParentedChild);

        SceneManager.UnloadScene(op.Scene, world);
        Assert.IsFalse(rig.AssetManager.TryGetEntry(sceneGuid, out _));
        Assert.IsFalse(rig.AssetManager.TryGetEntry(prefabGuid, out _));
        Assert.IsFalse(rig.AssetManager.TryGetEntry(meshGuid, out _));
    }

    [TestMethod]
    public void SceneManager_SingleAndAdditive_PreservesPersistentEntitiesAndReusesSceneIDCleanly()
    {
        using var rig = new TestRig();
        using var world = World.Create(rig.JobScheduler, 64);

        var persistentEntity = world.EntityManager.CreateEntity(LocalTransform.Identity);

        var sceneAGuid = Guid.NewGuid();
        var sceneBGuid = Guid.NewGuid();
        var sceneCGuid = Guid.NewGuid();
        var sb = new ScenePayloadBuilder();
        sb.AddEntity(SceneEntityTarget.Invalid, sb.AddComponent(typeof(LocalTransform).FullName!, new[] { sb.FloatArrayField("position", 1, 2, 3) }));
        var sceneBytes = sb.BuildBytes();

        rig.ContentProvider.AddMockAsset(sceneAGuid, new MockingContentProvider.MockAssetData { type = AssetType.Scene, data = sceneBytes });
        rig.ContentProvider.AddMockAsset(sceneBGuid, new MockingContentProvider.MockAssetData { type = AssetType.Scene, data = sceneBytes });
        rig.ContentProvider.AddMockAsset(sceneCGuid, new MockingContentProvider.MockAssetData { type = AssetType.Scene, data = sceneBytes });

        var opA = SceneManager.LoadSceneAsync(world, rig.AssetManager, sceneAGuid, SceneLoadingType.Additive).GetValueOrThrow();
        var opB = SceneManager.LoadSceneAsync(world, rig.AssetManager, sceneBGuid, SceneLoadingType.Additive).GetValueOrThrow();
        WaitUntilLoadJobsDone(rig, sceneAGuid);
        WaitUntilLoadJobsDone(rig, sceneBGuid);
        SceneManager.WaitForCompletion(world, opA);
        SceneManager.WaitForCompletion(world, opB);

        Assert.AreEqual(1, CountSceneEntities(world, opA.Scene));
        Assert.AreEqual(1, CountSceneEntities(world, opB.Scene));

        SceneManager.UnloadScene(opA.Scene, world);
        Assert.AreEqual(0, CountSceneEntities(world, opA.Scene));
        Assert.IsFalse(rig.AssetManager.TryGetEntry(sceneAGuid, out _));
        Assert.AreEqual(1, CountSceneEntities(world, opB.Scene));

        var opC = SceneManager.LoadSceneAsync(world, rig.AssetManager, sceneCGuid, SceneLoadingType.Single).GetValueOrThrow();
        WaitUntilLoadJobsDone(rig, sceneCGuid);
        SceneManager.WaitForCompletion(world, opC);

        Assert.AreEqual(0, CountSceneEntities(world, opB.Scene));
        Assert.IsFalse(rig.AssetManager.TryGetEntry(sceneBGuid, out _));
        Assert.AreEqual(1, CountSceneEntities(world, opC.Scene));
        Assert.IsTrue(world.EntityManager.Exists(persistentEntity));

        SceneManager.UnloadScene(opC.Scene, world);
        Assert.IsFalse(rig.AssetManager.TryGetEntry(sceneCGuid, out _));

        static int CountSceneEntities(World targetWorld, Scene scene)
        {
            using var scope = AllocationManager.CreateStackScope();
            using var entities = SceneManager.GetSceneEntities(targetWorld, scene, scope.AllocationHandle);
            return entities.Count;
        }
    }

    [TestMethod]
    public void SceneStreamingSystem_SlicedMaterialization_AdvancesMonotonicallyAcrossFrames()
    {
        using var rig = new TestRig();
        using var world = World.Create(rig.JobScheduler, 64);

        var streamingSystem = new SceneStreamingSystem { Budget = new SceneMaterializeBudget(maxEntities: 1, maxScenes: 1) };
        var transformSystem = new TransformSystem();
        world.SystemManager.AddSystem(streamingSystem);
        world.SystemManager.AddSystem(transformSystem);
        world.SystemManager.InitializeAll();

        try
        {
            var sb = new ScenePayloadBuilder();
            for (var i = 0; i < 4; i++)
            {
                sb.AddEntity(SceneEntityTarget.Invalid, sb.AddComponent(typeof(LocalTransform).FullName!, new[]
                {
                    sb.FloatArrayField("position", i, 0, 0),
                    sb.FloatArrayField("scale", 1, 1, 1)
                }), sb.AddComponent(typeof(LocalToWorld).FullName!, ReadOnlySpan<SceneFieldRecord>.Empty));
            }

            var sceneGuid = Guid.NewGuid();
            rig.ContentProvider.AddMockAsset(sceneGuid, new MockingContentProvider.MockAssetData { type = AssetType.Scene, data = sb.BuildBytes() });
            rig.ContentProvider.RegisterPath("Scenes/Sliced", sceneGuid);

            var op = SceneManager.LoadSceneAsync(world, rig.AssetManager, "Scenes/Sliced", SceneLoadingType.Single).GetValueOrThrow();
            WaitUntilLoadJobsDone(rig, sceneGuid);

            var prevProgress = 0.0f;
            var sawIntermediateMaterializing = false;
            var ticks = 0;
            var time = new TimeData { DeltaTime = 1.0f / 60.0f };

            while (!op.IsCompleted && ticks < 32)
            {
                world.SystemManager.UpdateAll(time);
                Assert.IsGreaterThanOrEqualTo(prevProgress, op.Progress);
                prevProgress = op.Progress;
                if (op.Status == SceneLoadStatus.Materializing)
                {
                    sawIntermediateMaterializing = true;
                }

                ticks++;
            }

            Assert.IsTrue(sawIntermediateMaterializing);
            Assert.IsTrue(op.IsCompleted);
            Assert.AreEqual(SceneLoadStatus.Completed, op.Status);
            Assert.AreEqual(1.0f, op.Progress);
            Assert.IsGreaterThan(2, ticks);
        }
        finally
        {
            world.SystemManager.CleanupAll();
        }
    }

    [TestMethod]
    public void SceneManager_CorruptPayload_FailsCleanlyAndReleasesAsset()
    {
        using var rig = new TestRig();
        using var world = World.Create(rig.JobScheduler, 32);

        var badGuid = Guid.NewGuid();
        rig.ContentProvider.AddMockAsset(badGuid, new MockingContentProvider.MockAssetData
        {
            type = AssetType.Scene,
            data = new byte[] { 1, 2, 3, 4 }
        });

        var op = SceneManager.LoadSceneAsync(world, rig.AssetManager, badGuid, SceneLoadingType.Single).GetValueOrThrow();
        WaitUntilLoadJobsDone(rig, badGuid);
        var waitRes = SceneManager.WaitForCompletion(world, op);

        Assert.IsTrue(waitRes.IsFailure);
        Assert.AreEqual(SceneLoadStatus.Failed, op.Status);
        Assert.IsFalse(string.IsNullOrEmpty(op.ErrorMessage));
        Assert.IsFalse(rig.AssetManager.TryGetEntry(badGuid, out _));
    }

    [TestMethod]
    public void SceneManager_PrefabVolumeOverrides_AppliesAndClearsNullableFields()
    {
        using var rig = new TestRig();
        using var world = World.Create(rig.JobScheduler, 64);

        var meshGuid = Guid.NewGuid();
        rig.ContentProvider.AddMockMesh(meshGuid);

        var prefabGuid = Guid.NewGuid();
        var prefabBytes = BuildTwoNodePrefabBytes(meshGuid, out _);
        rig.ContentProvider.AddMockAsset(prefabGuid, new MockingContentProvider.MockAssetData
        {
            type = AssetType.EntityPrefab,
            data = prefabBytes,
            dependencies = new[] { meshGuid }
        });

        var sceneGuid = Guid.NewGuid();
        var sb = new ScenePayloadBuilder();

        var addedVol = sb.AddComponent(
            typeof(RenderVolumeDataContainer).FullName!,
            ReadOnlySpan<SceneFieldRecord>.Empty,
            new[]
            {
                (typeof(ExposureSettings).FullName!, new[] { ScenePayloadBuilder.DoubleField("fixedExposure", 11.5) }),
                (typeof(HDRISettings).FullName!, new[] { ScenePayloadBuilder.DoubleField("exposure", 2.0) })
            });

        var volOvrBloom = sb.BuildPrefabVolumeOverride(0, typeof(BloomSettings).FullName!, removed: false, ScenePayloadBuilder.DoubleField("threshold", 0.85));
        var volOvrClearExp = sb.BuildPrefabVolumeOverride(0, typeof(ExposureSettings).FullName!, removed: false, new SceneFieldRecord
        {
            fieldNameHash = CacheFileHeader.Fnv1a("fixedExposure"),
            kind = SceneFieldValueKind.None,
            payloadOffset = -1,
            entityTarget = SceneEntityTarget.Invalid
        });
        var volOvrRemoveHdri = sb.BuildPrefabVolumeOverride(0, typeof(HDRISettings).FullName!, removed: true);

        sb.AddPrefabInstance(
            depIndex: 0,
            parentTarget: SceneEntityTarget.Invalid,
            added: new[] { new SceneAddedComponentHeader { templateEntityIndex = 0, component = addedVol } },
            volumeOverrides: new[] { volOvrBloom, volOvrClearExp, volOvrRemoveHdri });

        rig.ContentProvider.AddMockAsset(sceneGuid, new MockingContentProvider.MockAssetData
        {
            type = AssetType.Scene,
            data = sb.BuildBytes(),
            dependencies = new[] { prefabGuid }
        });

        var op = SceneManager.LoadSceneAsync(world, rig.AssetManager, sceneGuid, SceneLoadingType.Single).GetValueOrThrow();
        WaitUntilLoadJobsDone(rig, sceneGuid);
        Assert.IsTrue(rig.AssetManager.TryGetEntry(meshGuid, out var meshEntry));
        meshEntry.State = AssetState.Ready;
        SceneManager.MaterializePendingScenes(world, SceneMaterializeBudget.Unlimited);
        Assert.IsTrue(op.IsMaterialized);
        Assert.AreEqual(SceneLoadStatus.Completed, op.Status);

        var em = world.EntityManager;
        var qId = QueryBuilder.New().WithAll<RenderVolumeDataContainer>().Build(world);
        ref var query = ref world.ComponentManager.GetEntityQueryReference(qId);
        var found = false;
        foreach (var chunk in query.GetChunkIterator())
        {
            var entities = chunk.GetEntities();
            for (var i = 0; i < entities.Length; i++)
            {
                found = true;
                ref var container = ref em.GetComponent<RenderVolumeDataContainer>(entities[i]);
                Assert.IsTrue(container.TryGet<BloomSettings>(out var bloom));
                Assert.AreEqual(0.85f, bloom.threshold);
                Assert.IsTrue(container.TryGet<ExposureSettings>(out var exp));
                Assert.IsNull(exp.fixedExposure);
                Assert.IsFalse(container.Has<HDRISettings>());
            }
        }

        Assert.IsTrue(found);
        SceneManager.UnloadScene(op.Scene, world);
        world.SystemManager.CleanupAll();
    }

    private static void WaitUntilLoadJobsDone(TestRig rig, Guid assetGuid)
    {
        if (rig.AssetManager.TryGetEntry(assetGuid, out var entry) && entry.LoadJobHandle.IsValid)
        {
            rig.JobScheduler.Wait(entry.LoadJobHandle);
        }
    }
}
