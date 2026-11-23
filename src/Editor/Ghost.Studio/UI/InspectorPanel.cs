using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Studio.Backend;
using Ghost.Studio.Services;
using Hexa.NET.ImGui;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Renders component property grids and component lifecycle controls for the active <see cref="EntityNode"/>.
/// </summary>
internal sealed partial class InspectorPanel : EditorWindow
{
    private readonly ProjectService _projectService;
    private readonly EditorAssetCatalog _assetCatalog;
    private readonly Action<string>? _onPingAsset;
    private readonly Action<string>? _onOpenAsset;

    private readonly Dictionary<PropertyNode, Vector3> _eulerCache = new();
    private PropertyNode? _activeEditProperty;
    private object? _editStartValue;
    private int _headerEntityId = -1;
    private string _headerNameBuffer = string.Empty;
    private string _addComponentSearch = string.Empty;

    public override Vector2 DefaultPos => new(1270.0f, 32.0f);

    public override Vector2 DefaultSize => new(322.0f, 820.0f);

    public InspectorPanel(ProjectService projectService, EditorAssetCatalog assetCatalog, Action<string>? onPingAsset = null, Action<string>? onOpenAsset = null)
        : base("Inspector")
    {
        _projectService = projectService;
        _assetCatalog = assetCatalog;
        _onPingAsset = onPingAsset;
        _onOpenAsset = onOpenAsset;
    }

    protected override void OnDraw(ImGuiContextManager contextManager)
    {
        ref readonly var tokens = ref contextManager.Tokens;
        var selection = EditorSelection.Instance;
        var entity = selection.ActiveEntity;

        if (entity == null)
        {
            ImGui.TextColored(tokens.TextDisabled, "Select an entity to inspect its properties.");
            return;
        }

        if (selection.SelectedObjects.Count > 1)
        {
            ImGui.TextColored(tokens.TextSecondary, $"Active of {selection.SelectedObjects.Count} selected entities");
        }

        DrawEntityHeader(contextManager, entity, in tokens);
        ImGui.Separator();

        ImGui.BeginChild("##InspectorScroll", new Vector2(0.0f, -38.0f * contextManager.DpiScale), ImGuiChildFlags.None);
        for (var i = 0; i < entity.Components.Count; i++)
        {
            DrawComponentCard(contextManager, entity, entity.Components[i], in tokens);
        }
        ImGui.EndChild();

        ImGui.Separator();
        DrawAddComponentFooter(entity, in tokens);
    }

    private void DrawEntityHeader(ImGuiContextManager contextManager, EntityNode entity, in EditorColorTokens tokens)
    {
        if (_headerEntityId != entity.Id)
        {
            _headerEntityId = entity.Id;
            _headerNameBuffer = entity.Name;
            _eulerCache.Clear();
        }
        else if (!ImGui.IsAnyItemActive() && !string.Equals(_headerNameBuffer, entity.Name, StringComparison.Ordinal))
        {
            _headerNameBuffer = entity.Name;
        }

        ImGui.PushFont(contextManager.HeadingFont, EditorTheme.FONT_SIZE_HEADING);
        ImGui.SetNextItemWidth(-76.0f * contextManager.DpiScale);
        var enterPressed = ImGui.InputText("##EntityHeaderName", ref _headerNameBuffer, 128, ImGuiInputTextFlags.EnterReturnsTrue);
        var committed = enterPressed || ImGui.IsItemDeactivatedAfterEdit();
        ImGui.PopFont();

        if (committed)
        {
            var trimmed = _headerNameBuffer.Trim();
            if (!string.IsNullOrEmpty(trimmed) && !string.Equals(trimmed, entity.Name, StringComparison.Ordinal))
            {
                CommandHistory.Instance.Execute(new RenameEntityCommand(entity, trimmed));
            }
        }

        ImGui.SameLine();
        ImGui.PushFont(contextManager.MonospaceFont, EditorTheme.FONT_SIZE_MONO);
        var idText = entity.EcsEntity.IsValid ? $"#{entity.EcsEntity.ID}" : $"#{entity.Id}";
        ImGui.TextColored(tokens.TextSecondary, idText);
        ImGui.PopFont();

        if (entity.PrefabLink != null)
        {
            DrawPrefabLinkRow(contextManager, entity.PrefabLink, in tokens);
        }
    }

    private void DrawPrefabLinkRow(ImGuiContextManager contextManager, PrefabLinkInfo prefabLink, in EditorColorTokens tokens)
    {
        var prefabGuid = prefabLink.PrefabAssetGuid;
        EditorAssetCatalog.Entry entry = default;
        var hasEntry = prefabGuid != Guid.Empty && _assetCatalog.TryGetByGuid(prefabGuid, out entry);
        var hasSourcePath = hasEntry && !string.IsNullOrEmpty(entry.FullSourcePath);

        string displayLabel;
        string tooltipText;
        if (hasEntry)
        {
            displayLabel = $"[PFB] {entry.DisplayName}";
            tooltipText = $"{entry.VirtualPath}\nPrefab GUID: {prefabGuid:D}\nInstance GUID: {prefabLink.InstanceGuid:D} (Node #{prefabLink.TemplateEntityIndex})";
        }
        else
        {
            var shortGuid = prefabGuid.ToString("D")[..8];
            displayLabel = $"[PFB] {shortGuid}...";
            tooltipText = $"Missing or unresolved Prefab GUID: {prefabGuid:D}\nInstance GUID: {prefabLink.InstanceGuid:D} (Node #{prefabLink.TemplateEntityIndex})";
        }

        var style = ImGui.GetStyle();
        var labelWidth = 54.0f * contextManager.DpiScale;
        var selectText = "Select";
        var selectWidth = hasSourcePath ? ImGui.CalcTextSize(selectText).X + style.FramePadding.X * 2.0f + style.ItemSpacing.X : 0.0f;
        var availX = ImGui.GetContentRegionAvail().X;
        var pillWidth = MathF.Max(60.0f, availX - labelWidth - selectWidth - style.ItemSpacing.X);

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(tokens.TextSecondary, "Prefab");
        ImGui.SameLine(ImGui.GetCursorPosX() + labelWidth);

        ImGui.PushStyleColor(ImGuiCol.Text, hasEntry ? tokens.Accent : tokens.TextSecondary);
        var pillClicked = ImGui.Button($"{displayLabel}##PrefabLinkPill", new Vector2(pillWidth, 0.0f));
        ImGui.PopStyleColor();

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltipText);
        }

        if (pillClicked && hasSourcePath)
        {
            _onPingAsset?.Invoke(entry.FullSourcePath);
        }

        if (hasSourcePath)
        {
            ImGui.SameLine();
            if (ImGui.Button($"{selectText}##SelectPrefabAsset"))
            {
                _onPingAsset?.Invoke(entry.FullSourcePath);
            }
        }
    }

    private void DrawComponentCard(ImGuiContextManager contextManager, EntityNode entity, ComponentNode component, in EditorColorTokens tokens)
    {
        ImGui.PushID(component.TypeName);

        var open = ImGui.CollapsingHeader(component.TypeName, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.AllowOverlap);

        var btnSize = ImGui.GetFrameHeight();
        var availMax = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
        ImGui.SameLine(MathF.Max(20.0f, availMax - btnSize));
        if (ImGui.SmallButton("X##RemoveComp"))
        {
            CommandHistory.Instance.Execute(new RemoveComponentCommand(entity, component));
            ImGui.PopID();
            return;
        }

        if (open)
        {
            if (component.Properties.Count == 1 && component.Properties[0] is VolumeOverridesPropertyNode volumeProp)
            {
                DrawVolumeOverridesSection(contextManager, entity, volumeProp, in tokens);
                ImGui.PopID();
                return;
            }

            var tableFlags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.PadOuterX;
            if (ImGui.BeginTable("##PropsTable", 2, tableFlags))
            {
                ImGui.TableSetupColumn("Property", ImGuiTableColumnFlags.WidthStretch, 0.42f);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch, 0.58f);

                string? lastGroup = null;
                for (var p = 0; p < component.Properties.Count; p++)
                {
                    var prop = component.Properties[p];
                    if (!string.IsNullOrEmpty(prop.GroupName) && !string.Equals(prop.GroupName, lastGroup, StringComparison.Ordinal))
                    {
                        lastGroup = prop.GroupName;
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.TextColored(tokens.TextSecondary, prop.GroupName);
                        ImGui.TableSetColumnIndex(1);
                    }

                    DrawPropertyRow(contextManager, prop, in tokens);
                }

                ImGui.EndTable();
            }
        }

        ImGui.PopID();
    }

    private void DrawPropertyRow(ImGuiContextManager contextManager, PropertyNode prop, in EditorColorTokens tokens)
    {
        ImGui.PushID(prop.Name);
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(prop.IsReadOnly ? tokens.TextDisabled : tokens.TextPrimary, prop.DisplayName);

        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(-1.0f);

        if (prop.IsReadOnly)
        {
            DrawReadOnlyValue(contextManager, prop.Value, in tokens);
            ImGui.PopID();
            return;
        }

        DrawEditableProperty(contextManager, prop, in tokens);
        ImGui.PopID();
    }

    private void DrawEditableProperty(ImGuiContextManager contextManager, PropertyNode prop, in EditorColorTokens tokens)
    {
        if (prop is AssetPropertyNode assetProp)
        {
            DrawAssetProperty(assetProp, in tokens);
            return;
        }

        if (prop is MaterialPalettePropertyNode paletteProp)
        {
            DrawMaterialPaletteProperty(paletteProp, in tokens);
            return;
        }

        if (prop is EntityRefPropertyNode entityProp)
        {
            DrawEntityRefProperty(entityProp, in tokens);
            return;
        }

        if (!DrawScalarOrVectorWidget(contextManager, prop, prop.PropertyType, prop.Value, prop.Name, in tokens, out var nextValue, out var isDiscrete))
        {
            DrawReadOnlyValue(contextManager, prop.Value, in tokens);
            return;
        }

        if (isDiscrete)
        {
            if (nextValue != null && !Equals(nextValue, prop.Value))
            {
                CommandHistory.Instance.Execute(new ModifyPropertyCommand(prop, prop.Value, nextValue));
            }

            return;
        }

        TrackContinuousEdit(prop, prop.Value, nextValue, !Equals(nextValue, prop.Value));
    }

    private bool DrawScalarOrVectorWidget(ImGuiContextManager contextManager, PropertyNode? eulerKey, Type type, object? val, string propName, in EditorColorTokens tokens, out object? nextValue, out bool isDiscrete)
    {
        nextValue = val;
        isDiscrete = false;

        if (type == typeof(bool) && val is bool bVal)
        {
            var current = bVal;
            if (ImGui.Checkbox("##val", ref current))
            {
                nextValue = current;
                isDiscrete = true;
            }

            return true;
        }

        if (type == typeof(int) && val is int iVal)
        {
            var current = iVal;
            if (ImGui.DragInt("##val", ref current, 1.0f))
            {
                nextValue = current;
            }

            return true;
        }

        if (type == typeof(uint) && val is uint uVal)
        {
            var current = (int)Math.Min(int.MaxValue, uVal);
            if (ImGui.DragInt("##val", ref current, 1.0f, 0, int.MaxValue))
            {
                nextValue = (uint)Math.Max(0, current);
            }

            return true;
        }

        if (type == typeof(float) && val is float fVal)
        {
            var current = fVal;
            if (ImGui.DragFloat("##val", ref current, 0.05f))
            {
                nextValue = current;
            }

            return true;
        }

        if (type == typeof(float2) && val is float2 f2Val)
        {
            var vec = new Vector2(f2Val.x, f2Val.y);
            if (ImGui.DragFloat2("##val", ref vec, 0.05f))
            {
                nextValue = new float2(vec.X, vec.Y);
            }

            return true;
        }

        if (type == typeof(Vector2) && val is Vector2 v2Val)
        {
            var vec = v2Val;
            if (ImGui.DragFloat2("##val", ref vec, 0.05f))
            {
                nextValue = vec;
            }

            return true;
        }

        if (type == typeof(float3) && val is float3 f3Val)
        {
            var vec = new Vector3(f3Val.x, f3Val.y, f3Val.z);
            var edited = IsColorProperty(propName)
                ? ImGui.ColorEdit3("##val", ref vec, ImGuiColorEditFlags.Float)
                : ImGui.DragFloat3("##val", ref vec, 0.05f);
            if (edited)
            {
                nextValue = new float3(vec.X, vec.Y, vec.Z);
            }

            return true;
        }

        if (type == typeof(Vector3) && val is Vector3 v3Val)
        {
            var vec = v3Val;
            var edited = IsColorProperty(propName)
                ? ImGui.ColorEdit3("##val", ref vec, ImGuiColorEditFlags.Float)
                : ImGui.DragFloat3("##val", ref vec, 0.05f);
            if (edited)
            {
                nextValue = vec;
            }

            return true;
        }

        if (type == typeof(float4) && val is float4 f4Val)
        {
            var vec = new Vector4(f4Val.x, f4Val.y, f4Val.z, f4Val.w);
            var edited = IsColorProperty(propName)
                ? ImGui.ColorEdit4("##val", ref vec, ImGuiColorEditFlags.Float)
                : ImGui.DragFloat4("##val", ref vec, 0.05f);
            if (edited)
            {
                nextValue = new float4(vec.X, vec.Y, vec.Z, vec.W);
            }

            return true;
        }

        if (type == typeof(Vector4) && val is Vector4 v4Val)
        {
            var vec = v4Val;
            var edited = IsColorProperty(propName)
                ? ImGui.ColorEdit4("##val", ref vec, ImGuiColorEditFlags.Float)
                : ImGui.DragFloat4("##val", ref vec, 0.05f);
            if (edited)
            {
                nextValue = vec;
            }

            return true;
        }

        if (type == typeof(quaternion) && val is quaternion qVal)
        {
            var isEditingThis = eulerKey != null && ReferenceEquals(_activeEditProperty, eulerKey);
            Vector3 eulerDeg;
            if (eulerKey == null || !isEditingThis || !_eulerCache.TryGetValue(eulerKey, out eulerDeg))
            {
                eulerDeg = QuaternionToEulerDegrees(qVal);
                if (eulerKey != null)
                {
                    _eulerCache[eulerKey] = eulerDeg;
                }
            }

            if (ImGui.DragFloat3("##val", ref eulerDeg, 0.5f))
            {
                if (eulerKey != null)
                {
                    _eulerCache[eulerKey] = eulerDeg;
                }

                nextValue = quaternion.EulerXYZ(math.radians(new float3(eulerDeg.X, eulerDeg.Y, eulerDeg.Z)));
            }

            return true;
        }

        if (type.IsEnum && val != null)
        {
            var names = Enum.GetNames(type);
            var values = Enum.GetValues(type);
            var currentIdx = 0;
            for (var i = 0; i < values.Length; i++)
            {
                if (Equals(values.GetValue(i), val))
                {
                    currentIdx = i;
                    break;
                }
            }

            if (ImGui.Combo("##val", ref currentIdx, names, names.Length))
            {
                nextValue = values.GetValue(currentIdx);
                isDiscrete = true;
            }

            return true;
        }

        if (type == typeof(string))
        {
            var str = val as string ?? string.Empty;
            if (ImGui.InputText("##val", ref str, 256))
            {
                nextValue = str;
            }

            return true;
        }

        return false;
    }

    private void TrackContinuousEdit(PropertyNode prop, object? currentBackingValue, object? nextValue, bool itemEdited)
    {
        if (ImGui.IsItemActivated())
        {
            _activeEditProperty = prop;
            _editStartValue = currentBackingValue;
        }

        if (itemEdited && nextValue != null)
        {
            prop.ApplyValue(nextValue);
        }

        if (ImGui.IsItemDeactivatedAfterEdit() && ReferenceEquals(_activeEditProperty, prop))
        {
            var startVal = _editStartValue;
            _activeEditProperty = null;
            _editStartValue = null;

            if (!Equals(startVal, prop.Value))
            {
                CommandHistory.Instance.Execute(new ModifyPropertyCommand(prop, startVal, prop.Value));
            }
        }
    }

    private void DrawAssetProperty(AssetPropertyNode assetProp, in EditorColorTokens tokens)
    {
        var filterType = assetProp.ExpectedAssetType;
        if (EditorObjectPickers.DrawAssetField(assetProp.Name, assetProp.AssetGuid, filterType, _assetCatalog, _projectService, in tokens,
            out var nextGuid, entry => PingAssetEntry(entry)))
        {
            if (nextGuid != assetProp.AssetGuid)
            {
                CommandHistory.Instance.Execute(new ModifyPropertyCommand(assetProp, assetProp.AssetGuid, nextGuid));
            }
        }
    }

    private void DrawMaterialPaletteProperty(MaterialPalettePropertyNode paletteProp, in EditorColorTokens tokens)
    {
        var guids = paletteProp.MaterialGuids;
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(tokens.TextSecondary, $"{guids.Count} slot(s)");
        ImGui.SameLine();
        if (ImGui.SmallButton("+ Slot##addMatSlot"))
        {
            var next = new Guid[guids.Count + 1];
            for (var i = 0; i < guids.Count; i++)
            {
                next[i] = guids[i];
            }

            CommandHistory.Instance.Execute(new ModifyPropertyCommand(paletteProp, guids.ToArray(), next));
            return;
        }

        var style = ImGui.GetStyle();
        var editBtnW = ImGui.CalcTextSize("Edit").X + style.FramePadding.X * 2.0f;
        var delBtnW = ImGui.GetFrameHeight();

        for (var i = 0; i < guids.Count; i++)
        {
            ImGui.PushID(i);
            var currentGuid = guids[i];
            var hasDiskPath = currentGuid != Guid.Empty && _assetCatalog.TryGetByGuid(currentGuid, out var entry) && !string.IsNullOrEmpty(entry.FullSourcePath);
            var reservedRight = editBtnW + delBtnW + style.ItemSpacing.X * 2.0f;

            var changed = EditorObjectPickers.DrawAssetField($"slot_{i}", currentGuid, AssetType.Material, _assetCatalog, _projectService, in tokens,
                out var nextGuid, e => PingAssetEntry(e), reservedRightWidth: reservedRight);

            if (changed && nextGuid != currentGuid)
            {
                var updated = guids.ToArray();
                updated[i] = nextGuid;
                CommandHistory.Instance.Execute(new ModifyPropertyCommand(paletteProp, guids.ToArray(), updated));
            }

            ImGui.SameLine();
            ImGui.BeginDisabled(!hasDiskPath);
            if (ImGui.Button($"Edit##editSlot_{i}") && hasDiskPath && _assetCatalog.TryGetByGuid(currentGuid, out var openEntry))
            {
                _onOpenAsset?.Invoke(openEntry.FullSourcePath);
            }
            ImGui.EndDisabled();

            ImGui.SameLine();
            if (ImGui.Button($"X##delSlot_{i}", new Vector2(delBtnW, delBtnW)))
            {
                var shrunk = new Guid[guids.Count - 1];
                var writeIdx = 0;
                for (var s = 0; s < guids.Count; s++)
                {
                    if (s != i)
                    {
                        shrunk[writeIdx++] = guids[s];
                    }
                }

                CommandHistory.Instance.Execute(new ModifyPropertyCommand(paletteProp, guids.ToArray(), shrunk));
                ImGui.PopID();
                return;
            }

            ImGui.PopID();
        }
    }

    private void DrawEntityRefProperty(EntityRefPropertyNode entityProp, in EditorColorTokens tokens)
    {
        var snapshot = EditorSyncBridge.Instance.LatestSnapshot;
        if (EditorObjectPickers.DrawEntityField(entityProp.Name, entityProp.TargetNodeId, snapshot, in tokens, out var nextNodeId, node => EditorSelection.Instance.Select(node)))
        {
            if (nextNodeId != entityProp.TargetNodeId)
            {
                CommandHistory.Instance.Execute(new ModifyPropertyCommand(entityProp, entityProp.TargetNodeId, nextNodeId));
            }
        }
    }

    private void PingAssetEntry(EditorAssetCatalog.Entry entry)
    {
        if (!string.IsNullOrEmpty(entry.FullSourcePath))
        {
            _onPingAsset?.Invoke(entry.FullSourcePath);
        }
    }

    private void DrawAddComponentFooter(EntityNode entity, in EditorColorTokens tokens)
    {
        var availW = ImGui.GetContentRegionAvail().X;
        var btnW = MathF.Min(200.0f, availW);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0.0f, (availW - btnW) * 0.5f));

        if (ImGui.Button("Add Component", new Vector2(btnW, 0.0f)))
        {
            _addComponentSearch = string.Empty;
            ImGui.OpenPopup("##AddComponentPopup");
        }

        if (!ImGui.BeginPopup("##AddComponentPopup"))
        {
            return;
        }

        ImGuiWidgets.SearchField("##AddCompSearch", "Search components...", ref _addComponentSearch, 230.0f, in tokens);
        ImGui.Separator();

        ImGui.BeginChild("##AddCompList", new Vector2(230.0f, 220.0f), ImGuiChildFlags.None);
        var discovered = ComponentNodeFactory.DiscoverComponentTypes();
        for (var i = 0; i < discovered.Count; i++)
        {
            var compType = discovered[i];
            if (!string.IsNullOrWhiteSpace(_addComponentSearch) && !compType.Name.Contains(_addComponentSearch, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (HasComponentType(entity, compType))
            {
                continue;
            }

            if (compType == typeof(RenderVolumeDataContainer) && !HasComponentType(entity, typeof(RenderVolume)))
            {
                ImGui.BeginDisabled();
                ImGui.Selectable($"{compType.Name} (Requires RenderVolume)");
                ImGui.EndDisabled();
                continue;
            }

            if (ImGui.Selectable(compType.Name))
            {
                CommandHistory.Instance.Execute(new AddComponentCommand(entity, compType));
                ImGui.CloseCurrentPopup();
            }
        }
        ImGui.EndChild();
        ImGui.EndPopup();
    }

    private static void DrawReadOnlyValue(ImGuiContextManager contextManager, object? val, in EditorColorTokens tokens)
    {
        ImGui.PushFont(contextManager.MonospaceFont, EditorTheme.FONT_SIZE_MONO);
        string formatted;
        if (val == null)
        {
            formatted = "<null>";
        }
        else if (val is float4x4 m)
        {
            formatted = $"T({m.c3.x:0.00}, {m.c3.y:0.00}, {m.c3.z:0.00})";
        }
        else
        {
            formatted = val.ToString() ?? string.Empty;
        }

        ImGui.TextColored(tokens.TextSecondary, formatted);
        ImGui.PopFont();
    }

    private static bool HasComponentType(EntityNode entity, Type componentType)
    {
        for (var i = 0; i < entity.Components.Count; i++)
        {
            if (entity.Components[i].ComponentType == componentType)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsColorProperty(string propertyName)
    {
        return propertyName.Contains("color", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("tint", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("albedo", StringComparison.OrdinalIgnoreCase);
    }

    private static Vector3 QuaternionToEulerDegrees(quaternion q)
    {
        var m = new float3x3(q);
        float rx;
        float ry;
        float rz;

        var sy = Math.Clamp(m.c2.x, -1.0f, 1.0f);
        if (MathF.Abs(sy) < 0.99999f)
        {
            rx = MathF.Atan2(-m.c2.y, m.c2.z);
            ry = MathF.Asin(sy);
            rz = MathF.Atan2(-m.c1.x, m.c0.x);
        }
        else
        {
            rx = MathF.Atan2(m.c1.z, m.c1.y);
            ry = MathF.Asin(sy);
            rz = 0.0f;
        }

        const float radToDeg = 180.0f / MathF.PI;
        return new Vector3(rx * radToDeg, ry * radToDeg, rz * radToDeg);
    }
}
