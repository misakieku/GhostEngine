using Ghost.TestCore;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Ghost.Entities.Test;

public class SerializationTest : ITest
{
    private World _world = null!;

    public void Setup()
    {
        _world = World.Create();
    }

    public unsafe void Run()
    {
#if GHOST_EDITOR
        using var scope = AllocationManager.CreateStackScope();
        var set1 = new ComponentSet(scope.AllocationHandle, ComponentTypeID<Transform>.Value);
        var set2 = new ComponentSet(scope.AllocationHandle, ComponentTypeID<Transform>.Value, ComponentTypeID<Mesh>.Value);

        var e1 = _world.EntityManager.CreateEntity(set1);
        var e2 = _world.EntityManager.CreateEntity(set2);

        _world.EntityManager.SetComponent(e1, new Transform { position = new float3(1, 2, 3) });
        _world.EntityManager.SetComponent(e2, new Transform { position = new float3(4, 5, 6) });
        _world.EntityManager.SetComponent(e2, new Mesh { index = 42 });

        using var stream = new MemoryStream();
        var serializeOptions = new JsonSerializerOptions
        {
            IncludeFields = true
        };

        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("Name", "world 1");
        writer.WriteStartArray("Entities");

        for (var i = 0; i < _world.ComponentManager.ArchetypeCount; i++)
        {
            ref var archetype = ref _world.ComponentManager.GetArchetypeReference(i);

            for (var j = 0; j < archetype.ChunkCount; j++)
            {
                ref var chunk = ref archetype.GetChunkReference(j);
                for (var k = 0; k < chunk._count; k++)
                {
                    writer.WriteStartObject();

                    var entity = *(Entity*)(chunk.GetUnsafePtr() + archetype.EntityIDsOffset + k * sizeof(Entity));
                    writer.WriteNumber("ID", entity.ID);
                    writer.WriteStartArray("Components");

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
                        var instace = Marshal.PtrToStructure((nint)pComponentData, type);
                        JsonSerializer.Serialize(writer, instace, type, serializeOptions);

                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }
            }
        }

        writer.WriteEndArray();
        writer.WriteEndObject();

        writer.Flush();

        var data = stream.ToArray();

        var json = System.Text.Encoding.UTF8.GetString(data);
        Console.WriteLine(json);


        var reader = new Utf8JsonReader(data);

        var root = JsonDocument.ParseValue(ref reader).RootElement;
        var name = root.GetProperty("Name").GetString();
        Console.WriteLine($"Deserialized World Name: {name}");

        var entityData = new List<(int EntityID, Type ComponentType, object Instance)>();

        foreach (var entityElement in root.GetProperty("Entities").EnumerateArray())
        {
            var id = entityElement.GetProperty("ID").GetInt32();

            // Access the new "Components" array
            var componentsElement = entityElement.GetProperty("Components");

            foreach (var componentElement in componentsElement.EnumerateArray())
            {
                var typeName = componentElement.GetProperty("Type").GetString();
                var dataElement = componentElement.GetProperty("Data");

                var type = Type.GetType(typeName!);
                if (type == null)
                {
                    continue;
                }

                var instance = dataElement.Deserialize(type, serializeOptions);
                if (instance != null)
                {
                    entityData.Add((id, type, instance));
                }
            }
        }

        foreach (var (id, type, instance) in entityData)
        {
            Console.WriteLine($"Entity ID: {id}, Component: {type.Name}, Data: {instance}");
        }
#endif
    }

    public void Cleanup()
    {
        _world.Dispose();
    }
}
