using Ghost.Editor.Core.Inspector;
using Ghost.Editor.Core.Services;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Text.Json;

namespace Ghost.Editor.Core.SceneGraph;

/// <summary>
/// Represents a single component on an entity within the Editor's scene graph.
/// Acts as the middleware between the Inspector's PropertyModels and the actual ECS memory.
/// </summary>
public unsafe class ComponentNode
{
    private readonly IUndoService _undoService;
    private readonly IEditorWorldService _worldService;

    private readonly Dictionary<string, int> _propertyIndices;
    protected readonly World _world;

    public EntityNode EntityNode { get; }

    public Type ComponentType { get; }
    public ComponentDescriptor Descriptor { get; }
    public PropertyNode[] Properties { get; }
    public string Name => Descriptor.DisplayName;

    internal ComponentNode(World world, EntityNode entityNode, Type componentType, ComponentDescriptor descriptor)
    {
        _undoService = EditorApplication.GetService<IUndoService>();
        _worldService = EditorApplication.GetService<IEditorWorldService>();

        _propertyIndices = new Dictionary<string, int>(descriptor.Properties.Length);
        _world = world;

        EntityNode = entityNode;

        ComponentType = componentType;
        Descriptor = descriptor;

        Properties = new PropertyNode[descriptor.Properties.Length];
        for (var i = 0; i < descriptor.Properties.Length; i++)
        {
            _propertyIndices[descriptor.Properties[i].Name] = i;

            // TODO: We should use a registry/factory for different PropertyNode types instead of hardcoding HandlePropertyNode here. This is just a quick solution for handles for now.
            var prop = descriptor.Properties[i];
            if (prop.ValueType.IsGenericType && prop.ValueType.GetGenericTypeDefinition() == typeof(Ghost.Core.Handle<>))
            {
                var nodeType = typeof(HandlePropertyNode<>).MakeGenericType(prop.ValueType.GetGenericArguments()[0]);
                Properties[i] = (PropertyNode)Activator.CreateInstance(nodeType, prop, this)!;
            }
            else
            {
                // Create a standard PropertyNode<T> for non-handle types
                // We use MakeGenericType to create the correct PropertyNode<T> based on FieldType
                var nodeType = typeof(PropertyNode<>).MakeGenericType(prop.ValueType);
                Properties[i] = (PropertyNode)Activator.CreateInstance(nodeType, prop, this, null)!;
            }
        }
    }

    public void SetPropertyValue<T>(PropertyDescriptor property, T value)
        where T : unmanaged
    {
        if (property.ValueType != typeof(T))
        {
            throw new ArgumentException("Property type does not match value type");
        }

        _undoService.RecordObject(EntityNode, $"Edit property {property.DisplayName} on {Descriptor.DisplayName}");
        _worldService.Defer(() =>
        {
            if (Descriptor.IsShared)
            {
                var ptr = _world.EntityManager.GetSharedComponent(EntityNode.Entity, Descriptor.ComponentId);
                if (ptr != null)
                {
                    using var scope = AllocationManager.CreateStackScope();
                    using var buffer = new MemoryBlock((nuint)Descriptor.Size, 16, scope.AllocationHandle);
                    System.Runtime.CompilerServices.Unsafe.CopyBlock(buffer.GetUnsafePtr(), ptr, (uint)Descriptor.Size);
                    property.Write(buffer.GetUnsafePtr(), value);
                    _world.EntityManager.SetSharedComponent(EntityNode.Entity, Descriptor.ComponentId, buffer.GetUnsafePtr());
                }
            }
            else
            {
                var pComponent = GetComponentPointer();
                property.Write(pComponent, value);
            }
        });
    }

    public void SetComponent<T>(T value)
        where T : unmanaged
    {
        if (typeof(T) != ComponentType)
        {
            throw new ArgumentException("Value type does not match component type");
        }

        _undoService.RecordObject(EntityNode, $"Edit component {Descriptor.DisplayName}");
        _worldService.Defer(() =>
        {
            if (Descriptor.IsShared)
            {
                using var scope = AllocationManager.CreateStackScope();
                using var buffer = new MemoryBlock((nuint)Descriptor.Size, 16, scope.AllocationHandle);
                buffer.GetElementAt<T>(0) = value;
                _world.EntityManager.SetSharedComponent(EntityNode.Entity, Descriptor.ComponentId, buffer.GetUnsafePtr());
            }
            else
            {
                var pComponent = GetComponentPointer();
                *(T*)pComponent = value;
            }
        });
    }

    public PropertyNode GetProperty(string propertyName)
    {
        if (_propertyIndices.TryGetValue(propertyName, out var index))
        {
            return Properties[index];
        }

        throw new ArgumentException($"Property '{propertyName}' not found in component '{Name}'");
    }

    public PropertyNode<T> GetProperty<T>(string propertyName)
        where T : unmanaged
    {
        var prop = GetProperty(propertyName);
        if (prop is PropertyNode<T> typedProp)
        {
            return typedProp;
        }

        throw new ArgumentException($"Property '{propertyName}' is not of type {typeof(T).Name}");
    }

    public void* GetComponentPointer()
    {
        if (Descriptor.IsShared)
        {
            return _world.EntityManager.GetSharedComponent(EntityNode.Entity, Descriptor.ComponentId);
        }
        else
        {
            return _world.EntityManager.GetComponent(EntityNode.Entity, Descriptor.ComponentId);
        }
    }

    public T GetComponent<T>()
        where T : unmanaged
    {
        if (typeof(T) != ComponentType)
        {
            throw new ArgumentException("Field type does not match component type");
        }

        var pComponent = GetComponentPointer();
        return *(T*)pComponent;
    }

    public T GetPropertyValue<T>(PropertyDescriptor field)
        where T : unmanaged
    {
        var pComponent = GetComponentPointer();
        return field.Read<T>(pComponent);
    }

    /// <summary>Serialize this component to JSON. Base reads from ECS directly.</summary>
    public virtual void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options, Action<object>? preSerialize = null)
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

    /// <summary>Deserialize from JSON and apply to ECS. Base writes to ECS directly.</summary>
    public virtual void Deserialize(JsonElement element, JsonSerializerOptions options, Action<object>? postDeserialize = null)
    {
        var boxed = element.Deserialize(ComponentType, options);
        if (boxed != null)
        {
            postDeserialize?.Invoke(boxed);

            foreach (var prop in Properties)
            {
                prop.DeserializeOverride(element, boxed);
            }

            _worldService.Defer(() =>
            {
                if (Descriptor.IsShared)
                {
                    using var scope = AllocationManager.CreateStackScope();
                    using var buffer = new MemoryBlock((nuint)Descriptor.Size, 16, scope.AllocationHandle);
                    System.Runtime.InteropServices.Marshal.StructureToPtr(boxed, (nint)buffer.GetUnsafePtr(), false);
                    _world.EntityManager.SetSharedComponent(EntityNode.Entity, Descriptor.ComponentId, buffer.GetUnsafePtr());
                }
                else
                {
                    System.Runtime.InteropServices.Marshal.StructureToPtr(boxed, (nint)GetComponentPointer(), false);
                }
            });
        }
    }
}
