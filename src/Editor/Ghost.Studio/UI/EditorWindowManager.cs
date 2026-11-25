using Ghost.Studio.Backend;
using Hexa.NET.ImGui;

namespace Ghost.Studio.UI;

/// <summary>
/// Central registry and lifecycle manager for all Studio <see cref="EditorWindow"/> instances,
/// dynamic multi-tab <see cref="AssetEditorWindow"/> tabs, and extension-based asset opening.
/// </summary>
internal sealed unsafe class EditorWindowManager : IDisposable
{
    private readonly List<EditorWindow> _registeredWindows = new();
    private readonly List<AssetEditorWindow> _assetWindows = new();
    private readonly Dictionary<string, Func<string, AssetEditorWindow?>> _assetEditorFactories = new(StringComparer.OrdinalIgnoreCase);

    private uint _lastFloatingHostDockId;

    public IReadOnlyList<EditorWindow> RegisteredWindows => _registeredWindows;

    public IReadOnlyList<AssetEditorWindow> AssetWindows => _assetWindows;

    public void RegisterWindow(EditorWindow window)
    {
        if (!_registeredWindows.Contains(window))
        {
            _registeredWindows.Add(window);
        }
    }

    public void RegisterAssetEditor(string extension, Func<string, AssetEditorWindow?> factory)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return;
        }

        var normalized = extension.StartsWith('.') ? extension : "." + extension;
        _assetEditorFactories[normalized] = factory;
    }

    public bool HasAssetEditor(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        var normalized = extension.StartsWith('.') ? extension : "." + extension;
        return _assetEditorFactories.ContainsKey(normalized);
    }

    /// <summary>
    /// Opens or focuses the registered <see cref="AssetEditorWindow"/> for <paramref name="fullPath"/>.
    /// Does nothing if no editor is registered for the file's extension.
    /// </summary>
    public bool OpenAsset(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
        {
            return false;
        }

        var canonical = Path.GetFullPath(fullPath);
        for (var i = 0; i < _assetWindows.Count; i++)
        {
            var existing = _assetWindows[i];
            if (string.Equals(existing.AssetPath, canonical, StringComparison.OrdinalIgnoreCase))
            {
                existing.RequestFocus();
                return true;
            }
        }

        var ext = Path.GetExtension(canonical);
        if (!_assetEditorFactories.TryGetValue(ext, out var factory))
        {
            return false;
        }

        var created = factory(canonical);
        if (created == null)
        {
            return false;
        }

        var hostDockId = ResolveOrCreateFloatingHostDockId(created);
        if (hostDockId != 0)
        {
            created.SetPendingDockId(hostDockId);
        }

        created.RequestFocus();
        _assetWindows.Add(created);
        return true;
    }

    public void DrawAll(ImGuiContextManager contextManager)
    {
        for (var i = 0; i < _registeredWindows.Count; i++)
        {
            _registeredWindows[i].Draw(contextManager);
        }

        for (var i = _assetWindows.Count - 1; i >= 0; i--)
        {
            var assetWindow = _assetWindows[i];
            assetWindow.Draw(contextManager);

            if (assetWindow.IsOpen && assetWindow.LastDockId != 0)
            {
                _lastFloatingHostDockId = assetWindow.LastDockId;
            }

            if (!assetWindow.IsOpen)
            {
                _assetWindows.RemoveAt(i);
                assetWindow.Dispose();
            }
        }
    }

    public void CollectDirtyAssetWindows(List<AssetEditorWindow> destination)
    {
        destination.Clear();
        for (var i = 0; i < _assetWindows.Count; i++)
        {
            var win = _assetWindows[i];
            if (win.IsOpen && win.IsDirty)
            {
                destination.Add(win);
            }
        }
    }

    public void CloseAllAssetWindows()
    {
        for (var i = 0; i < _assetWindows.Count; i++)
        {
            _assetWindows[i].IsOpen = false;
            _assetWindows[i].Dispose();
        }
        _assetWindows.Clear();
    }

    public void Dispose()
    {
        for (var i = 0; i < _assetWindows.Count; i++)
        {
            _assetWindows[i].Dispose();
        }
        _assetWindows.Clear();

        for (var i = 0; i < _registeredWindows.Count; i++)
        {
            _registeredWindows[i].Dispose();
        }
        _registeredWindows.Clear();
    }

    private uint ResolveOrCreateFloatingHostDockId(EditorWindow window)
    {
        if (ImGui.GetCurrentContext().Handle == null)
        {
            return 0;
        }

        // Reuse an existing open asset tab's dock node so new custom windows stack as tabs automatically.
        for (var i = _assetWindows.Count - 1; i >= 0; i--)
        {
            var existing = _assetWindows[i];
            if (existing.IsOpen && existing.LastDockId != 0)
            {
                _lastFloatingHostDockId = existing.LastDockId;
                return _lastFloatingHostDockId;
            }
        }

        if (_lastFloatingHostDockId != 0 && ImGuiP.DockBuilderGetNode(_lastFloatingHostDockId).Handle != null)
        {
            return _lastFloatingHostDockId;
        }

        var viewport = ImGui.GetMainViewport();
        var size = window.DefaultSize;
        var pos = viewport.WorkPos + (viewport.WorkSize - size) * 0.5f;

        var hostNodeId = ImGui.GetID("GhostStudioAssetHostDock");
        ImGuiP.DockBuilderRemoveNode(hostNodeId);
        ImGuiP.DockBuilderAddNode(hostNodeId, ImGuiDockNodeFlags.None);
        ImGuiP.DockBuilderSetNodePos(hostNodeId, pos);
        ImGuiP.DockBuilderSetNodeSize(hostNodeId, size);
        ImGuiP.DockBuilderDockWindow(window.ImGuiLabel, hostNodeId);
        ImGuiP.DockBuilderFinish(hostNodeId);

        _lastFloatingHostDockId = hostNodeId;
        return hostNodeId;
    }
}
