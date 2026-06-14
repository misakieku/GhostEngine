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
internal sealed class EntityInspectorModel : ISyncableInspectorModel
{
    private readonly World _world;
    private readonly Entity _entity;
    private EntityNode? _entityNode;
    private readonly List<ComponentNode> _components = new();
    private readonly List<ComponentEditor> _activeCustomEditors = new();
    private int _lastArchetypeId = -1;

    // Master-Detail UI State
    private StackPanel? _rootContainer;
    private ListView? _masterListView;
    private StackPanel? _detailContainer;
    private AutoSuggestBox? _masterSearchBox;
    private bool _isUpdatingSelection = false;
    private HashSet<Type> _knownComponentTypes = new();

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

        if (RefreshStructure())
        {
            UpdateMasterListView(false);
        }

        SyncFromECS();
    }

    public UIElement BuildUI()
    {
        RefreshStructure();

        if (_rootContainer == null)
        {
            _rootContainer = new StackPanel { Spacing = 8 };

            // --- Master Section ---
            var masterContainer = new StackPanel { Spacing = 4 };
            
            _masterSearchBox = new AutoSuggestBox { PlaceholderText = "Filter components..." };
            _masterSearchBox.TextChanged += (s, args) =>
            {
                if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                {
                    UpdateMasterListView(false);
                }
            };

            _masterListView = new ListView
            {
                SelectionMode = ListViewSelectionMode.Extended,
                MaxHeight = 300,
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"]
            };
            _masterListView.SelectionChanged += (s, e) =>
            {
                RebuildDetailView();
            };

            masterContainer.Children.Add(_masterSearchBox);
            masterContainer.Children.Add(_masterListView);

            var addComponentBtn = new Button
            {
                Content = "+ Add Component",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 4, 0, 0)
            };
            SetupAddComponentFlyout(addComponentBtn);
            masterContainer.Children.Add(addComponentBtn);

            _rootContainer.Children.Add(masterContainer);

            // --- Detail Section ---
            _detailContainer = new StackPanel { Spacing = 8 };
            _rootContainer.Children.Add(_detailContainer);
        }

        UpdateMasterListView(true);
        return _rootContainer;
    }

    private void UpdateMasterListView(bool isInitialLoad)
    {
        if (_masterListView == null) return;

        var query = _masterSearchBox?.Text?.ToLowerInvariant() ?? "";
        var oldSelectedTypes = _masterListView.SelectedItems.Cast<TextBlock>().Select(tb => ((ComponentNode)tb.Tag).ComponentType).ToHashSet();

        var items = new List<TextBlock>();
        foreach (var compNode in _components)
        {
            if (string.IsNullOrEmpty(query) || compNode.Descriptor.DisplayName.ToLowerInvariant().Contains(query))
            {
                var tb = new TextBlock 
                { 
                    Text = compNode.Descriptor.DisplayName, 
                    Tag = compNode,
                    Margin = new Thickness(0, 4, 0, 4)
                };

                var flyout = new MenuFlyout();
                var removeMenuItem = new MenuFlyoutItem { Text = "Remove Component", Icon = new FontIcon { Glyph = "\uE74D" } };
                removeMenuItem.Click += (s, e) =>
                {
                    compNode.EntityNode.RemoveComponent(compNode.ComponentType);
                };
                flyout.Items.Add(removeMenuItem);
                tb.ContextFlyout = flyout;

                items.Add(tb);
            }
        }

        _isUpdatingSelection = true;
        _masterListView.ItemsSource = items;

        foreach (var item in items)
        {
            var type = ((ComponentNode)item.Tag).ComponentType;
            bool shouldSelect = false;

            if (isInitialLoad)
            {
                shouldSelect = true;
                _knownComponentTypes.Add(type);
            }
            else
            {
                if (!_knownComponentTypes.Contains(type))
                {
                    shouldSelect = true;
                    _knownComponentTypes.Add(type);
                }
                else if (oldSelectedTypes.Contains(type))
                {
                    shouldSelect = true;
                }
            }

            if (shouldSelect)
            {
                _masterListView.SelectedItems.Add(item);
            }
        }

        var currentTypes = _components.Select(c => c.ComponentType).ToHashSet();
        _knownComponentTypes.RemoveWhere(t => !currentTypes.Contains(t));

        _isUpdatingSelection = false;
        RebuildDetailView();
    }

    private void RebuildDetailView()
    {
        if (_isUpdatingSelection || _detailContainer == null || _masterListView == null) return;

        _activeCustomEditors.Clear();
        _detailContainer.Children.Clear();

        var selectedNodes = _masterListView.SelectedItems.Cast<TextBlock>().Select(tb => (ComponentNode)tb.Tag).ToList();

        foreach (var compNode in selectedNodes)
        {
            var compHeader = new Border
            {
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 8, 0, 4)
            };

            var headerText = new TextBlock
            {
                Text = compNode.Descriptor.DisplayName,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };

            var flyout = new MenuFlyout();
            var removeMenuItem = new MenuFlyoutItem { Text = "Remove Component", Icon = new FontIcon { Glyph = "\uE74D" } };
            removeMenuItem.Click += (s, e) =>
            {
                compNode.EntityNode.RemoveComponent(compNode.ComponentType);
            };
            flyout.Items.Add(removeMenuItem);
            compHeader.ContextFlyout = flyout;

            compHeader.Child = headerText;
            _detailContainer.Children.Add(compHeader);

            var propertiesPanel = new StackPanel { Spacing = 8, Margin = new Thickness(8, 0, 8, 8) };

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

            _detailContainer.Children.Add(propertiesPanel);
        }
    }

    private void SetupAddComponentFlyout(Button addComponentBtn)
    {
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
    }

    public void Dispose()
    {
        _components.Clear();
        _activeCustomEditors.Clear();
    }
}
