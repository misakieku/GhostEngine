using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.SceneGraph;
using Ghost.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Inspector;

/// <summary>
/// Model for an entire entity being inspected.
/// Discovers components from archetype, builds ComponentModels.
/// </summary>
public sealed class EntityInspectorModel : ISyncableInspectorModel
{
    private readonly World _world;
    private readonly Entity _entity;
    private EntityNode? _entityNode;
    private readonly List<ComponentNode> _components = new();
    private int _lastArchetypeId = -1;

    public World World => _world;
    public Entity Entity => _entity;
    public IReadOnlyList<ComponentNode> Components => _components;

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
            foreach (var prop in comp.Properties)
            {
                prop.SyncFromECS();
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
            foreach (var prop in comp.Properties)
            {
                prop.FlushToECS();
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

        if (_entityNode == null)
        {
            var syncService = EditorApplication.GetService<Services.SceneGraphSyncService>();
            if (syncService != null && syncService.TryGetNode(_entity, out var node))
            {
                _entityNode = node;
            }
        }

        if (_entityNode != null)
        {
            // Update components list in EntityNode first
            _entityNode.BuildComponents();

            foreach (var compNode in _entityNode.Components)
            {
                _components.Add(compNode);
            }
        }
    }

    private readonly List<ComponentEditor> _activeCustomEditors = new();

    public void Sync()
    {
        if (!_world.EntityManager.Exists(_entity)) return;
        RefreshStructure();
        SyncFromECS();
        foreach (var editor in _activeCustomEditors)
        {
            editor.SyncBindings();
        }
        FlushToECS();
    }

    public UIElement BuildUI()
    {
        RefreshStructure();

        var container = new StackPanel { Spacing = 4 };

        foreach (var compNode in _components)
        {
            var expander = new Expander
            {
                Header = compNode.Descriptor.DisplayName,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                IsExpanded = true,
                Margin = new Thickness(4, 2, 4, 2)
            };

            var propertiesPanel = new StackPanel { Spacing = 8 };

            if (ComponentEditorRegistry.HasCustomEditor(compNode.ComponentType))
            {
                var editor = ComponentEditorRegistry.CreateCustomEditor(compNode.ComponentType);
                if (editor != null)
                {
                    var compObject = new ComponentObject(_world, _entity);
                    editor.Initialize(compObject);
                    editor.Create(propertiesPanel);
                    _activeCustomEditors.Add(editor);
                }
            }
            else
            {
                foreach (var propNode in compNode.Properties)
                {
                    BuildPropertyUI(propNode, propertiesPanel);
                }
            }

            expander.Content = propertiesPanel;
            container.Children.Add(expander);
        }

        return container;
    }

    private static void BuildPropertyUI(PropertyNode propNode, Panel container)
    {
        var drawer = PropertyDrawerRegistry.GetDrawer(propNode.Descriptor.FieldType);
        var control = drawer.CreateControl(propNode);

        var propertyField = new Controls.PropertyField
        {
            Label = propNode.Descriptor.DisplayName,
            Content = control,
            IsEditable = !propNode.Descriptor.IsReadOnly
        };

        container.Children.Add(propertyField);

        if (propNode.Children != null && propNode.Children.Length > 0)
        {
            var childrenPanel = new StackPanel { Spacing = 4, Margin = new Thickness(12, 4, 0, 0) };
            foreach (var child in propNode.Children)
            {
                BuildPropertyUI(child, childrenPanel);
            }
            container.Children.Add(childrenPanel);
        }
    }

    public void Dispose()
    {
        _components.Clear();
        _activeCustomEditors.Clear();
    }
}
