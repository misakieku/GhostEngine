using Ghost.Entities;
using System;
using System.Collections.Generic;

namespace Ghost.Editor.Core.Inspector;

/// <summary>
/// Model for an entire entity being inspected.
/// Discovers components from archetype, builds ComponentModels.
/// </summary>
public sealed unsafe class EntityInspectorModel : IDisposable
{
    private readonly World _world;
    private readonly Entity _entity;
    private readonly List<ComponentModel> _components = new();
    private int _lastArchetypeId = -1;

    public World World => _world;
    public Entity Entity => _entity;
    public IReadOnlyList<ComponentModel> Components => _components;

    public EntityInspectorModel(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    /// <summary>
    /// Called when entity archetype may have changed.
    /// Returns true if structure was rebuilt (components added/removed).
    /// </summary>
    public bool RefreshStructure()
    {
        var locationResult = _world.EntityManager.GetEntityLocation(_entity);
        if (locationResult.IsFailure) return false;

        var location = locationResult.Value;
        if (location.archetypeID == _lastArchetypeId) return false;

        _lastArchetypeId = location.archetypeID;
        RebuildComponentList();
        return true;
    }

    /// <summary>
    /// Read all component values from ECS -> model.
    /// </summary>
    public void SyncFromECS()
    {
        if (!_world.EntityManager.Exists(_entity)) return;

        foreach (var comp in _components)
        {
            // Fresh pointer every tick - never cached
            var ptr = _world.EntityManager.GetComponent(_entity, comp.Descriptor.ComponentId);
            if (ptr != null)
                comp.SyncFromECS(ptr);
        }
    }

    /// <summary>
    /// Write dirty model values -> ECS.
    /// </summary>
    public void FlushToECS()
    {
        if (!_world.EntityManager.Exists(_entity)) return;

        foreach (var comp in _components)
        {
            var ptr = _world.EntityManager.GetComponent(_entity, comp.Descriptor.ComponentId);
            if (ptr != null)
                comp.FlushToECS(ptr);
        }
    }

    private void RebuildComponentList()
    {
        _components.Clear();
        
        if (!_world.EntityManager.Exists(_entity)) return;
        ref readonly var archetype = ref _world.EntityManager.GetEntityArchetype(_entity);

#if DEBUG || GHOST_EDITOR
        var it = archetype._signature.GetIterator();
        while (it.Next(out var componentID))
        {
            var info = ComponentRegistry.GetComponentInfo(new Ghost.Core.Identifier<Ghost.Entities.IComponent>(componentID));
            if (info.isCleanup)
                continue;

            if (ComponentRegistry.s_runtimeIDToType.TryGetValue(componentID, out var type))
            {
                var descriptor = ComponentDescriptorRegistry.GetOrCreate(type);
                _components.Add(new ComponentModel(descriptor));
            }
        }
#endif
    }

    public void Dispose()
    {
        _components.Clear();
    }
}
