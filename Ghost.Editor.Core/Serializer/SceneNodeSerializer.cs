using Ghost.Editor.Core.SceneGraph;
using Ghost.Engine.IO;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using System.Text.Json;

namespace Ghost.Editor.Core.Serializer;

internal class SceneNodeSerializer : CustomSerializer<SceneNode>
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
        return typeToConvert == typeof(SceneNode) || typeToConvert.IsSubclassOf(typeof(SceneNode));
    }

    public unsafe override void SerializeJson(Utf8JsonWriter writer, SceneNode value, JsonSerializerOptions options)
    {
        writer.WriteObject(() =>
        {
            writer.WriteString(Property.NAME, value.Name);
            writer.WriteStartArray(Property.ENTITIES);

            for (var i = 0; i < value.Scene.World.ComponentManager.ArchetypeCount; i++)
            {
                ref var archetype = ref value.Scene.World.ComponentManager.GetArchetypeReference(i);

                for (var j = 0; j < archetype.ChunkCount; j++)
                {
                    ref var chunk = ref archetype.GetChunkReference(j);
                    for (var k = 0; k < chunk._count; k++)
                    {
                        foreach (var layout in archetype._layouts)
                        {
                            var type = ComponentRegistry.s_runtimeIDToType[layout.componentID];
                            var size = ComponentRegistry.GetComponentInfo(layout.componentID).size;

                            if (type.AssemblyQualifiedName == null)
                            {
                                continue;
                            }

                            writer.WriteStartObject();
                            writer.WriteString("Type", type.AssemblyQualifiedName);
                            writer.WritePropertyName("Data");

                            var pComponentData = chunk.GetUnsafePtr() + layout.offset + (k * size);
                            ComponentSerializerRegistry.SerializeJson(layout.componentID, writer, pComponentData, options);
                        }
                    }
                }
            }

            writer.WriteEndArray();

            writer.WriteArray(Property.SYSTEMS, value.Scene.World.SystemManager.Systems, system =>
            {
                var name = system.GetType().AssemblyQualifiedName;
                if (name == null)
                {
                    return;
                }

                writer.WriteStringValue(name);
            });
        });
    }

    public override SceneNode? DeserializeJson(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        throw new NotImplementedException();

        //var element = JsonDocument.ParseValue(ref reader).RootElement;
        //var name = element.GetProperty(Property.NAME).GetString() ?? "New World";

        //var world = World.Create(EngineCore.JobScheduler);
        //var result = new WorldNode(world, name);

        //foreach (var entityElement in element.GetProperty(Property.ENTITIES).EnumerateArray())
        //{
        //    var entityName = entityElement.GetProperty(Property.NAME).GetString() ?? "New Entity";
        //    var entityID = entityElement.GetProperty(Property.ID).GetInt32();
        //    var entity = new Entity(entityID, 0);
        //    var node = new EntityNode(result, entity, entityName);

        //    world.EntityManager.AddEntityInternal(entity);
        //    result.EntityNodeLookup[entity] = node;
        //}

        //foreach (var componentElement in element.GetProperty(Property.COMPONENTS).EnumerateObject())
        //{
        //    var typeName = componentElement.Name;
        //    var space = Type.GetType(typeName) ?? throw new Exception($"Type {typeName} not found.");

        //    foreach (var dataElement in componentElement.Value.EnumerateArray())
        //    {
        //        var entityID = dataElement.GetProperty(Property.ENTITY_ID).GetInt32();
        //        var entity = new Entity(entityID, 0);

        //        var dataProperty = dataElement.GetProperty(Property.DATA);
        //        var component = JsonSerializer.Deserialize(dataProperty.GetRawText(), space, options);
        //        if (component is IComponent data)
        //        {
        //            world.EntityManager.AddComponent(entity, data, space);
        //        }
        //    }
        //}

        //foreach (var systemElement in element.GetProperty(Property.SYSTEMS).EnumerateArray())
        //{
        //    var typeString = systemElement.GetString();
        //    if (string.IsNullOrEmpty(typeString))
        //    {
        //        continue;
        //    }

        //    var systemType = Type.GetType(typeString);
        //    if (systemType == null)
        //    {
        //        continue;
        //    }

        //    world.SystemStorage.AddSystem(systemType);
        //}

        //return result;
    }

    public override void SerializeBinary(BinaryWriter writer, SceneNode value)
    {
        throw new NotImplementedException();
    }

    public override SceneNode? DeserializeBinary(BinaryReader reader)
    {
        throw new NotImplementedException();
    }
}
