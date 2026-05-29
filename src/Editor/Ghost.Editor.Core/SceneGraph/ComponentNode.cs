using Ghost.Editor.Core.Inspector;
using Ghost.Entities;
using System.Text.Json;

namespace Ghost.Editor.Core.SceneGraph;

/// <summary>
/// Represents a single component on an entity within the Editor's scene graph.
/// Acts as the middleware between the Inspector's PropertyModels and the actual ECS memory.
/// </summary>
public class ComponentNode
{
    protected readonly World _world;
    protected readonly Entity _entity;

    public Type ComponentType { get; }
    public ComponentDescriptor Descriptor { get; }
    public PropertyNode[] Properties { get; }
    public string Name => Descriptor.DisplayName;

    public ComponentNode(World world, Entity entity, Type componentType, ComponentDescriptor descriptor)
    {
        _world = world;
        _entity = entity;
        ComponentType = componentType;
        Descriptor = descriptor;

        Properties = new PropertyNode[descriptor.Properties.Length];
        for (var i = 0; i < descriptor.Properties.Length; i++)
        {
            var prop = descriptor.Properties[i];
            if (prop.FieldType.IsGenericType && prop.FieldType.GetGenericTypeDefinition() == typeof(Ghost.Core.Handle<>))
            {
                var nodeType = typeof(HandlePropertyNode<>).MakeGenericType(prop.FieldType.GetGenericArguments()[0]);
                Properties[i] = (PropertyNode)Activator.CreateInstance(nodeType, prop, this)!;
            }
            else
            {
                // Create a standard PropertyNode<T> for non-handle types
                // We use MakeGenericType to create the correct PropertyNode<T> based on FieldType
                var nodeType = typeof(PropertyNode<>).MakeGenericType(prop.FieldType);
                Properties[i] = (PropertyNode)Activator.CreateInstance(nodeType, prop, this, null)!;
            }
        }
    }

    // --- Data Access ---

    public object ReadBoxedValue(PropertyDescriptor field)
    {
        unsafe
        {
            var pComponent = GetComponentPointer();
            return field.ReadBoxed(pComponent);
        }
    }

    public T GetFieldValue<T>(PropertyDescriptor field) where T : unmanaged
    {
        unsafe
        {
            var pComponent = GetComponentPointer();
            return field.Read<T>(pComponent);
        }
    }

    public void SetFieldValue<T>(PropertyDescriptor field, T value) where T : unmanaged
    {
        EditorApplication.GetService<Services.EditorWorldService>().Defer(() =>
        {
            unsafe
            {
                if (Descriptor.IsShared)
                {
                    var ptr = _world.EntityManager.GetSharedComponent(_entity, Descriptor.ComponentId);
                    if (ptr != null)
                    {
                        using var scope = Misaki.HighPerformance.LowLevel.Buffer.AllocationManager.CreateStackScope();
                        using var buffer = new Misaki.HighPerformance.LowLevel.Buffer.MemoryBlock((nuint)Descriptor.Size, 16, scope.AllocationHandle);
                        System.Runtime.CompilerServices.Unsafe.CopyBlock(buffer.GetUnsafePtr(), ptr, (uint)Descriptor.Size);
                        field.Write<T>(buffer.GetUnsafePtr(), value);
                        _world.EntityManager.SetSharedComponent(_entity, Descriptor.ComponentId, buffer.GetUnsafePtr());
                    }
                }
                else
                {
                    var pComponent = GetComponentPointer();
                    field.Write<T>(pComponent, value);
                }
            }
        });
    }

    // --- Serialization ---

    /// <summary>Serialize this component to JSON. Base reads from ECS directly.</summary>
    public virtual void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options, Action<object>? preSerialize = null)
    {
        unsafe
        {
            var boxed = System.Runtime.InteropServices.Marshal.PtrToStructure((nint)GetComponentPointer(), ComponentType);
            if (boxed != null)
            {
                preSerialize?.Invoke(boxed);

                var jsonString = JsonSerializer.Serialize(boxed, ComponentType, options);
                using var doc = JsonDocument.Parse(jsonString);
                var root = System.Text.Json.Nodes.JsonObject.Create(doc.RootElement);
                if (root != null)
                {
                    foreach (var prop in Properties)
                    {
                        prop.SerializeOverride(root, boxed);
                    }
                    root.WriteTo(writer, options);
                    return;
                }

                JsonSerializer.Serialize(writer, boxed, ComponentType, options);
            }
        }
    }

    /// <summary>Deserialize from JSON and apply to ECS. Base writes to ECS directly.</summary>
    public virtual void Deserialize(JsonElement element, JsonSerializerOptions options, Action<object>? postDeserialize = null)
    {
        unsafe
        {
            var boxed = element.Deserialize(ComponentType, options);
            if (boxed != null)
            {
                postDeserialize?.Invoke(boxed);

                foreach (var prop in Properties)
                {
                    prop.DeserializeOverride(element, boxed);
                }

                EditorApplication.GetService<Services.EditorWorldService>().Defer(() =>
                {
                    if (Descriptor.IsShared)
                    {
                        using var scope = Misaki.HighPerformance.LowLevel.Buffer.AllocationManager.CreateStackScope();
                        using var buffer = new Misaki.HighPerformance.LowLevel.Buffer.MemoryBlock((nuint)Descriptor.Size, 16, scope.AllocationHandle);
                        System.Runtime.InteropServices.Marshal.StructureToPtr(boxed, (nint)buffer.GetUnsafePtr(), false);
                        _world.EntityManager.SetSharedComponent(_entity, Descriptor.ComponentId, buffer.GetUnsafePtr());
                    }
                    else
                    {
                        System.Runtime.InteropServices.Marshal.StructureToPtr(boxed, (nint)GetComponentPointer(), false);
                    }
                });
            }
        }
    }

    public unsafe void* GetComponentPointer()
    {
        if (Descriptor.IsShared)
        {
            return _world.EntityManager.GetSharedComponent(_entity, Descriptor.ComponentId);
        }
        else
        {
            return _world.EntityManager.GetComponent(_entity, Descriptor.ComponentId);
        }
    }
}
