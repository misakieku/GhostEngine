using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Views.Controls;

public sealed partial class Inspector : UserControl
{
    private readonly IInspectorService _inspectorService;
    private readonly InspectorSyncService _syncService;

    private IInspectorModel? _currentModel;

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
        _currentModel = inspectable.CreateInspectorModel();
        if (_currentModel != null)
        {
            InspectorContentContainer.Children.Add(_currentModel.BuildUI());

            if (_currentModel is ISyncableInspectorModel syncableModel)
            {
                _syncService.Bind(syncableModel);
                syncableModel.Sync(); // Initial sync
            }
        }
    }
}
