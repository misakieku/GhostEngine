using Ghost.Editor.Core.Inspector;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;

namespace Ghost.Editor.Core.Services;

/// <summary>
/// Syncs the inspector model from ECS data on every render frame.
/// Uses CompositionTarget.Rendering with a 60Hz cap.
/// </summary>
public sealed class InspectorSyncService : IDisposable
{
    private EntityInspectorModel? _activeModel;
    private Inspector.ComponentEditor? _activeCustomEditor;
    private long _lastSyncTick;
    private static readonly long s_minSyncInterval = Stopwatch.Frequency / 60;
    private bool _isStarted;

    public void Start()
    {
        if (_isStarted) return;
        CompositionTarget.Rendering += OnRendering;
        _isStarted = true;
    }

    public void Bind(EntityInspectorModel model)
    {
        _activeModel = model;
    }

    public void BindCustomEditor(Inspector.ComponentEditor editor)
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
            return;
        
        _lastSyncTick = now;

        if (_activeModel == null) return;

        // 1. Check entity still alive
        if (!_activeModel.World.EntityManager.Exists(_activeModel.Entity))
        {
            Unbind();
            return;
        }

        // 2. Check archetype change -> rebuild if needed
        _activeModel.RefreshStructure();

        // 3. Sync ECS -> model (PropertyChanged fires -> UI updates)
        _activeModel.SyncFromECS();

        // 4. Sync custom editor bindings
        _activeCustomEditor?.SyncBindings();

        // 5. Flush dirty writes back to ECS
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
