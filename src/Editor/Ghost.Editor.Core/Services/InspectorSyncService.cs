using Ghost.Editor.Core.Contracts;

namespace Ghost.Editor.Core.Services;

/// <summary>
/// Syncs the inspector model from ECS data on every editor tick (Phase 3).
/// </summary>
public sealed class InspectorSyncService : IDisposable
{
    private readonly EditorTickEngine _tickEngine;
    private ISyncableInspectorModel? _activeModel;
    private bool _isStarted;

    public InspectorSyncService(EditorTickEngine tickEngine)
    {
        _tickEngine = tickEngine;
    }

    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        _tickEngine.OnInspectorSync += OnInspectorSync;
        _isStarted = true;
    }

    public void Bind(ISyncableInspectorModel model)
    {
        _activeModel = model;
    }

    public void Unbind()
    {
        _activeModel = null;
    }

    private void OnInspectorSync()
    {
        if (_activeModel == null)
        {
            return;
        }

        _activeModel.Sync();
    }

    public void Dispose()
    {
        if (_isStarted)
        {
            _tickEngine.OnInspectorSync -= OnInspectorSync;
            _isStarted = false;
        }
    }
}
