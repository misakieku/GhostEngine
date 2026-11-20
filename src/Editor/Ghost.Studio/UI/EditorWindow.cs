using Ghost.Studio.Backend;
using Hexa.NET.ImGui;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Base class for dockable and floating Studio windows (both built-in tool panels and multi-tab asset editors).
/// Uses ImGui's <c>Title###WindowId</c> convention so dynamic titles or dirty state never reset docking/layout.
/// </summary>
internal abstract class EditorWindow : IDisposable
{
    private bool _focusRequested;
    private uint _pendingDockId;

    public bool IsOpen;

    public string Title { get; protected set; }

    public string WindowId { get; }

    public string ImGuiLabel => string.Equals(Title, WindowId, StringComparison.Ordinal) ? Title : $"{Title}###{WindowId}";

    public virtual bool IsDirty => false;

    public virtual bool ShowInWindowMenu => true;

    public virtual ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.None;

    public virtual Vector2 DefaultPos => default;

    public virtual Vector2 DefaultSize => new(420.0f, 520.0f);

    public virtual bool ScaleDefaultSizeByDpi => false;

    public uint LastDockId { get; private set; }

    public bool IsFocused { get; private set; }

    protected EditorWindow(string title, string? windowId = null, bool openByDefault = true)
    {
        Title = title;
        WindowId = string.IsNullOrEmpty(windowId) ? title : windowId;
        IsOpen = openByDefault;
    }

    public void RequestFocus()
    {
        IsOpen = true;
        _focusRequested = true;
    }

    public void SetPendingDockId(uint dockId)
    {
        _pendingDockId = dockId;
    }

    public void Draw(ImGuiContextManager contextManager)
    {
        if (!IsOpen)
        {
            IsFocused = false;
            return;
        }

        var scale = ScaleDefaultSizeByDpi ? contextManager.DpiScale : 1.0f;
        if (DefaultPos != default)
        {
            ImGui.SetNextWindowPos(DefaultPos, ImGuiCond.FirstUseEver);
        }

        if (DefaultSize != default)
        {
            ImGui.SetNextWindowSize(DefaultSize * scale, ImGuiCond.FirstUseEver);
        }

        if (_pendingDockId != 0)
        {
            ImGui.SetNextWindowDockID(_pendingDockId, ImGuiCond.Appearing);
            _pendingDockId = 0;
        }

        if (_focusRequested)
        {
            ImGui.SetNextWindowFocus();
            _focusRequested = false;
        }

        OnBeforeBegin(contextManager);
        var flags = WindowFlags | (IsDirty ? ImGuiWindowFlags.UnsavedDocument : ImGuiWindowFlags.None);
        var visible = ImGui.Begin(ImGuiLabel, ref IsOpen, flags);
        OnAfterBegin(contextManager);

        if (visible)
        {
            LastDockId = ImGui.GetWindowDockID();
            IsFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
            OnDraw(contextManager);
        }
        else
        {
            IsFocused = false;
            OnCollapsed(contextManager);
        }

        ImGui.End();

        if (!IsOpen)
        {
            IsFocused = false;
            OnClosed();
        }
    }

    protected virtual void OnBeforeBegin(ImGuiContextManager contextManager)
    {
    }

    protected virtual void OnAfterBegin(ImGuiContextManager contextManager)
    {
    }

    protected virtual void OnCollapsed(ImGuiContextManager contextManager)
    {
    }

    protected abstract void OnDraw(ImGuiContextManager contextManager);

    protected virtual void OnClosed()
    {
    }

    public virtual void Dispose()
    {
    }
}
