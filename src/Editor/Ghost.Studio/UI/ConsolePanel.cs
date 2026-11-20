using Ghost.Core;
using Ghost.Studio.Backend;
using Hexa.NET.ImGui;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Thread-safe, virtualized Console window subscribed to <see cref="Logger"/> with severity filtering and stack inspection.
/// </summary>
internal sealed class ConsolePanel : EditorWindow
{
    private readonly struct CollapsedEntry
    {
        public readonly LogMessage Message;
        public readonly int Count;

        public CollapsedEntry(in LogMessage message, int count)
        {
            Message = message;
            Count = count;
        }
    }

    private readonly object _lock = new();
    private readonly List<LogMessage> _entries = new();
    private readonly List<CollapsedEntry> _filtered = new();

    private bool _showInfo = true;
    private bool _showWarning = true;
    private bool _showError = true;
    private bool _collapse;
    private bool _autoScroll = true;
    private string _searchQuery = string.Empty;
    private int _selectedIndex = -1;
    private bool _dirty = true;
    private bool _scrollToBottomRequested;

    public override Vector2 DefaultPos => new(300.0f, 640.0f);

    public override Vector2 DefaultSize => new(640.0f, 220.0f);

    public ConsolePanel()
        : base("Console")
    {
        lock (_lock)
        {
            foreach (var existing in Logger.Logs)
            {
                _entries.Add(existing);
            }
        }

        Logger.Impl.OnLogAdded += OnLogAdded;
        Logger.Impl.OnLogsCleared += OnLogsCleared;
    }

    protected override unsafe void OnDraw(ImGuiContextManager contextManager)
    {

        ref readonly var tokens = ref contextManager.Tokens;

        CountLevels(out var infoCount, out var warnCount, out var errCount);
        DrawToolbar(in tokens, infoCount, warnCount, errCount);
        ImGui.Separator();

        if (_dirty)
        {
            RebuildFilteredList();
            _dirty = false;
        }

        var detailHeight = _selectedIndex >= 0 && _selectedIndex < _filtered.Count ? 72.0f * contextManager.DpiScale : 0.0f;
        var listHeight = detailHeight > 0.0f ? -detailHeight - ImGui.GetStyle().ItemSpacing.Y : 0.0f;

        ImGui.BeginChild("##ConsoleLogScroll", new Vector2(0.0f, listHeight), ImGuiChildFlags.None);
        ImGui.PushFont(contextManager.MonospaceFont, EditorTheme.FONT_SIZE_MONO);

        var rowHeight = ImGui.GetTextLineHeightWithSpacing();
        var clipper = ImGui.ImGuiListClipper();
        clipper.Begin(_filtered.Count, rowHeight);

        while (clipper.Step())
        {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                ref readonly var entry = ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_filtered)[i];
                var isSelected = i == _selectedIndex;
                var msgColor = ImGuiWidgets.GetLogColor(entry.Message.Level, in tokens);

                ImGui.PushID(i);
                ImGui.PushStyleColor(ImGuiCol.Text, msgColor);

                var prefix = ImGuiWidgets.GetLogTag(entry.Message.Level);
                var suffix = entry.Count > 1 ? $"  ({entry.Count}x)" : string.Empty;
                var line = $"{entry.Message.Timestamp:HH:mm:ss} {prefix} {SingleLinePreview(entry.Message.Message)}{suffix}";

                if (ImGui.Selectable(line, isSelected))
                {
                    _selectedIndex = i;
                }

                ImGui.PopStyleColor();
                ImGui.PopID();
            }
        }

        clipper.Destroy();
        ImGui.PopFont();

        if (_scrollToBottomRequested && _autoScroll)
        {
            ImGui.SetScrollHereY(1.0f);
            _scrollToBottomRequested = false;
        }

        ImGui.EndChild();

        if (detailHeight > 0.0f && _selectedIndex >= 0 && _selectedIndex < _filtered.Count)
        {
            ImGui.Separator();
            ImGui.BeginChild("##ConsoleDetail", Vector2.Zero, ImGuiChildFlags.None);
            ImGui.PushFont(contextManager.MonospaceFont, EditorTheme.FONT_SIZE_MONO);
            var selected = _filtered[_selectedIndex].Message;
            ImGui.TextWrapped(string.IsNullOrEmpty(selected.StackTrace) ? selected.Message : $"{selected.Message}\n{selected.StackTrace}");
            ImGui.PopFont();
            ImGui.EndChild();
        }
    }

    private void DrawToolbar(in EditorColorTokens tokens, int infoCount, int warnCount, int errCount)
    {
        if (ImGui.Button("Clear"))
        {
            Logger.Impl.Clear();
        }

        ImGui.SameLine();
        if (ImGuiWidgets.ToggleButton("Collapse", _collapse, in tokens))
        {
            _collapse = !_collapse;
            _dirty = true;
        }

        ImGui.SameLine();
        if (ImGuiWidgets.ToggleButton("Auto-Scroll", _autoScroll, in tokens))
        {
            _autoScroll = !_autoScroll;
        }

        ImGui.SameLine();
        ImGuiWidgets.SearchField("##ConsoleFilter", "Filter logs...", ref _searchQuery, 200.0f, in tokens);

        ImGui.SameLine();
        if (ImGuiWidgets.ToggleButton($"Info ({infoCount})", _showInfo, in tokens))
        {
            _showInfo = !_showInfo;
            _dirty = true;
        }

        ImGui.SameLine();
        if (ImGuiWidgets.ToggleButton($"Warn ({warnCount})", _showWarning, in tokens))
        {
            _showWarning = !_showWarning;
            _dirty = true;
        }

        ImGui.SameLine();
        if (ImGuiWidgets.ToggleButton($"Err ({errCount})", _showError, in tokens))
        {
            _showError = !_showError;
            _dirty = true;
        }
    }

    private void CountLevels(out int infoCount, out int warnCount, out int errCount)
    {
        infoCount = 0;
        warnCount = 0;
        errCount = 0;

        lock (_lock)
        {
            for (var i = 0; i < _entries.Count; i++)
            {
                switch (_entries[i].Level)
                {
                    case LogLevel.Warning:
                        warnCount++;
                        break;
                    case LogLevel.Error:
                    case LogLevel.Fatal:
                        errCount++;
                        break;
                    default:
                        infoCount++;
                        break;
                }
            }
        }
    }

    private void RebuildFilteredList()
    {
        _filtered.Clear();

        List<LogMessage> snapshot;
        lock (_lock)
        {
            snapshot = new List<LogMessage>(_entries);
        }

        var collapseMap = _collapse ? new Dictionary<(LogLevel, string), int>() : null;

        for (var i = 0; i < snapshot.Count; i++)
        {
            var msg = snapshot[i];
            if (!MatchesFilter(in msg))
            {
                continue;
            }

            if (collapseMap != null)
            {
                var key = (msg.Level, msg.Message);
                if (collapseMap.TryGetValue(key, out var idx))
                {
                    _filtered[idx] = new CollapsedEntry(_filtered[idx].Message, _filtered[idx].Count + 1);
                    continue;
                }

                collapseMap[key] = _filtered.Count;
            }

            _filtered.Add(new CollapsedEntry(in msg, 1));
        }

        if (_selectedIndex >= _filtered.Count)
        {
            _selectedIndex = -1;
        }
    }

    private bool MatchesFilter(in LogMessage msg)
    {
        var levelOk = msg.Level switch
        {
            LogLevel.Warning => _showWarning,
            LogLevel.Error or LogLevel.Fatal => _showError,
            _ => _showInfo,
        };

        if (!levelOk)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(_searchQuery) || msg.Message.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase);
    }

    private void OnLogAdded(LogMessage message)
    {
        lock (_lock)
        {
            _entries.Add(message);
        }

        _dirty = true;
        _scrollToBottomRequested = true;
    }

    private void OnLogsCleared()
    {
        lock (_lock)
        {
            _entries.Clear();
        }

        _selectedIndex = -1;
        _dirty = true;
    }

    private static string SingleLinePreview(string text)
    {
        var newlineIdx = text.IndexOfAny(new char[] { '\r', '\n' });
        return newlineIdx >= 0 ? text[..newlineIdx] : text;
    }

    public override void Dispose()
    {
        Logger.Impl.OnLogAdded -= OnLogAdded;
        Logger.Impl.OnLogsCleared -= OnLogsCleared;
    }
}
