using Ghost.Studio.Backend;
using Hexa.NET.ImGui;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Displays the workspace asset directory tree and virtualized asset grid/list browser.
/// </summary>
internal sealed class ProjectPanel
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

    private readonly string _rootDirectory;
    private readonly List<AssetEntryInfo> _currentItems = new();
    private readonly List<AssetEntryInfo> _filteredItems = new();

    private string _currentDirectory;
    private string _searchQuery = string.Empty;
    private string _selectedPath = string.Empty;
    private bool _showMetaFiles;
    private bool _dirty = true;

    public ProjectPanel(string? rootDirectory = null)
    {
        _rootDirectory = ResolveAssetRoot(rootDirectory);
        _currentDirectory = _rootDirectory;
    }

    public unsafe void Draw(ImGuiContextManager contextManager, ref bool isOpen)
    {
        ImGui.SetNextWindowPos(new Vector2(8.0f, 560.0f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(280.0f, 300.0f), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin("Project", ref isOpen))
        {
            ImGui.End();
            return;
        }

        ref readonly var tokens = ref contextManager.Tokens;

        DrawToolbar(in tokens);
        ImGui.Separator();

        if (_dirty)
        {
            RefreshCurrentDirectory();
            _dirty = false;
        }

        var availW = ImGui.GetContentRegionAvail().X;
        var treeWidth = Math.Clamp(availW * 0.34f, 120.0f, 240.0f);

        ImGui.BeginChild("##ProjectFolderTree", new Vector2(treeWidth, 0.0f), ImGuiChildFlags.Borders);
        DrawFolderNode(_rootDirectory, "Assets");
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("##ProjectAssetList", Vector2.Zero, ImGuiChildFlags.None);
        var rowHeight = ImGui.GetFrameHeight();
        var clipper = ImGui.ImGuiListClipper();
        clipper.Begin(_filteredItems.Count, rowHeight);

        while (clipper.Step())
        {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                var item = _filteredItems[i];
                var tag = item.IsDirectory ? "[DIR]" : GetExtensionTag(item.Extension);
                var selected = string.Equals(_selectedPath, item.FullPath, StringComparison.OrdinalIgnoreCase);

                ImGui.PushID(item.FullPath);
                if (ImGui.Selectable($"{tag}  {item.Name}", selected, ImGuiSelectableFlags.AllowDoubleClick))
                {
                    _selectedPath = item.FullPath;
                    if (item.IsDirectory && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        _currentDirectory = item.FullPath;
                        _dirty = true;
                    }
                }
                ImGui.PopID();
            }
        }

        clipper.Destroy();
        if (_filteredItems.Count == 0)
        {
            ImGui.TextColored(tokens.TextDisabled, "Empty folder");
        }
        ImGui.EndChild();

        ImGui.End();
    }

    private void DrawToolbar(in EditorColorTokens tokens)
    {
        var atRoot = string.Equals(_currentDirectory, _rootDirectory, StringComparison.OrdinalIgnoreCase);
        ImGui.BeginDisabled(atRoot);
        if (ImGui.Button("Up"))
        {
            var parent = Directory.GetParent(_currentDirectory)?.FullName;
            if (!string.IsNullOrEmpty(parent) && parent.Length >= _rootDirectory.Length)
            {
                _currentDirectory = parent;
                _dirty = true;
            }
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Refresh"))
        {
            _dirty = true;
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

    private void DrawFolderNode(string directoryPath, string label)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        var subDirs = Directory.GetDirectories(directoryPath);
        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
        if (subDirs.Length == 0)
        {
            flags |= ImGuiTreeNodeFlags.Leaf;
        }

        if (string.Equals(_currentDirectory, directoryPath, StringComparison.OrdinalIgnoreCase))
        {
            flags |= ImGuiTreeNodeFlags.Selected;
        }

        if (string.Equals(directoryPath, _rootDirectory, StringComparison.OrdinalIgnoreCase))
        {
            flags |= ImGuiTreeNodeFlags.DefaultOpen;
        }

        var opened = ImGui.TreeNodeEx($"{label}##{directoryPath}", flags);
        if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen())
        {
            _currentDirectory = directoryPath;
            _dirty = true;
        }

        if (opened)
        {
            for (var i = 0; i < subDirs.Length; i++)
            {
                var sub = subDirs[i];
                DrawFolderNode(sub, Path.GetFileName(sub));
            }

            ImGui.TreePop();
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

    private static string GetExtensionTag(string ext)
    {
        if (string.IsNullOrEmpty(ext))
        {
            return "[FIL]";
        }

        return ext.ToUpperInvariant() switch
        {
            ".GSHDR" or ".GCOMP" or ".GGRAPH" or ".HLSL" => "[SHD]",
            ".OBJ" or ".FBX" or ".GLTF" or ".GLB" => "[MSH]",
            ".PNG" or ".JPG" or ".TGA" or ".DDS" => "[TEX]",
            ".JSON" or ".XML" => "[CFG]",
            _ => "[FIL]",
        };
    }
}
