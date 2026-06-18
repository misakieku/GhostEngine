using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Utilities;
using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Engine.Streaming;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics;
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
    public string Name
    {
        get; set;
    } = "Entity";

    // TODO: Maybe we can store the component data directly instead of the json element.
    public Dictionary<string, JsonElement> Components
    {
        get; set;
    } = new();
}

// TODO: Serialize shared components.
// TODO: This is a bit chaos now.
internal class SceneSerializationService : IDisposable
{
    private static readonly Dictionary<Type, FieldInfo[]> s_entityFieldsCache = new();
    private static readonly Dictionary<Type, FieldInfo[]> s_handleFieldsCache = new();
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

    private readonly IEditorWorldService _worldService;
    private readonly IAssetRegistry _assetRegistry;
    private readonly SceneGraphSyncService _syncService;

    public SceneSerializationService(IEditorWorldService worldService, IAssetRegistry assetRegistry, SceneGraphSyncService syncService)
    {
        _worldService = worldService;
        _assetRegistry = assetRegistry;
        _syncService = syncService;
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

    private static FieldInfo[] GetEntityFields(Type type)
    {
        if (!s_entityFieldsCache.TryGetValue(type, out var fields))
        {
            var list = new List<FieldInfo>();
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType == typeof(Entity))
                {
                    list.Add(field);
                }
            }

            fields = list.ToArray();
            s_entityFieldsCache[type] = fields;
        }

        return fields;
    }

    private static FieldInfo[] GetHandleFields(Type type)
    {
        if (!s_handleFieldsCache.TryGetValue(type, out var fields))
        {
            var list = new List<FieldInfo>();
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Ghost.Core.Handle<>))
                {
                    list.Add(field);
                }
            }

            fields = list.ToArray();
            s_handleFieldsCache[type] = fields;
        }

        return fields;
    }

    private static void RemapEntityFieldsToLocal(object boxed, Type type, Dictionary<Entity, int> reverseMap)
    {
        var entityFields = GetEntityFields(type);
        foreach (var field in entityFields)
        {
            var entity = (Entity)field.GetValue(boxed)!;
            var localIndex = FileLocalIndexOf(reverseMap, entity);
            field.SetValue(boxed, new Entity(localIndex, localIndex >= 0 ? 0 : -1));
        }
    }

    private static void RemapLocalFieldsToEntity(object boxed, Type type, Dictionary<int, Entity> forwardMap)
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

    public static unsafe Guid[] SerializeToBinary(SceneSaveData data, Stream targetStream)
    {
        using var writer = new BinaryWriter(targetStream, Encoding.UTF8, true);
        var dependencies = new List<Guid>();

        var header = new SceneContentHeader
        {
            magic = SceneContentHeader.MAGIC,
            version = SceneContentHeader.VERSION,
            entityCount = data.Entities.Count,
        };

        writer.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<SceneContentHeader>(ref header)));

        if (data.Entities == null)
        {
            return dependencies.ToArray();
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

                var handleFieldOffsets = GetHandleFields(componentType);
                var handleOffsets = new int[handleFieldOffsets.Length];
                for (var i = 0; i < handleFieldOffsets.Length; i++)
                {
                    var field = handleFieldOffsets[i];
                    handleOffsets[i] = (int)Marshal.OffsetOf(componentType, field.Name);

                    var camelCaseName = char.ToLowerInvariant(field.Name[0]) + field.Name.Substring(1);
                    var assetGuid = Guid.Empty;
                    if (componentElement.TryGetProperty(camelCaseName, out var propElement) || componentElement.TryGetProperty(field.Name, out propElement))
                    {
                        if (propElement.ValueKind == JsonValueKind.String && Guid.TryParse(propElement.GetString(), out var parsedGuid))
                        {
                            assetGuid = parsedGuid;
                        }
                    }

                    int importIndex = -1;
                    if (assetGuid != Guid.Empty)
                    {
                        importIndex = dependencies.IndexOf(assetGuid);
                        if (importIndex == -1)
                        {
                            importIndex = dependencies.Count;
                            dependencies.Add(assetGuid);
                        }
                    }

                    var pField = (byte*)buffer.GetUnsafePtr() + handleOffsets[i];
                    *(int*)pField = importIndex;
                    *((int*)pField + 1) = 0;
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

                writer.Write(handleOffsets.Length);
                foreach (var off in handleOffsets)
                {
                    writer.Write(off);
                }
            }
        }

        return dependencies.ToArray();
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

                if (entityElement.TryGetProperty("name", out var nameElement))
                {
                    entityData.Name = nameElement.GetString() ?? "Entity";
                }

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

    #region Save Scene from Editor World

    public unsafe void SaveSceneFromEditorWorld(string filePath, Scene scene)
    {
        var bytes = SerializeSceneToMemory(scene);
        File.WriteAllBytes(filePath, bytes);
    }

    public unsafe byte[] SerializeSceneToMemory(Scene scene)
    {
        var world = _worldService.EditorWorld;

        using var scope = AllocationManager.CreateStackScope();
        using var entities = SceneManager.GetSceneEntities(world, scene, scope.AllocationHandle);

        using var sorted = SortEntitiesByHierarchy(world, entities, scope.AllocationHandle);

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
            Debug.WriteLine(entity);
            var locationResult = world.EntityManager.GetEntityLocation(entity);
            if (!locationResult.IsSuccess)
            {
                continue;
            }

            var location = locationResult.Value;
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(location.archetypeID);

            writer.WriteStartObject();

            var entityName = "Entity";
            SceneGraph.EntityNode? node = null;
            if (_syncService != null && _syncService.TryGetNode(entity, out node))
            {
                entityName = node.Name;
            }
            writer.WriteString("name", entityName);

            writer.WriteStartObject("components");

            if (node != null)
            {
                node.BuildComponents(); // Ensure latest

                foreach (var compNode in node.Components)
                {
                    var type = compNode.ComponentType;
                    if (type == typeof(SceneID))
                    {
                        continue;
                    }

                    var fullName = type.FullName ?? type.Name;
                    writer.WritePropertyName(fullName);
                    compNode.Serialize(writer, s_jsonOptions, (boxed) =>
                    {
                        RemapEntityFieldsToLocal(boxed, type, reverseMap);
                    });
                }
            }
            else
            {
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

                    RemapEntityFieldsToLocal(boxed, type, reverseMap);

                    writer.WritePropertyName(fullName);
                    JsonSerializer.Serialize(writer, boxed, type, s_jsonOptions);
                }
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        return stream.ToArray();
    }

    private static UnsafeList<Entity> SortEntitiesByHierarchy(World world, ReadOnlySpan<Entity> entities, AllocationHandle allocationHandle)
    {
        using var scope = AllocationManager.CreateStackScope();

        using var entitySet = new UnsafeHashSet<Entity>(entities.Length, scope.AllocationHandle);
        using var roots = new UnsafeList<Entity>(32, scope.AllocationHandle);
        var childrenMap = new UnsafeHashMap<Entity, UnsafeList<Entity>>(32, scope.AllocationHandle);

        try
        {
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
                    ref var list = ref childrenMap.GetValueRefOrAddDefault(hierarchy.parent, out var exist);
                    if (!exist)
                    {
                        list = new UnsafeList<Entity>(4, allocationHandle);
                    }

                    list.Add(entity);
                }
                else
                {
                    roots.Add(entity);
                }
            }

            var sorted = new UnsafeList<Entity>(entities.Length, allocationHandle);
            foreach (var root in roots)
            {
                AddEntityAndDescendants(ref sorted, root, in childrenMap);
            }

            return sorted;
        }
        finally
        {
            foreach (var kvp in childrenMap)
            {
                kvp.Value.Dispose();
            }

            childrenMap.Dispose();
        }
    }

    private static void AddEntityAndDescendants(ref UnsafeList<Entity> sorted, Entity entity, ref readonly UnsafeHashMap<Entity, UnsafeList<Entity>> childrenMap)
    {
        sorted.Add(entity);
        if (childrenMap.TryGetValue(entity, out var children))
        {
            foreach (var child in children)
            {
                AddEntityAndDescendants(ref sorted, child, in childrenMap);
            }
        }
    }

    #endregion

    public void Dispose()
    {
    }
}
