using Ghost.Editor.SceneGraph;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Ghost.Entities.Components;
using Ghost.Entities.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.Editor.Serializer;

internal class WorldNodeSerializer : JsonConverter<WorldNode>
{
    private static class Property
    {
        public const string NAME = "Name";
        public const string ENTITIES = "Entities";
        public const string ID = "ID";
        public const string ENTITY_ID = "EntityID";
        public const string COMPONENTS = "Components";
        public const string DATA = "Data";
        public const string SYSTEMS = "Systems";
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(WorldNode) || typeToConvert.IsSubclassOf(typeof(WorldNode));
    }

    public override WorldNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonDocument.ParseValue(ref reader).RootElement;
        var name = element.GetProperty(Property.NAME).GetString() ?? "New World";

        var world = World.Create();
        var result = new WorldNode(world, name);

        foreach (var entityElement in element.GetProperty(Property.ENTITIES).EnumerateArray())
        {
            var entityName = entityElement.GetProperty(Property.NAME).GetString() ?? "New Entity";
            var entityID = entityElement.GetProperty(Property.ID).GetInt32();
            var entity = new Entity(entityID, 0);
            var node = new EntityNode(entity, entityName);

            world.EntityManager.AddEntityInternal(entity);
            result.EntityNodeLookup[entity] = node;
        }

        foreach (var componentElement in element.GetProperty(Property.COMPONENTS).EnumerateObject())
        {
            var typeName = componentElement.Name;
            var type = Type.GetType(typeName) ?? throw new Exception($"Type {typeName} not found.");

            foreach (var dataElement in componentElement.Value.EnumerateArray())
            {
                var entityID = dataElement.GetProperty(Property.ENTITY_ID).GetInt32();
                var entity = new Entity(entityID, 0);

                var dataProperty = dataElement.GetProperty(Property.DATA);
                var component = JsonSerializer.Deserialize(dataProperty.GetRawText(), type, options);
                if (component is IComponentData data)
                {
                    world.EntityManager.AddComponent(entity, data, type);
                }
            }
        }

        foreach (var systemElement in element.GetProperty(Property.SYSTEMS).EnumerateArray())
        {
            var typeString = systemElement.GetString();
            if (string.IsNullOrEmpty(typeString))
            {
                continue;
            }

            var systemType = Type.GetType(typeString);
            if (systemType == null)
            {
                continue;
            }

            world.SystemStorage.AddSystem(systemType);
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, WorldNode value, JsonSerializerOptions options)
    {
        writer.WriteObject(() =>
        {
            writer.WriteString(Property.NAME, value.Name);
            writer.WriteArray(Property.ENTITIES, value.World.EntityManager.Entities, entity =>
            {
                if (!entity.IsValid)
                {
                    return;
                }

                writer.WriteObject(() =>
                {
                    writer.WriteString(Property.NAME, value.EntityNodeLookup[entity].Name);
                    writer.WriteNumber(Property.ID, entity.ID);
                });
            });

            writer.WriteObject(Property.COMPONENTS, () =>
            {
                foreach (var kvp in value.World.ComponentStorage.ComponentPools)
                {
                    var type = TypeHandle.ToType(kvp.Key) ?? throw new Exception($"Type {kvp.Key} not found.");
                    var typeName = type.AssemblyQualifiedName ?? type.Name;

                    writer.WriteArray(typeName, kvp.Value.Enumerate(), data =>
                    {
                        writer.WriteObject(() =>
                        {
                            writer.WriteNumber(Property.ENTITY_ID, data.entity.ID);
                            writer.WritePropertyName(Property.DATA);
                            JsonSerializer.Serialize(writer, data.component, type, options);
                        });
                    });
                }
            });

            writer.WriteArray(Property.SYSTEMS, value.World.SystemStorage.Systems, systemType =>
            {
                writer.WriteStringValue(systemType.AssemblyQualifiedName ?? systemType.Name);
            });
        });
    }
}