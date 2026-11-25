using Ghost.AssetForge.Core.Models;
using Ghost.Core;
using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Menus;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Studio.Assets;
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
        public readonly HierarchyNode Node;
        public readonly int Depth;
        public readonly bool HasChildren;
        public readonly bool IsExpanded;

        public FlatRow(HierarchyNode node, int depth, bool hasChildren, bool isExpanded)
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
    private readonly Action<string>? _onOpenSceneAdditive;
    private readonly Action<SceneNode>? _onSetActiveScene;
    private readonly Action<SceneNode, bool>? _onSaveScene;
    private readonly Action<SceneNode>? _onUnloadScene;
    private readonly List<FlatRow> _visibleRows = new();
    private readonly HashSet<int> _expandedIds = new();
    private readonly HashSet<int> _seenSceneIds = new();
    private readonly Dictionary<int, List<HierarchyNode>> _childrenMap = new();

    private string _searchQuery = string.Empty;
    private long _builtRevision = -1;
    private string _builtSearchQuery = string.Empty;
    private bool _rowsDirty = true;
    private int _sceneCount = 1;

    private int _renamingEntityId = -1;
    private string _renameBuffer = string.Empty;
    private bool _focusRenameInput;

    public override Vector2 DefaultPos => new(8.0f, 32.0f);

    public override Vector2 DefaultSize => new(280.0f, 520.0f);

    public HierarchyPanel(EditorViewportInputState viewportInput, EditorAssetCatalog? assetCatalog = null, Action<string>? onSelectAssetInProject = null, Action<string>? onOpenSceneAdditive = null, Action<SceneNode>? onSetActiveScene = null, Action<SceneNode, bool>? onSaveScene = null, Action<SceneNode>? onUnloadScene = null)
        : base("Hierarchy")
    {
        _viewportInput = viewportInput;
        _assetCatalog = assetCatalog;
        _onSelectAssetInProject = onSelectAssetInProject;
        _onOpenSceneAdditive = onOpenSceneAdditive;
        _onSetActiveScene = onSetActiveScene;
        _onSaveScene = onSaveScene;
        _onUnloadScene = onUnloadScene;
        EditorSelection.Instance.RenameRequested += OnRenameRequested;
    }

    protected override unsafe void OnDraw(ImGuiContextManager contextManager)
    {

        ref readonly var tokens = ref contextManager.Tokens;
        var bridge = EditorSyncBridge.Instance;

        DrawToolbar(in tokens);
        ImGui.Separator();

        var revision = bridge.Revision;
        if (_rowsDirty || _builtRevision != revision || !string.Equals(_builtSearchQuery, _searchQuery, StringComparison.Ordinal))
        {
            RebuildVisibleRows(bridge);
            _builtRevision = revision;
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
            AcceptRootDropTarget(bridge);
        }

        if (ImGui.BeginPopupContextWindow("##HierarchyBgContext", ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems))
        {
            var menu = EditorMenuRegistry.Default.BuildMenu("Hierarchy", new MenuContext(null));
            ImGuiMenuBuilder.DrawNodes(menu);
            ImGui.EndPopup();
        }

        ImGui.EndChild();

        // Child window item itself also acts as root drop target when tree rows fill the scroll view.
        AcceptRootDropTarget(bridge);
    }

    private unsafe void AcceptRootDropTarget(EditorSyncBridge bridge)
    {
        if (!ImGui.BeginDragDropTarget())
        {
            return;
        }

        var payload = ImGui.AcceptDragDropPayload(EditorDragDrop.ENTITY_PAYLOAD);
        if (!payload.IsNull && payload.Data != null)
        {
            var droppedId = *(int*)payload.Data;
            var droppedNode = bridge.FindEntityNode(droppedId);
            if (droppedNode != null && droppedNode.ParentId.HasValue)
            {
                CommandHistory.Instance.Execute(new ReparentEntityCommand(droppedNode, null));
            }
        }

        TryAcceptPrefabDrop(null);
        ImGui.EndDragDropTarget();
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

        if (_renamingEntityId == node.Id && node is EntityNode renameTarget)
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
                CommitRename(renameTarget);
            }
        }
        else
        {
            var isSelected = EditorSelection.Instance.SelectedObjects.Contains(node) || ReferenceEquals(EditorSelection.Instance.ActiveObject, node);
            var visual = HierarchyVisualRegistry.Resolve(node, in tokens);
            var displayLabel = node is SceneNode sceneNode
                ? $"{sceneNode.Name}{(sceneNode.IsDirty ? "*" : string.Empty)}{(_sceneCount > 1 && sceneNode.SceneId == EditorSyncBridge.Instance.ActiveSceneId ? " (Active)" : string.Empty)}"
                : node.Name;
            var rowResult = ImGuiWidgets.TreeRow("row", displayLabel, row.Depth, indentWidth, row.HasChildren, row.IsExpanded, isSelected, visual.IconGlyph, visual.IconColor, in tokens, visual.LabelColor);

            if (rowResult.ToggledExpand)
            {
                ToggleExpand(node.Id, row.IsExpanded);
            }

            if (rowResult.Clicked)
            {
                if (ImGui.GetIO().KeyCtrl && node is EntityNode)
                {
                    EditorSelection.Instance.ToggleSelection(node);
                }
                else
                {
                    EditorSelection.Instance.Select(node);
                }

                if (rowResult.DoubleClicked)
                {
                    if (node is EntityNode)
                    {
                        _viewportInput.FocusRequested = true;
                    }
                    else if (row.HasChildren)
                    {
                        ToggleExpand(node.Id, row.IsExpanded);
                    }
                }
            }

            if (node is EntityNode entityNode)
            {
                if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                {
                    var id = entityNode.Id;
                    ImGui.SetDragDropPayload(EditorDragDrop.ENTITY_PAYLOAD, &id, sizeof(int));
                    ImGui.TextUnformatted($"{visual.IconGlyph}  {entityNode.Name}");
                    ImGui.EndDragDropSource();
                }

                if (ImGui.BeginDragDropTarget())
                {
                    var payload = ImGui.AcceptDragDropPayload(EditorDragDrop.ENTITY_PAYLOAD);
                    if (!payload.IsNull && payload.Data != null)
                    {
                        var droppedId = *(int*)payload.Data;
                        if (droppedId != entityNode.Id)
                        {
                            var droppedNode = EditorSyncBridge.Instance.FindEntityNode(droppedId);
                            if (droppedNode != null)
                            {
                                _expandedIds.Add(entityNode.Id);
                                _rowsDirty = true;
                                CommandHistory.Instance.Execute(new ReparentEntityCommand(droppedNode, entityNode.Id));
                            }
                        }
                    }

                    if (TryAcceptPrefabDrop(entityNode.Id))
                    {
                        _expandedIds.Add(entityNode.Id);
                        _rowsDirty = true;
                    }

                    ImGui.EndDragDropTarget();
                }

                if (ImGui.BeginPopupContextItem("##EntityContextMenu"))
                {
                    if (!isSelected)
                    {
                        EditorSelection.Instance.Select(entityNode);
                    }

                    var menu = EditorMenuRegistry.Default.BuildMenu("Hierarchy", new MenuContext(entityNode));
                    ImGuiMenuBuilder.DrawNodes(menu);

                    if (entityNode.PrefabLink != null)
                    {
                        ImGui.Separator();
                        if (ImGui.BeginMenu("Prefab"))
                        {
                            if (ImGui.MenuItem("Select Prefab Asset", string.Empty, false, _assetCatalog != null && _onSelectAssetInProject != null))
                            {
                                if (_assetCatalog != null && _assetCatalog.TryGetByGuid(entityNode.PrefabLink.PrefabAssetGuid, out var entry) && !string.IsNullOrEmpty(entry.FullSourcePath))
                                {
                                    _onSelectAssetInProject?.Invoke(entry.FullSourcePath);
                                }
                            }

                            if (ImGui.MenuItem("Unpack Prefab"))
                            {
                                CommandHistory.Instance.Execute(new UnpackPrefabCommand(entityNode.PrefabLink.InstanceGuid));
                            }

                            ImGui.EndMenu();
                        }
                    }

                    ImGui.EndPopup();
                }
            }
            else if (node is SceneNode targetSceneNode)
            {
                if (ImGui.BeginDragDropTarget())
                {
                    var payload = ImGui.AcceptDragDropPayload(EditorDragDrop.ENTITY_PAYLOAD);
                    if (!payload.IsNull && payload.Data != null)
                    {
                        var droppedId = *(int*)payload.Data;
                        var droppedNode = EditorSyncBridge.Instance.FindEntityNode(droppedId);
                        if (droppedNode != null && droppedNode.ParentId.HasValue)
                        {
                            CommandHistory.Instance.Execute(new ReparentEntityCommand(droppedNode, null));
                        }
                    }

                    if (TryAcceptPrefabDrop(null, targetSceneNode.SceneId))
                    {
                        _expandedIds.Add(node.Id);
                        _rowsDirty = true;
                    }

                    ImGui.EndDragDropTarget();
                }

                if (ImGui.BeginPopupContextItem("##SceneContextMenu"))
                {
                    if (!isSelected)
                    {
                        EditorSelection.Instance.Select(node);
                    }

                    if (_onSetActiveScene != null)
                    {
                        var isAlreadyActive = targetSceneNode.SceneId == EditorSyncBridge.Instance.ActiveSceneId;
                        if (ImGui.MenuItem("Set Active Scene", string.Empty, false, !isAlreadyActive))
                        {
                            _onSetActiveScene.Invoke(targetSceneNode);
                        }
                    }

                    if (_onSaveScene != null)
                    {
                        if (ImGui.MenuItem("Save Scene"))
                        {
                            _onSaveScene.Invoke(targetSceneNode, false);
                        }

                        if (ImGui.MenuItem("Save Scene As..."))
                        {
                            _onSaveScene.Invoke(targetSceneNode, true);
                        }
                    }

                    if (_onUnloadScene != null)
                    {
                        if (ImGui.MenuItem("Unload Scene"))
                        {
                            _onUnloadScene.Invoke(targetSceneNode);
                        }
                    }

                    ImGui.Separator();
                    var menu = EditorMenuRegistry.Default.BuildMenu("Hierarchy", new MenuContext(null));
                    ImGuiMenuBuilder.DrawNodes(menu);
                    ImGui.EndPopup();
                }
            }
        }

        ImGui.PopID();
    }

    private bool TryAcceptPrefabDrop(int? parentNodeId, ushort? targetSceneId = null)
    {
        if (!EditorDragDrop.TryAcceptStringPayload(EditorDragDrop.ASSET_PAYLOAD, out var droppedPath))
        {
            return false;
        }

        if (_assetCatalog != null && _assetCatalog.TryGetBySourcePath(droppedPath, out var entry))
        {
            if (entry.AssetType == AssetType.EntityPrefab)
            {
                var cmd = new InstantiatePrefabCommand(entry.Guid, Path.GetFileNameWithoutExtension(entry.DisplayName), float3.zero, quaternion.identity, float3.one, parentNodeId, targetSceneId: targetSceneId);
                CommandHistory.Instance.Execute(cmd);
                return true;
            }

            if (entry.AssetType == AssetType.Scene && _onOpenSceneAdditive != null)
            {
                _onOpenSceneAdditive.Invoke(droppedPath);
                return true;
            }

            return false;
        }

        if (_onOpenSceneAdditive != null && string.Equals(Path.GetExtension(droppedPath), SceneAssetDocument.EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            _onOpenSceneAdditive.Invoke(droppedPath);
            return true;
        }

        return false;
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

    private void RebuildVisibleRows(EditorSyncBridge bridge)
    {
        _visibleRows.Clear();
        _childrenMap.Clear();

        var scenes = bridge.GetSceneNodes();
        var entities = bridge.Entities;
        _sceneCount = scenes.Count;
        if (!string.IsNullOrWhiteSpace(_searchQuery))
        {
            for (var i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    _visibleRows.Add(new FlatRow(scenes[i], 0, false, false));
                }
            }

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

        var sceneById = new Dictionary<ushort, SceneNode>(scenes.Count);
        for (var i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            sceneById[scene.SceneId] = scene;
            if (_seenSceneIds.Add(scene.Id))
            {
                _expandedIds.Add(scene.Id);
            }
        }

        var roots = new List<HierarchyNode>();
        for (var i = 0; i < scenes.Count; i++)
        {
            roots.Add(scenes[i]);
        }

        for (var i = 0; i < entities.Count; i++)
        {
            var e = entities[i];
            if (e.ParentId.HasValue && validIds.Contains(e.ParentId.Value))
            {
                AddChild(e.ParentId.Value, e);
            }
            else if (sceneById.TryGetValue(e.SceneId, out var ownerScene))
            {
                AddChild(ownerScene.Id, e);
            }
            else if (scenes.Count > 0)
            {
                AddChild(scenes[0].Id, e);
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

    private void AddChild(int parentId, HierarchyNode child)
    {
        if (!_childrenMap.TryGetValue(parentId, out var list))
        {
            list = new List<HierarchyNode>();
            _childrenMap[parentId] = list;
        }

        list.Add(child);
    }

    private void AppendNodeRecursive(HierarchyNode node, int depth)
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

    public override void Dispose()
    {
        EditorSelection.Instance.RenameRequested -= OnRenameRequested;
    }
}
