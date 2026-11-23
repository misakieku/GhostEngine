using Ghost.Core;
using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Menus;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Studio.Backend;
using Ghost.Studio.Rendering;
using Ghost.Studio.Services;
using Hexa.NET.ImGui;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Virtualized scene tree panel supporting search filtering, expand/collapse, drag-drop reparenting,
/// prefab linking/instantiation, F2 inline rename, and dynamic context menus.
/// </summary>
internal sealed class HierarchyPanel : EditorWindow
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

    private readonly EditorViewportInputState _viewportInput;
    private readonly EditorAssetCatalog? _assetCatalog;
    private readonly Action<string>? _onSelectAssetInProject;
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

    public override Vector2 DefaultPos => new(8.0f, 32.0f);

    public override Vector2 DefaultSize => new(280.0f, 520.0f);

    public HierarchyPanel(EditorViewportInputState viewportInput, EditorAssetCatalog? assetCatalog = null, Action<string>? onSelectAssetInProject = null)
        : base("Hierarchy")
    {
        _viewportInput = viewportInput;
        _assetCatalog = assetCatalog;
        _onSelectAssetInProject = onSelectAssetInProject;
        EditorSelection.Instance.RenameRequested += OnRenameRequested;
    }

    protected override unsafe void OnDraw(ImGuiContextManager contextManager)
    {

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
                var payload = ImGui.AcceptDragDropPayload(EditorDragDrop.ENTITY_PAYLOAD);
                if (!payload.IsNull && payload.Data != null)
                {
                    var droppedId = *(int*)payload.Data;
                    var droppedNode = FindEntityById(snapshot, droppedId);
                    if (droppedNode != null && droppedNode.ParentId.HasValue)
                    {
                        CommandHistory.Instance.Execute(new ReparentEntityCommand(droppedNode, null));
                    }
                }

                TryAcceptPrefabDrop(null);
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
    }

    private void DrawToolbar(in EditorColorTokens tokens)
    {
        if (ImGui.Button($"{EditorIcons.ADD}##CreateEntityBtn"))
        {
            ImGui.OpenPopup("##HierarchyCreatePopup");
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Create Entity");
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

        if (_renamingEntityId == node.Id)
        {
            if (ImGuiWidgets.DrawTreeIndentAndChevron(row.Depth, indentWidth, row.HasChildren, row.IsExpanded, in tokens))
            {
                ToggleExpand(node.Id, row.IsExpanded);
            }

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
            var isPrefabLinked = node.PrefabLink != null;
            var iconColor = isPrefabLinked ? tokens.AccentHovered : tokens.TextSecondary;
            var labelColor = isPrefabLinked ? tokens.AccentHovered : (Vector4?)null;
            var rowResult = ImGuiWidgets.TreeRow("row", node.Name, row.Depth, indentWidth, row.HasChildren, row.IsExpanded, isSelected, EditorIcons.CUBE, iconColor, in tokens, labelColor);

            if (rowResult.ToggledExpand)
            {
                ToggleExpand(node.Id, row.IsExpanded);
            }

            if (rowResult.Clicked)
            {
                if (ImGui.GetIO().KeyCtrl)
                {
                    EditorSelection.Instance.ToggleSelection(node);
                }
                else
                {
                    EditorSelection.Instance.Select(node);
                }

                if (rowResult.DoubleClicked)
                {
                    _viewportInput.FocusRequested = true;
                }
            }

            if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
            {
                var id = node.Id;
                ImGui.SetDragDropPayload(EditorDragDrop.ENTITY_PAYLOAD, &id, sizeof(int));
                ImGui.TextUnformatted($"{EditorIcons.CUBE}  {node.Name}");
                ImGui.EndDragDropSource();
            }

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload(EditorDragDrop.ENTITY_PAYLOAD);
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

                if (TryAcceptPrefabDrop(node.Id))
                {
                    _expandedIds.Add(node.Id);
                    _rowsDirty = true;
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

                if (node.PrefabLink != null)
                {
                    ImGui.Separator();
                    if (ImGui.BeginMenu("Prefab"))
                    {
                        if (ImGui.MenuItem("Select Prefab Asset", string.Empty, false, _assetCatalog != null && _onSelectAssetInProject != null))
                        {
                            if (_assetCatalog != null && _assetCatalog.TryGetByGuid(node.PrefabLink.PrefabAssetGuid, out var entry) && !string.IsNullOrEmpty(entry.FullSourcePath))
                            {
                                _onSelectAssetInProject?.Invoke(entry.FullSourcePath);
                            }
                        }

                        if (ImGui.MenuItem("Unpack Prefab"))
                        {
                            CommandHistory.Instance.Execute(new UnpackPrefabCommand(node.PrefabLink.InstanceGuid));
                        }

                        ImGui.EndMenu();
                    }
                }

                ImGui.EndPopup();
            }
        }

        ImGui.PopID();
    }

    private bool TryAcceptPrefabDrop(int? parentNodeId)
    {
        if (_assetCatalog == null || !EditorDragDrop.TryAcceptStringPayload(EditorDragDrop.ASSET_PAYLOAD, out var droppedPath))
        {
            return false;
        }

        if (!_assetCatalog.TryGetBySourcePath(droppedPath, out var entry) || entry.AssetType != AssetType.EntityPrefab)
        {
            return false;
        }

        var cmd = new InstantiatePrefabCommand(entry.Guid, Path.GetFileNameWithoutExtension(entry.DisplayName), float3.zero, quaternion.identity, float3.one, parentNodeId);
        CommandHistory.Instance.Execute(cmd);
        return true;
    }

    private void ToggleExpand(int entityId, bool isExpanded)
    {
        if (isExpanded)
        {
            _expandedIds.Remove(entityId);
        }
        else
        {
            _expandedIds.Add(entityId);
        }

        _rowsDirty = true;
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

    public override void Dispose()
    {
        EditorSelection.Instance.RenameRequested -= OnRenameRequested;
    }
}
