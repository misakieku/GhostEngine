using Ghost.Entities;
using System.Buffers;

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
        if (locationResult.IsFailure)
        {
            return false;
        }

        var location = locationResult.Value;
        if (location.archetypeID == _lastArchetypeId)
        {
            return false;
        }

        _lastArchetypeId = location.archetypeID;
        RebuildComponentList();
        return true;
    }

    /// <summary>
    /// Read all component values from ECS -> model.
    /// </summary>
    public void SyncFromECS()
    {
        if (!_world.EntityManager.Exists(_entity))
        {
            return;
        }

        foreach (var comp in _components)
        {
            if (comp.Descriptor.IsShared)
            {
                var ptr = _world.EntityManager.GetSharedComponent(_entity, comp.Descriptor.ComponentId);
                if (ptr != null)
                {
                    comp.SyncFromECS(ptr);
                }
            }
            else
            {
                // Fresh pointer every tick - never cached
                var ptr = _world.EntityManager.GetComponent(_entity, comp.Descriptor.ComponentId);
                if (ptr != null)
                {
                    comp.SyncFromECS(ptr);
                }
            }
        }
    }

    /// <summary>
    /// Write dirty model values -> ECS.
    /// </summary>
    public void FlushToECS()
    {
        if (!_world.EntityManager.Exists(_entity))
        {
            return;
        }

        foreach (var comp in _components)
        {
            if (comp.Descriptor.IsShared)
            {
                var ptr = _world.EntityManager.GetSharedComponent(_entity, comp.Descriptor.ComponentId);
                if (ptr != null)
                {
                    // Copy existing shared component data to a local stack buffer
                    var tempArray = ArrayPool<byte>.Shared.Rent(comp.Descriptor.Size);
                    try
                    {
                        fixed (byte* tempBuffer = tempArray)
                        {
                            System.Runtime.CompilerServices.Unsafe.CopyBlock(tempBuffer, ptr, (uint)comp.Descriptor.Size);

                            // Flush local property models to the copied data
                            comp.FlushToECS(tempBuffer);

                            // Call SetSharedComponent with the modified data
                            _world.EntityManager.SetSharedComponent(_entity, comp.Descriptor.ComponentId, tempBuffer);
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(tempArray);
                    }
                }
            }
            else
            {
                var ptr = _world.EntityManager.GetComponent(_entity, comp.Descriptor.ComponentId);
                if (ptr != null)
                {
                    comp.FlushToECS(ptr);
                }
            }
        }
    }

    private void RebuildComponentList()
    {
        _components.Clear();

        if (!_world.EntityManager.Exists(_entity))
        {
            return;
        }

        ref readonly var archetype = ref _world.EntityManager.GetEntityArchetype(_entity);

        var it = archetype._signature.GetIterator();
        while (it.Next(out var componentID))
        {
            var info = ComponentRegistry.GetComponentInfo(new Ghost.Core.Identifier<IComponent>(componentID));
            if (info.isCleanup)
            {
                continue;
            }

            if (ComponentRegistry.s_runtimeIDToType.TryGetValue(componentID, out var type))
            {
                var descriptor = ComponentDescriptorRegistry.GetOrCreate(type);
                _components.Add(new ComponentModel(descriptor));
            }
        }
    }

    public void Dispose()
    {
        _components.Clear();
    }
}
