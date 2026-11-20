using Ghost.Studio.Assets;
using Ghost.Studio.Backend;
using Ghost.Studio.Services;
using Hexa.NET.ImGui;
using System.Diagnostics;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Displays the workspace asset directory tree and virtualized asset grid/list browser.
/// </summary>
internal sealed class ProjectPanel : EditorWindow
{
    private readonly struct AssetEntryInfo
    {
        public readonly string Name;
        public readonly string FullPath;
        public readonly bool IsDirectory;
        public readonly string Extension;
        public readonly long SizeBytes;

        public AssetEntryInfo(string name, string fullPath, bool isDirectory, string extension, long sizeBytes)
        {
            Name = name;
            FullPath = fullPath;
            IsDirectory = isDirectory;
            Extension = extension;
            SizeBytes = sizeBytes;
        }
    }

    private readonly AssetVisualRegistry _visualRegistry;
    private readonly List<AssetEntryInfo> _currentItems = new();
    private readonly List<AssetEntryInfo> _filteredItems = new();
    private readonly HashSet<string> _expandedDirectories = new(StringComparer.OrdinalIgnoreCase);

    private string _rootDirectory;
    private string _currentDirectory;
    private string _searchQuery = string.Empty;
    private string _selectedPath = string.Empty;
    private AssetViewMode _viewMode = AssetViewMode.List;
    private bool _showMetaFiles;
    private bool _dirty = true;
    private Func<string, bool>? _hasBaker;
    private Action<string>? _onOpenAsset;
    private Action<string>? _onOpenImportSettings;
    private Action<string>? _onReimportAsset;

    public AssetImportService.ImportProgressSnapshot CurrentImportProgress { get; set; }

    public AssetVisualRegistry VisualRegistry => _visualRegistry;

    public override Vector2 DefaultPos => new(8.0f, 560.0f);

    public override Vector2 DefaultSize => new(280.0f, 300.0f);

    public ProjectPanel(string? rootDirectory = null, AssetVisualRegistry? visualRegistry = null)
        : base("Project")
    {
        _visualRegistry = visualRegistry ?? AssetVisualRegistry.CreateDefault();
        _rootDirectory = ResolveAssetRoot(rootDirectory);
        _currentDirectory = _rootDirectory;
        _expandedDirectories.Add(_rootDirectory);
    }

    public string CurrentDirectory => _currentDirectory;

    public void BindAssetActions(Func<string, bool> hasBaker, Action<string> onOpenAsset, Action<string> onOpenImportSettings, Action<string> onReimportAsset)
    {
        _hasBaker = hasBaker;
        _onOpenAsset = onOpenAsset;
        _onOpenImportSettings = onOpenImportSettings;
        _onReimportAsset = onReimportAsset;
    }

    public void SelectAndRevealAsset(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return;
        }

        var canonical = Path.GetFullPath(fullPath);
        var dir = Path.GetDirectoryName(canonical);
        if (!string.IsNullOrEmpty(dir) && dir.StartsWith(_rootDirectory, StringComparison.OrdinalIgnoreCase) && Directory.Exists(dir))
        {
            _currentDirectory = dir;
            ExpandDirectoryAncestors(dir);
            _dirty = true;
        }

        _selectedPath = canonical;
        RequestFocus();
    }

    public void SetRootDirectory(string rootDirectory)
    {
        _rootDirectory = ResolveAssetRoot(rootDirectory);
        _currentDirectory = _rootDirectory;
        _selectedPath = string.Empty;
        _expandedDirectories.Clear();
        _expandedDirectories.Add(_rootDirectory);
        _dirty = true;
    }

    public void MarkDirty()
    {
        _dirty = true;
    }

    protected override unsafe void OnDraw(ImGuiContextManager contextManager)
    {
        ref readonly var tokens = ref contextManager.Tokens;
        var importProgress = CurrentImportProgress;

        DrawToolbar(in tokens);
        ImGui.Separator();

        if (_dirty)
        {
            RefreshCurrentDirectory();
            _dirty = false;
        }

        var avail = ImGui.GetContentRegionAvail();
        var footerHeight = importProgress.IsImporting ? ImGui.GetFrameHeightWithSpacing() + 4.0f : 0.0f;
        var contentHeight = MathF.Max(40.0f, avail.Y - footerHeight);
        var treeWidth = Math.Clamp(avail.X * 0.34f, 120.0f, 240.0f);
        var indentWidth = 14.0f * contextManager.DpiScale;

        ImGui.BeginChild("##ProjectFolderTree", new Vector2(treeWidth, contentHeight), ImGuiChildFlags.Borders);
        DrawFolderNode(_rootDirectory, "Assets", 0, indentWidth, in tokens);
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("##ProjectAssetList", new Vector2(0.0f, contentHeight), ImGuiChildFlags.None);
        DrawAssetItems(in tokens);
        if (_filteredItems.Count == 0)
        {
            ImGui.TextColored(tokens.TextDisabled, "Empty folder (drop files anywhere to import)");
        }
        ImGui.EndChild();

        if (importProgress.IsImporting)
        {
            ImGui.Separator();
            var fraction = importProgress.TotalFiles > 0 ? Math.Clamp((float)importProgress.CompletedFiles / importProgress.TotalFiles, 0.0f, 1.0f) : 0.0f;
            var overlay = string.IsNullOrEmpty(importProgress.CurrentItemName)
                ? $"Importing ({importProgress.CompletedFiles}/{importProgress.TotalFiles})..."
                : importProgress.CurrentItemName;
            ImGui.ProgressBar(fraction, new Vector2(-1.0f, 0.0f), overlay);
        }
    }

    private void DrawAssetItems(in EditorColorTokens tokens)
    {
        // _viewMode currently renders List view with Nerd Font icons; Grid view can switch here to use visual.PreviewTexture.
        var rowHeight = ImGui.GetFrameHeight();
        var clipper = ImGui.ImGuiListClipper();
        clipper.Begin(_filteredItems.Count, rowHeight);

        while (clipper.Step())
        {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                var item = _filteredItems[i];
                var visual = _visualRegistry.Resolve(item.FullPath, item.Extension, item.IsDirectory, _viewMode, in tokens);
                var selected = string.Equals(_selectedPath, item.FullPath, StringComparison.OrdinalIgnoreCase);

                ImGui.PushID(item.FullPath);
                if (ImGuiWidgets.IconSelectableRow("asset", item.Name, visual.IconGlyph, visual.IconColor, selected, in tokens, out var doubleClicked))
                {
                    _selectedPath = item.FullPath;
                    if (doubleClicked)
                    {
                        if (item.IsDirectory)
                        {
                            _currentDirectory = item.FullPath;
                            ExpandDirectoryAncestors(item.FullPath);
                            _dirty = true;
                        }
                        else
                        {
                            _onOpenAsset?.Invoke(item.FullPath);
                        }
                    }
                }

                if (!item.IsDirectory && ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                {
                    EditorDragDrop.SetStringPayload(EditorDragDrop.ASSET_PAYLOAD, item.FullPath);
                    ImGui.TextUnformatted($"{visual.IconGlyph}  {item.Name}");
                    ImGui.EndDragDropSource();
                }

                DrawItemContextMenu(in item);
                ImGui.PopID();
            }
        }

        clipper.Destroy();
    }

    private void DrawToolbar(in EditorColorTokens tokens)
    {
        var atRoot = string.Equals(_currentDirectory, _rootDirectory, StringComparison.OrdinalIgnoreCase);
        ImGui.BeginDisabled(atRoot);
        if (ImGui.Button($"{EditorIcons.ARROW_UP}##ProjectUpBtn"))
        {
            var parent = Directory.GetParent(_currentDirectory)?.FullName;
            if (!string.IsNullOrEmpty(parent) && parent.Length >= _rootDirectory.Length)
            {
                _currentDirectory = parent;
                _dirty = true;
            }
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip("Parent Folder");
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button($"{EditorIcons.REFRESH}##ProjectRefreshBtn"))
        {
            _dirty = true;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Refresh");
        }

        ImGui.SameLine();
        if (ImGuiWidgets.ToggleButton(".meta", _showMetaFiles, in tokens))
        {
            _showMetaFiles = !_showMetaFiles;
            _dirty = true;
        }

        ImGui.SameLine();
        if (ImGuiWidgets.SearchField("##ProjectSearch", "Search assets...", ref _searchQuery, -1.0f, in tokens))
        {
            RebuildFilter();
        }
    }

    private void DrawFolderNode(string directoryPath, string label, int depth, float indentWidth, in EditorColorTokens tokens)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        var subDirs = Directory.GetDirectories(directoryPath);
        var hasChildren = subDirs.Length > 0;
        var isExpanded = hasChildren && _expandedDirectories.Contains(directoryPath);
        var isSelected = string.Equals(_currentDirectory, directoryPath, StringComparison.OrdinalIgnoreCase);
        var visual = _visualRegistry.Resolve(directoryPath, string.Empty, true, AssetViewMode.List, in tokens, isExpanded);

        ImGui.PushID(directoryPath);
        var result = ImGuiWidgets.TreeRow("dir", label, depth, indentWidth, hasChildren, isExpanded, isSelected, visual.IconGlyph, visual.IconColor, in tokens);

        if (result.ToggledExpand || (result.DoubleClicked && hasChildren))
        {
            if (isExpanded)
            {
                _expandedDirectories.Remove(directoryPath);
            }
            else
            {
                _expandedDirectories.Add(directoryPath);
            }
        }

        if (result.Clicked)
        {
            _currentDirectory = directoryPath;
            _dirty = true;
        }
        ImGui.PopID();

        if (isExpanded)
        {
            Array.Sort(subDirs, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < subDirs.Length; i++)
            {
                var sub = subDirs[i];
                DrawFolderNode(sub, Path.GetFileName(sub), depth + 1, indentWidth, in tokens);
            }
        }
    }

    private void ExpandDirectoryAncestors(string directoryPath)
    {
        var current = Path.GetFullPath(directoryPath);
        while (!string.IsNullOrEmpty(current) && current.StartsWith(_rootDirectory, StringComparison.OrdinalIgnoreCase))
        {
            _expandedDirectories.Add(current);
            if (string.Equals(current, _rootDirectory, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            current = Directory.GetParent(current)?.FullName;
        }
    }

    private void RefreshCurrentDirectory()
    {
        _currentItems.Clear();

        if (!Directory.Exists(_currentDirectory))
        {
            RebuildFilter();
            return;
        }

        var dirs = Directory.GetDirectories(_currentDirectory);
        Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < dirs.Length; i++)
        {
            var dir = dirs[i];
            _currentItems.Add(new AssetEntryInfo(Path.GetFileName(dir), dir, true, string.Empty, 0));
        }

        var files = Directory.GetFiles(_currentDirectory);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < files.Length; i++)
        {
            var file = files[i];
            var ext = Path.GetExtension(file);
            if (!_showMetaFiles && string.Equals(ext, ".meta", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var length = new FileInfo(file).Length;
            _currentItems.Add(new AssetEntryInfo(Path.GetFileName(file), file, false, ext, length));
        }

        RebuildFilter();
    }

    private void RebuildFilter()
    {
        _filteredItems.Clear();
        for (var i = 0; i < _currentItems.Count; i++)
        {
            var item = _currentItems[i];
            if (string.IsNullOrWhiteSpace(_searchQuery) || item.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                _filteredItems.Add(item);
            }
        }
    }

    private static string ResolveAssetRoot(string? customRoot)
    {
        if (!string.IsNullOrWhiteSpace(customRoot) && Directory.Exists(customRoot))
        {
            return Path.GetFullPath(customRoot);
        }

        var builtIn = Path.Combine(AppContext.BaseDirectory, "BuiltInAssets");
        var sourceAssets = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Editor", "Ghost.Studio", "Assets"));
        if (Directory.Exists(sourceAssets))
        {
            return sourceAssets;
        }

        return Directory.Exists(builtIn) ? builtIn : AppContext.BaseDirectory;
    }

    private void DrawItemContextMenu(in AssetEntryInfo item)
    {
        if (!ImGui.BeginPopupContextItem())
        {
            return;
        }

        _selectedPath = item.FullPath;

        if (item.IsDirectory)
        {
            if (ImGui.MenuItem("Open"))
            {
                _currentDirectory = item.FullPath;
                _dirty = true;
            }

            if (ImGui.MenuItem("Refresh"))
            {
                _dirty = true;
            }
        }
        else
        {
            var hasBaker = _hasBaker?.Invoke(item.Extension) ?? false;

            if (ImGui.MenuItem("Import Settings...", string.Empty, false, hasBaker))
            {
                _onOpenImportSettings?.Invoke(item.FullPath);
            }

            if (!hasBaker && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip($"No baker registered for '{item.Extension}'.");
            }

            if (ImGui.MenuItem("Reimport", string.Empty, false, hasBaker))
            {
                _onReimportAsset?.Invoke(item.FullPath);
            }

            if (!hasBaker && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip($"No baker registered for '{item.Extension}'.");
            }
        }

        if (OperatingSystem.IsWindows())
        {
            ImGui.Separator();
            if (ImGui.MenuItem("Show in Explorer"))
            {
                RevealInExplorer(item.FullPath);
            }
        }

        ImGui.EndPopup();
    }

    private static void RevealInExplorer(string fullPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{fullPath}\"",
                UseShellExecute = true,
            });
        }
        catch
        {
        }
    }
}
