using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Inspector;
using Ghost.Editor.Core.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Views.Controls;

public sealed partial class Inspector : UserControl
{
    private readonly IInspectorService _inspectorService;
    private readonly InspectorSyncService _syncService;

    private EntityInspectorModel? _currentModel;

    public Inspector()
    {
        InitializeComponent();
        _inspectorService = App.GetService<IInspectorService>();
        _syncService = App.GetService<InspectorSyncService>();

        _inspectorService.OnSelectionChanged += InspectorService_OnSelectionChanged;
        Loaded += Inspector_Loaded;
        Unloaded += Inspector_Unloaded;

        if (_inspectorService.Selected != null)
        {
            BuildInspector(_inspectorService.Selected);
        }
    }

    private void Inspector_Loaded(object sender, RoutedEventArgs e)
    {
        _syncService.Start();
    }

    private void Inspector_Unloaded(object sender, RoutedEventArgs e)
    {
        _syncService.Unbind();
        _currentModel?.Dispose();
        _currentModel = null;
    }

    private void InspectorService_OnSelectionChanged(object? sender, InspectorSelectionChangedEventArgs e)
    {
        BuildInspector(e.Selected);
    }

    private void BuildInspector(IInspectable? inspectable)
    {
        // Cleanup old
        _syncService.Unbind();
        _currentModel?.Dispose();
        _currentModel = null;
        InspectorContentContainer.Children.Clear();

        if (inspectable == null)
        {
            IconPresenter.Content = null;
            HeaderPresenter.Content = null;
            return;
        }

        // Set header
        var icon = inspectable.CreateIcon();
        if (icon != null)
        {
            IconPresenter.Content = new IconSourceElement { IconSource = icon };
        }
        else
        {
            IconPresenter.Content = new FontIcon { Glyph = "\uF158", FontSize = 18 };
        }

        HeaderPresenter.Content = inspectable.CreateHeader();

        // Build body
        var descriptor = inspectable.CreateInspectorDescriptor();

        if (descriptor is EntityInspectorDescriptor entityDesc)
        {
            _currentModel = new EntityInspectorModel(entityDesc.World, entityDesc.Entity);
            _currentModel.RefreshStructure();

            foreach (var compModel in _currentModel.Components)
            {
                var expander = new Expander
                {
                    Header = compModel.Descriptor.DisplayName,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    IsExpanded = true,
                    Margin = new Thickness(4, 2, 4, 2)
                };

                var propertiesPanel = new StackPanel { Spacing = 8 };

                if (ComponentEditorRegistry.HasCustomEditor(compModel.Descriptor.ComponentType))
                {
                    var editor = ComponentEditorRegistry.CreateCustomEditor(compModel.Descriptor.ComponentType);
                    if (editor != null)
                    {
                        var compObject = new ComponentObject(entityDesc.World, entityDesc.Entity);
                        editor.Initialize(compObject);
                        editor.Create(propertiesPanel);
                        _syncService.BindCustomEditor(editor);
                    }
                }
                else
                {
                    foreach (var propModel in compModel.Properties)
                    {
                        BuildPropertyUI(propModel, propertiesPanel);
                    }
                }

                expander.Content = propertiesPanel;
                InspectorContentContainer.Children.Add(expander);
            }

            _syncService.Bind(_currentModel);
            _currentModel.SyncFromECS(); // initial sync
        }
        else if (descriptor is CustomInspectorDescriptor customDesc)
        {
            var ui = customDesc.Factory();
            if (ui != null)
            {
                InspectorContentContainer.Children.Add(ui);
            }
        }
    }

    private void BuildPropertyUI(IPropertyModel propModel, Panel container)
    {
        var drawer = PropertyDrawerRegistry.GetDrawer(propModel.Descriptor.FieldType);
        var control = drawer.CreateControl(propModel);

        var propertyField = new Core.Controls.PropertyField
        {
            Label = propModel.Descriptor.DisplayName,
            Content = control
        };

        container.Children.Add(propertyField);

        if (propModel.Children != null && propModel.Children.Length > 0)
        {
            var childrenPanel = new StackPanel { Spacing = 4, Margin = new Thickness(12, 4, 0, 0) };
            foreach (var child in propModel.Children)
            {
                BuildPropertyUI(child, childrenPanel);
            }
            container.Children.Add(childrenPanel);
        }
    }
}
