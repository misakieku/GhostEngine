using Ghost.Core;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.SceneGraph;
using Ghost.Editor.Core.Utilities;
using Ghost.Engine;
using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Engine.Streaming;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.Editor.Core.Services;

internal sealed class SceneSaveData
{
    public uint FormatVersion
    {
        get; set;
    } = 1;

    public List<EntitySaveData> Entities
    {
        get; set;
    } = new();
}

internal sealed class EntitySaveData
{
    public Dictionary<string, JsonElement> Components
    {
        get; set;
    } = new();
}

[EditorInjection(EditorInjectionAttribute.ServiceLifetime.Singleton, typeof(SceneSerializationService))]
internal class SceneSerializationService : IDisposable
{
    private static readonly Dictionary<Type, FieldInfo[]> s_entityFieldsCache = new();
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new EntityJsonConverter() },
    };

    private sealed class EntityJsonConverter : JsonConverter<Entity>
    {
        public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var localId = reader.GetInt32();
            return new Entity(localId, localId >= 0 ? 1 : 0);
        }

        public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.ID);
        }
    }

    private readonly EditorWorldService _worldService;
    private readonly IAssetRegistry _assetRegistry;

    public SceneSerializationService(EditorWorldService worldService, IAssetRegistry assetRegistry)
    {
        _worldService = worldService;
        _assetRegistry = assetRegistry;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FileLocalIndexOf(Dictionary<Entity, int> reverseMap, Entity entity)
    {
        if (reverseMap.TryGetValue(entity, out var index))
        {
            return index;
        }

        return -1;
    }

    private static bool IsEntityType(Type type)
    {
        return type == typeof(Entity);
    }

    private static FieldInfo[] GetEntityFields(Type type)
    {
        if (!s_entityFieldsCache.TryGetValue(type, out var fields))
        {
            var list = new List<FieldInfo>();
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsEntityType(field.FieldType))
                {
                    list.Add(field);
                }
            }

            fields = list.ToArray();
            s_entityFieldsCache[type] = fields;
        }

        return fields;
    }

    private static object RemapEntityFieldsToLocal(object boxed, Type type, Dictionary<Entity, int> reverseMap)
    {
        var entityFields = GetEntityFields(type);
        foreach (var field in entityFields)
        {
            var entity = (Entity)field.GetValue(boxed)!;
            var localIndex = FileLocalIndexOf(reverseMap, entity);
            field.SetValue(boxed, new Entity(localIndex, localIndex >= 0 ? 0 : -1));
        }

        return boxed;
    }

    private static object RemapLocalFieldsToEntity(object boxed, Type type, Dictionary<int, Entity> forwardMap)
    {
        var entityFields = GetEntityFields(type);
        foreach (var field in entityFields)
        {
            var localAsEntity = (Entity)field.GetValue(boxed)!;
            var localIndex = localAsEntity.ID;
            if (!forwardMap.TryGetValue(localIndex, out var entity))
            {
                entity = Entity.Invalid;
            }

            field.SetValue(boxed, entity);
        }

        return boxed;
    }

    #region Binary Serialization

    private static uint GetTypeNameHash(string typeName)
    {
        var hash = 2166136261u;
        for (var i = 0; i < typeName.Length; i++)
        {
            var c = typeName[i];
            hash ^= c;
            hash *= 16777619u;
        }

        return hash;
    }

    public static unsafe void SerializeToBinary(SceneSaveData data, Stream targetStream)
    {
        using var writer = new BinaryWriter(targetStream, Encoding.UTF8, true);

        var header = new SceneContentHeader
        {
            magic = SceneContentHeader.MAGIC,
            version = SceneContentHeader.VERSION,
            entityCount = data.Entities.Count,
        };

        writer.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<SceneContentHeader>(ref header)));

        if (data.Entities == null)
        {
            return;
        }

        foreach (var entity in data.Entities)
        {
            if (entity.Components == null)
            {
                writer.Write(0);
                continue;
            }

            writer.Write(entity.Components.Count);

            foreach (var (typeName, componentElement) in entity.Components)
            {
                var typeHash = GetTypeNameHash(typeName);
                var componentType = TypeCache.GetTypes(typeName);
                if (componentType == typeof(SceneID))
                {
                    continue;
                }

                if (componentType == null)
                {
                    writer.Write(typeHash);

                    var nameBytes = Encoding.UTF8.GetBytes(typeName);
                    writer.Write(nameBytes.Length);
                    writer.Write(nameBytes);

                    var jsonBytes = Encoding.UTF8.GetBytes(componentElement.GetRawText());
                    writer.Write(jsonBytes.Length);
                    writer.Write(jsonBytes);
                    writer.Write(0);
                    continue;
                }

                var boxed = componentElement.Deserialize(componentType, s_jsonOptions);
                if (boxed == null)
                {
                    continue;
                }

                var compInfo = ComponentRegistry.GetComponentInfo(componentType);

                using var scope = AllocationManager.CreateStackScope();
                using var buffer = new MemoryBlock((nuint)compInfo.size, (nuint)compInfo.alignment, scope.AllocationHandle);

                Marshal.StructureToPtr(boxed, (nint)buffer.GetUnsafePtr(), false);

                var entityFieldOffsets = GetEntityFields(componentType);
                var offsets = new int[entityFieldOffsets.Length];
                for (var i = 0; i < entityFieldOffsets.Length; i++)
                {
                    offsets[i] = (int)Marshal.OffsetOf(componentType, entityFieldOffsets[i].Name);
                }

                writer.Write(typeHash);

                var nameBytes2 = Encoding.UTF8.GetBytes(typeName);
                writer.Write(nameBytes2.Length);
                writer.Write(nameBytes2);

                writer.Write((int)buffer.Size);
                writer.Write(buffer.AsSpan<byte>());
                writer.Write(offsets.Length);

                foreach (var off in offsets)
                {
                    writer.Write(off);
                }
            }
        }
    }

    #endregion

    #region Scene File Deserialization (static, used by handler too)

    public static async ValueTask<SceneSaveData?> DeserializeSceneFileAsync(string jsonPath, CancellationToken token = default)
    {
        var json = await File.ReadAllTextAsync(jsonPath, token);
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;
        var data = new SceneSaveData
        {
            FormatVersion = root.TryGetProperty("formatVersion", out var v) ? v.GetUInt32() : 1,
        };

        if (root.TryGetProperty("entities", out var entitiesElement))
        {
            foreach (var entityElement in entitiesElement.EnumerateArray())
            {
                var entityData = new EntitySaveData();

                if (entityElement.TryGetProperty("components", out var componentsElement))
                {
                    foreach (var componentProperty in componentsElement.EnumerateObject())
                    {
                        entityData.Components[componentProperty.Name] = componentProperty.Value.Clone();
                    }
                }

                data.Entities.Add(entityData);
            }
        }

        return data;
    }

    #endregion

    #region Load Scene into Editor World

    public unsafe Result<Scene> LoadSceneIntoEditorWorld(SceneSaveData data, SceneLoadingType loadingType = SceneLoadingType.Single)
    {
        if (loadingType == SceneLoadingType.Single)
        {
            // TODO: Support TimeData
            _worldService.EditorWorld.Clear(default);
        }

        var world = _worldService.EditorWorld;
        var activeScene = SceneManager.CreateScene();

        var entityCount = data.Entities.Count;
        if (entityCount == 0)
        {
            goto RebuildAndReturn;
        }

        var forwardMap = new Dictionary<int, Entity>(entityCount);

        var scope = AllocationManager.CreateStackScope();
        var typeIds = new UnsafeArray<UnsafeList<Identifier<IComponent>>>(entityCount, scope.AllocationHandle);
        for (var i = 0; i < typeIds.Length; i++)
        {
            typeIds[i] = new UnsafeList<Identifier<IComponent>>(16, scope.AllocationHandle);
        }

        try
        {
            for (var fileIndex = 0; fileIndex < entityCount; fileIndex++)
            {
                var entityData = data.Entities[fileIndex];
                ref var list = ref typeIds[fileIndex];

                list.Add(ComponentRegistry.GetOrRegisterComponentID<SceneID>());

                foreach (var (typeName, _) in entityData.Components)
                {
                    var compId = ComponentRegistry.GetComponentIDByName(typeName);
                    if (compId.IsInvalid)
                    {
                        var type = TypeCache.GetTypes().FirstOrDefault(t => t.FullName == typeName);
                        if (type == null)
                        {
                            continue;
                        }

                        compId = RegisterComponentByType(type);
                    }

                    list.Add(compId);
                }

                var componentSet = new ComponentSetView(list);
                var entity = world.EntityManager.CreateEntity(componentSet);
                forwardMap[fileIndex] = entity;
            }

            using var buffer = new MemoryBlock(1024, 16, scope.AllocationHandle);
            for (var fileIndex = 0; fileIndex < entityCount; fileIndex++)
            {
                if (!forwardMap.TryGetValue(fileIndex, out var entity))
                {
                    continue;
                }

                world.EntityManager.SetComponent(entity, new SceneID { value = activeScene });

                var entityData = data.Entities[fileIndex];
                ref var list = ref typeIds[fileIndex];
                var idx = 1;

                foreach (var (typeName, componentElement) in entityData.Components)
                {
                    var compId = list[idx++];
                    if (compId.IsInvalid)
                    {
                        continue;
                    }

                    var componentType = ComponentRegistry.s_runtimeIDToType[compId];

                    var boxed = componentElement.Deserialize(componentType, s_jsonOptions);
                    if (boxed == null)
                    {
                        continue;
                    }

                    boxed = RemapLocalFieldsToEntity(boxed, componentType, forwardMap);

                    Marshal.StructureToPtr(boxed, (nint)buffer.GetUnsafePtr(), false);

                    world.EntityManager.SetComponent(entity, compId, buffer.GetUnsafePtr());
                }
            }
        }
        finally
        {
            scope.Dispose();

            for (var i = 0; i < typeIds.Length; i++)
            {
                typeIds[i].Dispose();
            }

            typeIds.Dispose();
        }

    RebuildAndReturn:
        _worldService.RebuildSceneGraph();
        return activeScene;
    }

    private static Identifier<IComponent> RegisterComponentByType(Type type)
    {
        var getOrRegisterMethod = typeof(ComponentRegistry).GetMethod(
            "GetOrRegisterComponentID",
            BindingFlags.NonPublic | BindingFlags.Static,
            Array.Empty<Type>());

        if (getOrRegisterMethod == null)
        {
            return Identifier<IComponent>.Invalid;
        }

        if (type == null)
        {
            return Identifier<IComponent>.Invalid;
        }

        var genericMethod = getOrRegisterMethod.MakeGenericMethod(type);
        return (Identifier<IComponent>)genericMethod.Invoke(null, null)!;
    }
    #endregion

    #region Save Scene from Editor World

    public unsafe Result SaveSceneFromEditorWorld(string filePath, Scene scene)
    {
        var world = _worldService.EditorWorld;

        using var scope = AllocationManager.CreateStackScope();
        using var sceneEntities = SceneManager.GetSceneEntities(scene, world, scope.AllocationHandle);

        if (sceneEntities.Count == 0)
        {
            return Result.Failure("No entities found for the specified scene.");
        }

        var entities = new List<Entity>(sceneEntities.Count);
        for (var i = 0; i < sceneEntities.Count; i++)
        {
            entities.Add(sceneEntities[i]);
        }

        var sorted = SortEntitiesByHierarchy(world, entities);

        var reverseMap = new Dictionary<Entity, int>();
        for (var i = 0; i < sorted.Count; i++)
        {
            reverseMap[sorted[i]] = i;
        }

        var data = new SceneSaveData
        {
            FormatVersion = SceneContentHeader.VERSION,
        };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteNumber("formatVersion", SceneContentHeader.VERSION);
        writer.WriteStartArray("entities");

        foreach (var entity in sorted)
        {
            var locationResult = world.EntityManager.GetEntityLocation(entity);
            if (!locationResult.IsSuccess)
            {
                continue;
            }

            var location = locationResult.Value;
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(location.archetypeID);

            writer.WriteStartObject();
            writer.WriteStartObject("components");

            foreach (var layout in archetype._layouts)
            {
                var type = ComponentRegistry.s_runtimeIDToType[layout.componentID];
                if (type == typeof(SceneID))
                {
                    continue;
                }

                var fullName = type.FullName ?? type.Name;
                var compInfo = ComponentRegistry.GetComponentInfo(layout.componentID);

                var pData = archetype.GetComponentData(location.chunkIndex, location.rowIndex, layout.componentID);
                if (pData == null)
                {
                    continue;
                }

                var boxed = Marshal.PtrToStructure((nint)pData, type);
                if (boxed == null)
                {
                    continue;
                }

                boxed = RemapEntityFieldsToLocal(boxed, type, reverseMap);

                writer.WritePropertyName(fullName);
                JsonSerializer.Serialize(writer, boxed, type, s_jsonOptions);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        File.WriteAllBytes(filePath, stream.ToArray());

        return Result.Success();
    }

    private static List<Entity> SortEntitiesByHierarchy(World world, List<Entity> entities)
    {
        var entitySet = new HashSet<Entity>(entities);
        var roots = new List<Entity>();
        var childrenMap = new Dictionary<Entity, List<Entity>>();

        foreach (var entity in entities)
        {
            if (!world.EntityManager.HasComponent<Hierarchy>(entity))
            {
                roots.Add(entity);
                continue;
            }

            ref var hierarchy = ref world.EntityManager.GetComponent<Hierarchy>(entity);
            if (hierarchy.parent.IsValid && entitySet.Contains(hierarchy.parent))
            {
                if (!childrenMap.TryGetValue(hierarchy.parent, out var list))
                {
                    list = new List<Entity>();
                    childrenMap[hierarchy.parent] = list;
                }

                list.Add(entity);
            }
            else
            {
                roots.Add(entity);
            }
        }

        var sorted = new List<Entity>(entities.Count);
        foreach (var root in roots)
        {
            AddEntityAndDescendants(sorted, root, childrenMap);
        }

        return sorted;
    }

    private static void AddEntityAndDescendants(List<Entity> sorted, Entity entity, Dictionary<Entity, List<Entity>> childrenMap)
    {
        sorted.Add(entity);
        if (childrenMap.TryGetValue(entity, out var children))
        {
            foreach (var child in children)
            {
                AddEntityAndDescendants(sorted, child, childrenMap);
            }
        }
    }

    #endregion

    public void Dispose()
    {
    }
}
