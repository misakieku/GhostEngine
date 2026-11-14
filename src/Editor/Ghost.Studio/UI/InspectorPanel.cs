using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Studio.Backend;
using Hexa.NET.ImGui;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Renders component property grids and component lifecycle controls for the active <see cref="EntityNode"/>.
/// </summary>
internal sealed class InspectorPanel
{
    private readonly Dictionary<PropertyNode, Vector3> _eulerCache = new();
    private PropertyNode? _activeEditProperty;
    private object? _editStartValue;
    private int _headerEntityId = -1;
    private string _headerNameBuffer = string.Empty;
    private string _addComponentSearch = string.Empty;

    public void Draw(ImGuiContextManager contextManager, ref bool isOpen)
    {
        ImGui.SetNextWindowPos(new Vector2(1270.0f, 32.0f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(322.0f, 820.0f), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin("Inspector", ref isOpen))
        {
            ImGui.End();
            return;
        }

        ref readonly var tokens = ref contextManager.Tokens;
        var selection = EditorSelection.Instance;
        var entity = selection.ActiveEntity;

        if (entity == null)
        {
            ImGui.TextColored(tokens.TextDisabled, "Select an entity to inspect its properties.");
            ImGui.End();
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

        ImGui.End();
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
        var type = prop.PropertyType;
        var val = prop.Value;

        if (type == typeof(bool) && val is bool bVal)
        {
            var current = bVal;
            if (ImGui.Checkbox("##val", ref current))
            {
                CommandHistory.Instance.Execute(new ModifyPropertyCommand(prop, bVal, current));
            }

            return;
        }

        if (type == typeof(int) && val is int iVal)
        {
            var current = iVal;
            var edited = ImGui.DragInt("##val", ref current, 1.0f);
            TrackContinuousEdit(prop, iVal, current, edited);
            return;
        }

        if (type == typeof(uint) && val is uint uVal)
        {
            var current = (int)Math.Min(int.MaxValue, uVal);
            var edited = ImGui.DragInt("##val", ref current, 1.0f, 0, int.MaxValue);
            TrackContinuousEdit(prop, uVal, (uint)Math.Max(0, current), edited);
            return;
        }

        if (type == typeof(float) && val is float fVal)
        {
            var current = fVal;
            var edited = ImGui.DragFloat("##val", ref current, 0.05f);
            TrackContinuousEdit(prop, fVal, current, edited);
            return;
        }

        if (type == typeof(float2) && val is float2 f2Val)
        {
            var vec = new Vector2(f2Val.x, f2Val.y);
            var edited = ImGui.DragFloat2("##val", ref vec, 0.05f);
            TrackContinuousEdit(prop, f2Val, new float2(vec.X, vec.Y), edited);
            return;
        }

        if (type == typeof(Vector2) && val is Vector2 v2Val)
        {
            var vec = v2Val;
            var edited = ImGui.DragFloat2("##val", ref vec, 0.05f);
            TrackContinuousEdit(prop, v2Val, vec, edited);
            return;
        }

        if (type == typeof(float3) && val is float3 f3Val)
        {
            var vec = new Vector3(f3Val.x, f3Val.y, f3Val.z);
            var edited = IsColorProperty(prop.Name)
                ? ImGui.ColorEdit3("##val", ref vec, ImGuiColorEditFlags.Float)
                : ImGui.DragFloat3("##val", ref vec, 0.05f);
            TrackContinuousEdit(prop, f3Val, new float3(vec.X, vec.Y, vec.Z), edited);
            return;
        }

        if (type == typeof(Vector3) && val is Vector3 v3Val)
        {
            var vec = v3Val;
            var edited = IsColorProperty(prop.Name)
                ? ImGui.ColorEdit3("##val", ref vec, ImGuiColorEditFlags.Float)
                : ImGui.DragFloat3("##val", ref vec, 0.05f);
            TrackContinuousEdit(prop, v3Val, vec, edited);
            return;
        }

        if (type == typeof(float4) && val is float4 f4Val)
        {
            var vec = new Vector4(f4Val.x, f4Val.y, f4Val.z, f4Val.w);
            var edited = IsColorProperty(prop.Name)
                ? ImGui.ColorEdit4("##val", ref vec, ImGuiColorEditFlags.Float)
                : ImGui.DragFloat4("##val", ref vec, 0.05f);
            TrackContinuousEdit(prop, f4Val, new float4(vec.X, vec.Y, vec.Z, vec.W), edited);
            return;
        }

        if (type == typeof(Vector4) && val is Vector4 v4Val)
        {
            var vec = v4Val;
            var edited = IsColorProperty(prop.Name)
                ? ImGui.ColorEdit4("##val", ref vec, ImGuiColorEditFlags.Float)
                : ImGui.DragFloat4("##val", ref vec, 0.05f);
            TrackContinuousEdit(prop, v4Val, vec, edited);
            return;
        }

        if (type == typeof(quaternion) && val is quaternion qVal)
        {
            var isEditingThis = ReferenceEquals(_activeEditProperty, prop);
            if (!isEditingThis || !_eulerCache.TryGetValue(prop, out var eulerDeg))
            {
                eulerDeg = QuaternionToEulerDegrees(qVal);
                _eulerCache[prop] = eulerDeg;
            }

            var edited = ImGui.DragFloat3("##val", ref eulerDeg, 0.5f);
            if (edited)
            {
                _eulerCache[prop] = eulerDeg;
            }

            var newQuat = quaternion.EulerXYZ(math.radians(new float3(eulerDeg.X, eulerDeg.Y, eulerDeg.Z)));
            TrackContinuousEdit(prop, qVal, newQuat, edited);
            return;
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
                var nextEnum = values.GetValue(currentIdx);
                if (nextEnum != null && !Equals(nextEnum, val))
                {
                    CommandHistory.Instance.Execute(new ModifyPropertyCommand(prop, val, nextEnum));
                }
            }

            return;
        }

        if (type == typeof(string))
        {
            var str = val as string ?? string.Empty;
            var edited = ImGui.InputText("##val", ref str, 256);
            TrackContinuousEdit(prop, val, str, edited);
            return;
        }

        DrawReadOnlyValue(contextManager, val, in tokens);
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
            prop.Value = nextValue;
            prop.OnChange(nextValue);
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
