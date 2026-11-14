using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Menus;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Studio.Backend;
using Ghost.Studio.Rendering;
using Hexa.NET.ImGui;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Virtualized scene tree panel supporting search filtering, expand/collapse, drag-drop reparenting, F2 inline rename, and dynamic context menus.
/// </summary>
internal sealed class HierarchyPanel : IDisposable
{
    private readonly struct FlatRow
    {
        public readonly EntityNode Node;
        public readonly int Depth;
        public readonly bool HasChildren;
        public readonly bool IsExpanded;

        public FlatRow(EntityNode node, int depth, bool hasChildren, bool isExpanded)
        {
            Node = node;
            Depth = depth;
            HasChildren = hasChildren;
            IsExpanded = isExpanded;
        }
    }

    private const string DRAG_PAYLOAD_TYPE = "GHOST_HIERARCHY_ENTITY";

    private readonly EditorViewportInputState _viewportInput;
    private readonly List<FlatRow> _visibleRows = new();
    private readonly HashSet<int> _expandedIds = new();
    private readonly Dictionary<int, List<EntityNode>> _childrenMap = new();

    private string _searchQuery = string.Empty;
    private long _builtSnapshotVersion = -1;
    private string _builtSearchQuery = string.Empty;
    private bool _rowsDirty = true;

    private int _renamingEntityId = -1;
    private string _renameBuffer = string.Empty;
    private bool _focusRenameInput;

    public HierarchyPanel(EditorViewportInputState viewportInput)
    {
        _viewportInput = viewportInput;
        EditorSelection.Instance.RenameRequested += OnRenameRequested;
    }

    public unsafe void Draw(ImGuiContextManager contextManager, ref bool isOpen)
    {
        ImGui.SetNextWindowPos(new Vector2(8.0f, 32.0f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(280.0f, 520.0f), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin("Hierarchy", ref isOpen))
        {
            ImGui.End();
            return;
        }

        ref readonly var tokens = ref contextManager.Tokens;
        var snapshot = EditorSyncBridge.Instance.LatestSnapshot;

        DrawToolbar(in tokens);
        ImGui.Separator();

        var version = EditorSyncBridge.Instance.SnapshotVersion;
        if (_rowsDirty || _builtSnapshotVersion != version || !string.Equals(_builtSearchQuery, _searchQuery, StringComparison.Ordinal))
        {
            RebuildVisibleRows(snapshot);
            _builtSnapshotVersion = version;
            _builtSearchQuery = _searchQuery;
            _rowsDirty = false;
        }

        if (_renamingEntityId < 0 && ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && ImGui.IsKeyPressed(ImGuiKey.F2) && EditorSelection.Instance.ActiveEntity != null)
        {
            StartRename(EditorSelection.Instance.ActiveEntity);
        }

        ImGui.BeginChild("##HierarchyTreeScroll", Vector2.Zero, ImGuiChildFlags.None);

        var rowHeight = ImGui.GetFrameHeight();
        var indentWidth = 14.0f * contextManager.DpiScale;
        var clipper = ImGui.ImGuiListClipper();
        clipper.Begin(_visibleRows.Count, rowHeight);

        while (clipper.Step())
        {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                DrawRow(_visibleRows[i], indentWidth, in tokens);
            }
        }

        clipper.Destroy();

        if (_visibleRows.Count == 0)
        {
            ImGui.TextColored(tokens.TextDisabled, string.IsNullOrEmpty(_searchQuery) ? "No entities in scene" : "No matching entities");
        }

        // Invisible drop target filling remaining empty space to unparent entities onto root.
        var remaining = ImGui.GetContentRegionAvail();
        if (remaining.Y > 4.0f)
        {
            ImGui.Dummy(new Vector2(MathF.Max(1.0f, remaining.X), remaining.Y));
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload(DRAG_PAYLOAD_TYPE);
                if (!payload.IsNull && payload.Data != null)
                {
                    var droppedId = *(int*)payload.Data;
                    var droppedNode = FindEntityById(snapshot, droppedId);
                    if (droppedNode != null && droppedNode.ParentId.HasValue)
                    {
                        CommandHistory.Instance.Execute(new ReparentEntityCommand(droppedNode, null));
                    }
                }

                ImGui.EndDragDropTarget();
            }
        }

        if (ImGui.BeginPopupContextWindow("##HierarchyBgContext", ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems))
        {
            var menu = EditorMenuRegistry.Default.BuildMenu("Hierarchy", new MenuContext(null));
            ImGuiMenuBuilder.DrawNodes(menu);
            ImGui.EndPopup();
        }

        ImGui.EndChild();
        ImGui.End();
    }

    private void DrawToolbar(in EditorColorTokens tokens)
    {
        if (ImGui.Button("+##CreateEntityBtn"))
        {
            ImGui.OpenPopup("##HierarchyCreatePopup");
        }

        if (ImGui.BeginPopup("##HierarchyCreatePopup"))
        {
            var ctx = new MenuContext(EditorSelection.Instance.ActiveEntity);
            var menu = EditorMenuRegistry.Default.BuildMenu("Hierarchy", ctx);
            ImGuiMenuBuilder.DrawNodes(menu);
            ImGui.EndPopup();
        }

        ImGui.SameLine();
        ImGuiWidgets.SearchField("##HierarchySearch", "Search entities...", ref _searchQuery, -1.0f, in tokens);
    }

    private unsafe void DrawRow(in FlatRow row, float indentWidth, in EditorColorTokens tokens)
    {
        var node = row.Node;
        ImGui.PushID(node.Id);

        if (row.Depth > 0)
        {
            ImGui.Indent(row.Depth * indentWidth);
        }

        if (row.HasChildren)
        {
            var arrowLabel = row.IsExpanded ? "v##exp" : ">##exp";
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.Border, Vector4.Zero);
            if (ImGui.SmallButton(arrowLabel))
            {
                if (row.IsExpanded)
                {
                    _expandedIds.Remove(node.Id);
                }
                else
                {
                    _expandedIds.Add(node.Id);
                }

                _rowsDirty = true;
            }
            ImGui.PopStyleColor(2);
            ImGui.SameLine();
        }
        else
        {
            ImGui.Dummy(new Vector2(ImGui.GetFrameHeight() * 0.72f, ImGui.GetFrameHeight()));
            ImGui.SameLine();
        }

        if (_renamingEntityId == node.Id)
        {
            if (_focusRenameInput)
            {
                ImGui.SetKeyboardFocusHere();
                _focusRenameInput = false;
            }

            ImGui.SetNextItemWidth(-1.0f);
            var submitted = ImGui.InputText("##RenameInput", ref _renameBuffer, 128, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll);
            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                _renamingEntityId = -1;
            }
            else if (submitted || (!ImGui.IsItemActive() && !ImGui.IsItemFocused() && ImGui.IsMouseClicked(ImGuiMouseButton.Left)))
            {
                CommitRename(node);
            }
        }
        else
        {
            var isSelected = EditorSelection.Instance.SelectedObjects.Contains(node) || ReferenceEquals(EditorSelection.Instance.ActiveEntity, node);
            var selectableFlags = ImGuiSelectableFlags.AllowDoubleClick;
            if (ImGui.Selectable(node.Name, isSelected, selectableFlags))
            {
                if (ImGui.GetIO().KeyCtrl)
                {
                    EditorSelection.Instance.ToggleSelection(node);
                }
                else
                {
                    EditorSelection.Instance.Select(node);
                }

                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    _viewportInput.FocusRequested = true;
                }
            }

            if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
            {
                var id = node.Id;
                ImGui.SetDragDropPayload(DRAG_PAYLOAD_TYPE, &id, sizeof(int));
                ImGui.TextUnformatted(node.Name);
                ImGui.EndDragDropSource();
            }

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload(DRAG_PAYLOAD_TYPE);
                if (!payload.IsNull && payload.Data != null)
                {
                    var droppedId = *(int*)payload.Data;
                    if (droppedId != node.Id)
                    {
                        var snapshot = EditorSyncBridge.Instance.LatestSnapshot;
                        var droppedNode = FindEntityById(snapshot, droppedId);
                        if (droppedNode != null)
                        {
                            _expandedIds.Add(node.Id);
                            _rowsDirty = true;
                            CommandHistory.Instance.Execute(new ReparentEntityCommand(droppedNode, node.Id));
                        }
                    }
                }

                ImGui.EndDragDropTarget();
            }

            if (ImGui.BeginPopupContextItem("##EntityContextMenu"))
            {
                if (!isSelected)
                {
                    EditorSelection.Instance.Select(node);
                }

                var menu = EditorMenuRegistry.Default.BuildMenu("Hierarchy", new MenuContext(node));
                ImGuiMenuBuilder.DrawNodes(menu);
                ImGui.EndPopup();
            }
        }

        if (row.Depth > 0)
        {
            ImGui.Unindent(row.Depth * indentWidth);
        }

        ImGui.PopID();
    }

    private void RebuildVisibleRows(SceneSnapshot? snapshot)
    {
        _visibleRows.Clear();
        _childrenMap.Clear();

        if (snapshot == null || snapshot.Entities.Count == 0)
        {
            return;
        }

        var entities = snapshot.Entities;
        if (!string.IsNullOrWhiteSpace(_searchQuery))
        {
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    _visibleRows.Add(new FlatRow(entity, 0, false, false));
                }
            }

            return;
        }

        var validIds = new HashSet<int>(entities.Count);
        for (var i = 0; i < entities.Count; i++)
        {
            validIds.Add(entities[i].Id);
        }

        var roots = new List<EntityNode>();
        for (var i = 0; i < entities.Count; i++)
        {
            var e = entities[i];
            if (e.ParentId.HasValue && validIds.Contains(e.ParentId.Value))
            {
                if (!_childrenMap.TryGetValue(e.ParentId.Value, out var list))
                {
                    list = new List<EntityNode>();
                    _childrenMap[e.ParentId.Value] = list;
                }

                list.Add(e);
            }
            else
            {
                roots.Add(e);
            }
        }

        for (var i = 0; i < roots.Count; i++)
        {
            AppendNodeRecursive(roots[i], 0);
        }
    }

    private void AppendNodeRecursive(EntityNode node, int depth)
    {
        var hasChildren = _childrenMap.TryGetValue(node.Id, out var children) && children.Count > 0;
        var expanded = hasChildren && _expandedIds.Contains(node.Id);
        _visibleRows.Add(new FlatRow(node, depth, hasChildren, expanded));

        if (!expanded || children == null)
        {
            return;
        }

        for (var i = 0; i < children.Count; i++)
        {
            AppendNodeRecursive(children[i], depth + 1);
        }
    }

    private void OnRenameRequested(EntityNode entity)
    {
        StartRename(entity);
    }

    private void StartRename(EntityNode entity)
    {
        _renamingEntityId = entity.Id;
        _renameBuffer = entity.Name;
        _focusRenameInput = true;
    }

    private void CommitRename(EntityNode entity)
    {
        var trimmed = _renameBuffer.Trim();
        _renamingEntityId = -1;
        if (!string.IsNullOrEmpty(trimmed) && !string.Equals(trimmed, entity.Name, StringComparison.Ordinal))
        {
            CommandHistory.Instance.Execute(new RenameEntityCommand(entity, trimmed));
        }
    }

    private static EntityNode? FindEntityById(SceneSnapshot? snapshot, int id)
    {
        if (snapshot == null)
        {
            return null;
        }

        for (var i = 0; i < snapshot.Entities.Count; i++)
        {
            if (snapshot.Entities[i].Id == id)
            {
                return snapshot.Entities[i];
            }
        }

        return null;
    }

    public void Dispose()
    {
        EditorSelection.Instance.RenameRequested -= OnRenameRequested;
    }
}
