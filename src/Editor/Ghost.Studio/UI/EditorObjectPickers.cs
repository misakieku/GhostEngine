using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Editor.Core.Sync;
using Ghost.Studio.Backend;
using Ghost.Studio.Services;
using Hexa.NET.ImGui;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Shared Dear ImGui object/asset/entity picker fields with searchable popups, ping/open actions,
/// clear buttons, and drag-and-drop targets from <see cref="ProjectPanel"/> and <see cref="HierarchyPanel"/>.
/// </summary>
internal static unsafe class EditorObjectPickers
{
    private static readonly List<EditorAssetCatalog.Entry> s_scratchAssets = new();
    private static readonly List<EditorAssetCatalog.Entry> s_filteredAssets = new();
    private static string s_assetSearchQuery = string.Empty;
    private static string s_entitySearchQuery = string.Empty;

    public static bool DrawAssetField(string id, Guid currentGuid, AssetType filterType, EditorAssetCatalog catalog, ProjectService projectService,
        in EditorColorTokens tokens, out Guid nextGuid, Action<EditorAssetCatalog.Entry>? onPingOrOpen = null, bool allowClear = true, float reservedRightWidth = 0.0f)
    {
        nextGuid = currentGuid;
        var changed = false;

        ImGui.PushID(id);
        EditorAssetCatalog.Entry entry = default;
        var hasEntry = currentGuid != Guid.Empty && catalog.TryGetByGuid(currentGuid, out entry);
        var isBuiltInStandardLit = !hasEntry && filterType == AssetType.Shader && currentGuid == MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID;

        string displayLabel;
        string tooltipText;
        if (currentGuid == Guid.Empty)
        {
            displayLabel = $"None ({filterType})";
            tooltipText = "No asset assigned (drop asset here or click Pick)";
        }
        else if (hasEntry)
        {
            var tag = EditorAssetCatalog.GetTypeTag(entry.AssetType, entry.Extension);
            displayLabel = $"{tag} {entry.DisplayName}";
            tooltipText = $"{entry.VirtualPath}\n{currentGuid:D}";
        }
        else if (isBuiltInStandardLit)
        {
            displayLabel = "[SHD] Ghost/StandardLit";
            tooltipText = $"Built-in Ghost/StandardLit\n{currentGuid:D}";
        }
        else
        {
            displayLabel = $"[?] {currentGuid.ToString("D")[..8]}...";
            tooltipText = $"Unresolved GUID: {currentGuid:D}";
        }

        var style = ImGui.GetStyle();
        var pickText = "Pick";
        var pickWidth = ImGui.CalcTextSize(pickText).X + style.FramePadding.X * 2.0f;
        var clearWidth = allowClear && currentGuid != Guid.Empty ? ImGui.GetFrameHeight() + style.ItemSpacing.X : 0.0f;
        var availX = ImGui.GetContentRegionAvail().X - reservedRightWidth;
        var pillWidth = MathF.Max(48.0f, availX - pickWidth - clearWidth - style.ItemSpacing.X);

        ImGui.PushStyleColor(ImGuiCol.Text, currentGuid == Guid.Empty ? tokens.TextDisabled : tokens.TextPrimary);
        var pillClicked = ImGui.Button($"{displayLabel}##pill", new Vector2(pillWidth, 0.0f));
        ImGui.PopStyleColor();

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltipText);
        }

        if (pillClicked && hasEntry)
        {
            onPingOrOpen?.Invoke(entry);
        }

        if (ImGui.BeginDragDropTarget())
        {
            if (EditorDragDrop.TryAcceptStringPayload(EditorDragDrop.ASSET_PAYLOAD, out var droppedPath))
            {
                if (TryResolveDroppedAssetGuid(droppedPath, filterType, catalog, projectService, out var droppedGuid))
                {
                    nextGuid = droppedGuid;
                    changed = true;
                }
            }

            ImGui.EndDragDropTarget();
        }

        ImGui.SameLine();
        var popupId = $"##AssetPickPopup_{id}";
        if (ImGui.Button($"{pickText}##pickBtn"))
        {
            s_assetSearchQuery = string.Empty;
            ImGui.OpenPopup(popupId);
        }

        if (allowClear && currentGuid != Guid.Empty)
        {
            ImGui.SameLine();
            if (ImGui.Button("X##clrBtn", new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight())))
            {
                nextGuid = Guid.Empty;
                changed = true;
            }
        }

        if (ImGui.BeginPopup(popupId))
        {
            ImGuiWidgets.SearchField("##AssetPickSearch", $"Search {filterType}...", ref s_assetSearchQuery, 280.0f, in tokens);
            ImGui.Separator();

            if (allowClear && ImGui.Selectable($"None ({filterType})", currentGuid == Guid.Empty))
            {
                nextGuid = Guid.Empty;
                changed = true;
                ImGui.CloseCurrentPopup();
            }

            if (filterType == AssetType.Shader && !catalog.TryGetByGuid(MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID, out _))
            {
                if (string.IsNullOrWhiteSpace(s_assetSearchQuery) || "Ghost/StandardLit".Contains(s_assetSearchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    var isSel = currentGuid == MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID;
                    if (ImGui.Selectable("[SHD] Ghost/StandardLit (Built-in)", isSel))
                    {
                        nextGuid = MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID;
                        changed = true;
                        ImGui.CloseCurrentPopup();
                    }
                }
            }

            catalog.GetAssets(filterType, s_scratchAssets);
            s_filteredAssets.Clear();
            for (var i = 0; i < s_scratchAssets.Count; i++)
            {
                var item = s_scratchAssets[i];
                if (string.IsNullOrWhiteSpace(s_assetSearchQuery) || item.VirtualPath.Contains(s_assetSearchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    s_filteredAssets.Add(item);
                }
            }

            ImGui.BeginChild("##AssetPickList", new Vector2(300.0f, 240.0f), ImGuiChildFlags.None);
            var rowHeight = ImGui.GetFrameHeight();
            var clipper = ImGui.ImGuiListClipper();
            clipper.Begin(s_filteredAssets.Count, rowHeight);

            while (clipper.Step())
            {
                for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                {
                    var item = s_filteredAssets[i];
                    var tag = EditorAssetCatalog.GetTypeTag(item.AssetType, item.Extension);
                    var suffix = item.IsBuiltIn ? " [Built-in]" : string.Empty;
                    var isSelected = item.Guid == currentGuid;

                    ImGui.PushID(i);
                    if (ImGui.Selectable($"{tag} {item.VirtualPath}{suffix}", isSelected))
                    {
                        nextGuid = item.Guid;
                        changed = true;
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.PopID();
                }
            }

            clipper.Destroy();
            if (s_filteredAssets.Count == 0)
            {
                ImGui.TextColored(tokens.TextDisabled, "No matching assets");
            }
            ImGui.EndChild();
            ImGui.EndPopup();
        }

        ImGui.PopID();
        return changed;
    }

    public static bool DrawEntityField(string id, int currentNodeId, IReadOnlyList<EntityNode> entities, in EditorColorTokens tokens,
        out int nextNodeId, Action<EntityNode>? onSelectEntity = null, float reservedRightWidth = 0.0f)
    {
        nextNodeId = currentNodeId;
        var changed = false;

        ImGui.PushID(id);
        EntityNode? resolvedNode = null;
        if (currentNodeId > 0)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                if (entities[i].Id == currentNodeId)
                {
                    resolvedNode = entities[i];
                    break;
                }
            }
        }

        var label = currentNodeId <= 0
            ? "None (Entity)"
            : (resolvedNode != null ? $"{resolvedNode.Name} (#{resolvedNode.Id})" : $"Missing (#{currentNodeId})");

        var style = ImGui.GetStyle();
        var pickText = "Pick";
        var pickWidth = ImGui.CalcTextSize(pickText).X + style.FramePadding.X * 2.0f;
        var clearWidth = currentNodeId > 0 ? ImGui.GetFrameHeight() + style.ItemSpacing.X : 0.0f;
        var availX = ImGui.GetContentRegionAvail().X - reservedRightWidth;
        var pillWidth = MathF.Max(48.0f, availX - pickWidth - clearWidth - style.ItemSpacing.X);

        ImGui.PushStyleColor(ImGuiCol.Text, currentNodeId <= 0 ? tokens.TextDisabled : tokens.TextPrimary);
        var pillClicked = ImGui.Button($"{label}##entPill", new Vector2(pillWidth, 0.0f));
        ImGui.PopStyleColor();

        if (pillClicked && resolvedNode != null)
        {
            onSelectEntity?.Invoke(resolvedNode);
        }

        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload(EditorDragDrop.ENTITY_PAYLOAD);
            if (!payload.IsNull && payload.Data != null)
            {
                nextNodeId = *(int*)payload.Data;
                changed = true;
            }

            ImGui.EndDragDropTarget();
        }

        ImGui.SameLine();
        var popupId = $"##EntityPickPopup_{id}";
        if (ImGui.Button($"{pickText}##entPick"))
        {
            s_entitySearchQuery = string.Empty;
            ImGui.OpenPopup(popupId);
        }

        if (currentNodeId > 0)
        {
            ImGui.SameLine();
            if (ImGui.Button("X##entClr", new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight())))
            {
                nextNodeId = 0;
                changed = true;
            }
        }

        if (ImGui.BeginPopup(popupId))
        {
            ImGuiWidgets.SearchField("##EntityPickSearch", "Search entities...", ref s_entitySearchQuery, 240.0f, in tokens);
            ImGui.Separator();

            if (ImGui.Selectable("None (Entity)", currentNodeId <= 0))
            {
                nextNodeId = 0;
                changed = true;
                ImGui.CloseCurrentPopup();
            }

            ImGui.BeginChild("##EntityPickList", new Vector2(260.0f, 220.0f), ImGuiChildFlags.None);
            for (var i = 0; i < entities.Count; i++)
            {
                var node = entities[i];
                if (!string.IsNullOrWhiteSpace(s_entitySearchQuery) && !node.Name.Contains(s_entitySearchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (ImGui.Selectable($"{node.Name} (#{node.Id})", node.Id == currentNodeId))
                {
                    nextNodeId = node.Id;
                    changed = true;
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.EndChild();
            ImGui.EndPopup();
        }

        ImGui.PopID();
        return changed;
    }

    private static bool TryResolveDroppedAssetGuid(string droppedPath, AssetType filterType, EditorAssetCatalog catalog, ProjectService projectService, out Guid guid)
    {
        guid = Guid.Empty;
        if (string.IsNullOrWhiteSpace(droppedPath) || !File.Exists(droppedPath))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(droppedPath);
        if (catalog.TryGetBySourcePath(fullPath, out var entry))
        {
            if (filterType == AssetType.Unknown || entry.AssetType == filterType)
            {
                guid = entry.Guid;
                return guid != Guid.Empty;
            }

            // Dropping a .fbx/.obj prefab onto a Mesh field resolves its first baked Mesh sub-asset if present.
            if (filterType == AssetType.Mesh && entry.AssetType == AssetType.EntityPrefab)
            {
                catalog.GetAssets(AssetType.Mesh, s_scratchAssets);
                for (var i = 0; i < s_scratchAssets.Count; i++)
                {
                    if (string.Equals(s_scratchAssets[i].FullSourcePath, fullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        guid = s_scratchAssets[i].Guid;
                        return guid != Guid.Empty;
                    }
                }
            }

            return false;
        }

        var meta = projectService.GetOrCreateMetadata(fullPath);
        if (meta != null && (filterType == AssetType.Unknown || meta.Type == filterType))
        {
            guid = meta.Id;
            return guid != Guid.Empty;
        }

        return false;
    }
}
