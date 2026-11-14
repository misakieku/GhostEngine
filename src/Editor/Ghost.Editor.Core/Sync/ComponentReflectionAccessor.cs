using Ghost.Entities;
using System.Collections.Concurrent;
using System.Reflection;

namespace Ghost.Editor.Core.Sync;

/// <summary>
/// Caches typed unmanaged accessors to add, read, write, and remove <see cref="IComponentData"/> instances on an ECS <see cref="EntityManager"/>.
/// </summary>
public static unsafe class ComponentReflectionAccessor
{
    private interface IAccessor
    {
        object Read(void* ptr);
        void Write(void* ptr, object value);
        void AddOrSet(EntityManager em, Entity entity, object value);
    }

    private sealed class TypedAccessor<T> : IAccessor
        where T : unmanaged, IComponentData
    {
        public object Read(void* ptr)
        {
            return *(T*)ptr;
        }

        public void Write(void* ptr, object value)
        {
            *(T*)ptr = (T)value;
        }

        public void AddOrSet(EntityManager em, Entity entity, object value)
        {
            var typed = (T)value;
            var compId = ComponentTypeID<T>.Value;
            if (em.HasComponent(entity, compId))
            {
                em.GetComponent<T>(entity) = typed;
            }
            else
            {
                em.AddComponent(entity, in typed);
            }
        }
    }

    private static readonly ConcurrentDictionary<Type, IAccessor?> s_accessors = new();

    public static bool TryAddOrSet(EntityManager em, Entity entity, Type componentType, object? value)
    {
        if (!entity.IsValid || !em.Exists(entity) || value == null)
        {
            return false;
        }

        var accessor = GetOrCreate(componentType);
        if (accessor == null)
        {
            return false;
        }

        accessor.AddOrSet(em, entity, value);
        return true;
    }

    public static bool TryRemove(EntityManager em, Entity entity, Type componentType)
    {
        if (!entity.IsValid || !em.Exists(entity))
        {
            return false;
        }

        var compId = ComponentRegistry.GetComponentID(componentType);
        if (compId.IsInvalid || !em.HasComponent(entity, compId))
        {
            return false;
        }

        em.RemoveComponent(entity, compId);
        return true;
    }

    public static bool TryRead(World world, EntityManager em, Entity entity, Type componentType, out object? value)
    {
        value = null;
        if (!entity.IsValid || !em.Exists(entity))
        {
            return false;
        }

        var compId = ComponentRegistry.GetComponentID(componentType);
        if (compId.IsInvalid)
        {
            return false;
        }

        var info = ComponentRegistry.GetComponentInfo(compId);
        if (info.IsShared || info.IsCleanup)
        {
            return false;
        }

        if (!em.TryGetEntityLocation(entity, out var location))
        {
            return false;
        }

        ref var archetype = ref world.ComponentManager.GetArchetypeReference(location.archetypeID);
        if (!archetype.HasComponent(compId))
        {
            return false;
        }

        var ptr = archetype.GetComponentData(location.chunkIndex, location.rowIndex, compId);
        if (ptr == null)
        {
            return false;
        }

        var accessor = GetOrCreate(componentType);
        if (accessor == null)
        {
            return false;
        }

        value = accessor.Read(ptr);
        return true;
    }

    public static bool TrySetField(World world, EntityManager em, Entity entity, Type componentType, string fieldName, object? newValue)
    {
        if (!entity.IsValid || !em.Exists(entity))
        {
            return false;
        }

        var compId = ComponentRegistry.GetComponentID(componentType);
        if (compId.IsInvalid)
        {
            return false;
        }

        var info = ComponentRegistry.GetComponentInfo(compId);
        if (info.IsShared || info.IsCleanup)
        {
            return false;
        }

        if (!em.TryGetEntityLocation(entity, out var location))
        {
            return false;
        }

        ref var archetype = ref world.ComponentManager.GetArchetypeReference(location.archetypeID);
        if (!archetype.HasComponent(compId))
        {
            return false;
        }

        var ptr = archetype.GetComponentData(location.chunkIndex, location.rowIndex, compId);
        if (ptr == null)
        {
            return false;
        }

        var field = componentType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        var accessor = GetOrCreate(componentType);
        if (field == null || accessor == null)
        {
            return false;
        }

        var boxed = accessor.Read(ptr);
        field.SetValue(boxed, newValue);
        accessor.Write(ptr, boxed);
        return true;
    }

    private static IAccessor? GetOrCreate(Type componentType)
    {
        return s_accessors.GetOrAdd(componentType, static type =>
        {
            if (!type.IsValueType || !typeof(IComponentData).IsAssignableFrom(type))
            {
                return null;
            }

            var closedType = typeof(TypedAccessor<>).MakeGenericType(type);
            return (IAccessor?)Activator.CreateInstance(closedType);
        });
    }
}
