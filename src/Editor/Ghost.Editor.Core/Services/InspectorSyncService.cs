using Ghost.Editor.Core.Inspector;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;

namespace Ghost.Editor.Core.Services;

/// <summary>
/// Syncs the inspector model from ECS data on every render frame.
/// Uses CompositionTarget.Rendering with a 60Hz cap.
/// </summary>
public sealed class InspectorSyncService : IDisposable
{
    private EntityInspectorModel? _activeModel;
    private ComponentEditor? _activeCustomEditor;
    private long _lastSyncTick;
    private static readonly long s_minSyncInterval = Stopwatch.Frequency / 60;
    private bool _isStarted;

    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        CompositionTarget.Rendering += OnRendering;
        _isStarted = true;
    }

    public void Bind(EntityInspectorModel model)
    {
        _activeModel = model;
    }

    public void BindCustomEditor(ComponentEditor editor)
    {
        _activeCustomEditor = editor;
    }

    public void Unbind()
    {
        _activeModel = null;
        _activeCustomEditor = null;
    }

    private void OnRendering(object? sender, object e)
    {
        var now = Stopwatch.GetTimestamp();
        if (now - _lastSyncTick < s_minSyncInterval)
        {
            return;
        }

        _lastSyncTick = now;

        if (_activeModel == null)
        {
            return;
        }

        if (!_activeModel.World.EntityManager.Exists(_activeModel.Entity))
        {
            Unbind();
            return;
        }

        _activeModel.RefreshStructure();
        _activeModel.SyncFromECS();
        _activeCustomEditor?.SyncBindings();
        _activeModel.FlushToECS();
    }

    public void Dispose()
    {
        if (_isStarted)
        {
            CompositionTarget.Rendering -= OnRendering;
            _isStarted = false;
        }
    }
}
