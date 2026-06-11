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
    private readonly List<ComponentEditor> _activeCustomEditors = new();
    private int _lastArchetypeId = -1;

    public World World => _world;
    public Entity Entity => _entity;
    public IReadOnlyList<ComponentNode> Components => _components;

    public EntityInspectorModel(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
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

    private static void BuildPropertyUI(PropertyNode propNode, Panel container)
    {
        var drawer = PropertyDrawerRegistry.GetDrawer(propNode.Descriptor.ValueType);
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
                prop.Sync();
            }
        }
    }

    public void Sync()
    {
        if (!_world.EntityManager.Exists(_entity))
        {
            return;
        }

        RefreshStructure();
        SyncFromECS();
    }

    // TODO: Deselect is not supported yet.

    public UIElement BuildUI()
    {
        RefreshStructure();

        var container = new StackPanel { Spacing = 4 };

        foreach (var compNode in _components)
        {
            var expander = new Controls.ComponentExpander
            {
                Title = compNode.Descriptor.DisplayName,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(4, 2, 4, 2)
            };

            expander.RemoveRequested += (s, e) =>
            {
                compNode.EntityNode.RemoveComponent(compNode.ComponentType);
            };

            var propertiesPanel = new StackPanel { Spacing = 8 };

            if (ComponentEditorRegistry.HasCustomEditor(compNode.ComponentType))
            {
                var editor = ComponentEditorRegistry.CreateCustomEditor(compNode.ComponentType);
                if (editor != null)
                {
                    editor.Create(propertiesPanel, compNode);
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

            expander.ExpandedContent = propertiesPanel;
            container.Children.Add(expander);
        }

        var addComponentBtn = new Button
        {
            Content = "+ Add Component",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(4, 12, 4, 4)
        };

        var flyout = new Flyout();
        var flyoutContent = new StackPanel { Spacing = 4, Width = 250 };
        
        var searchBox = new AutoSuggestBox { PlaceholderText = "Search components..." };
        var listView = new ListView { MaxHeight = 300 };

        void UpdateList(string query)
        {
            var items = new List<Type>();
            var lowerQuery = query.ToLowerInvariant();
            foreach (var kvp in ComponentRegistry.s_runtimeIDToType)
            {
                var type = kvp.Value;
                if (_components.Any(c => c.ComponentType == type)) continue;
                
                var info = ComponentRegistry.GetComponentInfo(new Ghost.Core.Identifier<IComponent>(kvp.Key));
                if (info.isCleanup) continue;

                if (string.IsNullOrEmpty(query) || type.Name.ToLowerInvariant().Contains(lowerQuery))
                {
                    items.Add(type);
                }
            }
            listView.ItemsSource = items.Select(t => new TextBlock { Text = t.Name, Tag = t }).ToList();
        }

        searchBox.TextChanged += (s, args) =>
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                UpdateList(searchBox.Text);
            }
        };

        listView.ItemClick += (s, args) =>
        {
            if (args.ClickedItem is TextBlock tb && tb.Tag is Type t)
            {
                _entityNode?.AddComponent(t);
                flyout.Hide();
            }
        };
        listView.IsItemClickEnabled = true;
        listView.SelectionMode = ListViewSelectionMode.None;

        flyout.Opened += (s, e) =>
        {
            searchBox.Text = "";
            UpdateList("");
            searchBox.Focus(FocusState.Programmatic);
        };

        flyoutContent.Children.Add(searchBox);
        flyoutContent.Children.Add(listView);
        flyout.Content = flyoutContent;

        addComponentBtn.Flyout = flyout;
        container.Children.Add(addComponentBtn);

        return container;
    }

    public void Dispose()
    {
        _components.Clear();
        _activeCustomEditors.Clear();
    }
}
