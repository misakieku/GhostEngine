using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Studio.Backend;
using Hexa.NET.ImGui;
using System.Numerics;

namespace Ghost.Studio.UI;

internal sealed partial class InspectorPanel
{
    private string _addVolumeOverrideSearch = string.Empty;
    private VolumeOverridesPropertyNode? _activeVolumeProp;
    private Type? _activeVolumeSettingsType;
    private string? _activeVolumeFieldName;
    private bool _volumeEditStartOverridden;
    private object? _volumeEditStartValue;

    private void DrawVolumeOverridesSection(ImGuiContextManager contextManager, EntityNode entity, VolumeOverridesPropertyNode volumeProp, in EditorColorTokens tokens)
    {
        if (!HasComponentType(entity, typeof(RenderVolume)))
        {
            ImGui.TextColored(tokens.SemanticWarning, $"{EditorIcons.WARNING} Requires RenderVolume on this entity.");
        }

        if (volumeProp.Overrides.Count == 0)
        {
            ImGui.TextColored(tokens.TextDisabled, "No overrides added.");
        }
        else
        {
            for (var i = 0; i < volumeProp.Overrides.Count; i++)
            {
                var entry = volumeProp.Overrides[i];
                if (!DrawVolumeOverrideEntry(contextManager, volumeProp, entry, in tokens))
                {
                    break;
                }
            }
        }

        ImGui.Spacing();
        DrawAddVolumeOverrideButton(volumeProp, in tokens);
    }

    private bool DrawVolumeOverrideEntry(ImGuiContextManager contextManager, VolumeOverridesPropertyNode volumeProp, VolumeOverrideEntry entry, in EditorColorTokens tokens)
    {
        var typeKey = entry.SettingsType.FullName ?? entry.SettingsType.Name;
        ImGui.PushID(typeKey);

        var rowHeight = ImGui.GetFrameHeight();
        var menuBtnWidth = rowHeight;
        var style = ImGui.GetStyle();
        var headerWidth = MathF.Max(40.0f, ImGui.GetContentRegionAvail().X - menuBtnWidth - style.ItemSpacing.X);

        var activeCount = 0;
        for (var f = 0; f < entry.Fields.Count; f++)
        {
            if (entry.Fields[f].IsOverridden)
            {
                activeCount++;
            }
        }

        ImGui.PushStyleColor(ImGuiCol.Header, tokens.HeaderBar);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, tokens.ControlHovered);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, tokens.ControlActive);

        var chevron = entry.IsExpanded ? EditorIcons.CHEVRON_DOWN : EditorIcons.CHEVRON_RIGHT;
        var label = $"{chevron}  {entry.DisplayName}  ({activeCount}/{entry.Fields.Count})";
        var headerFlags = ImGuiSelectableFlags.AllowOverlap;
        if (ImGui.Selectable($"{label}##hdr", false, headerFlags, new Vector2(headerWidth, rowHeight)))
        {
            entry.IsExpanded = !entry.IsExpanded;
        }

        ImGui.PopStyleColor(3);

        ImGui.SameLine();
        if (ImGui.Button($"{EditorIcons.SETTINGS}##Menu", new Vector2(menuBtnWidth, rowHeight)))
        {
            ImGui.OpenPopup("##OverrideMenu");
        }

        var removed = false;
        if (ImGui.BeginPopup("##OverrideMenu"))
        {
            if (ImGui.MenuItem("All"))
            {
                var nextBoxed = BuildAllOverriddenBoxed(entry);
                CommandHistory.Instance.Execute(new SetVolumeOverrideBoxedCommand(volumeProp, entry.SettingsType, entry.BoxedValue, nextBoxed, $"Enable All {entry.DisplayName}"));
            }

            if (ImGui.MenuItem("None"))
            {
                CommandHistory.Instance.Execute(new SetVolumeOverrideBoxedCommand(volumeProp, entry.SettingsType, entry.BoxedValue, entry.Accessor.EmptyValue, $"Clear {entry.DisplayName}"));
            }

            if (ImGui.MenuItem("Reset to Defaults"))
            {
                CommandHistory.Instance.Execute(new SetVolumeOverrideBoxedCommand(volumeProp, entry.SettingsType, entry.BoxedValue, entry.Accessor.DefaultValue, $"Reset {entry.DisplayName}"));
            }

            ImGui.Separator();
            if (ImGui.MenuItem("Remove"))
            {
                CommandHistory.Instance.Execute(new RemoveVolumeOverrideCommand(volumeProp, entry.SettingsType));
                removed = true;
            }

            ImGui.EndPopup();
        }

        if (removed)
        {
            ImGui.PopID();
            return false;
        }

        if (entry.IsExpanded)
        {
            var tableFlags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.PadOuterX;
            if (ImGui.BeginTable("##VolFields", 3, tableFlags))
            {
                ImGui.TableSetupColumn("Ovr", ImGuiTableColumnFlags.WidthFixed, rowHeight + 4.0f);
                ImGui.TableSetupColumn("Property", ImGuiTableColumnFlags.WidthStretch, 0.40f);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch, 0.60f);

                for (var f = 0; f < entry.Fields.Count; f++)
                {
                    DrawVolumeFieldRow(contextManager, volumeProp, entry, entry.Fields[f], in tokens);
                }

                ImGui.EndTable();
            }
        }

        ImGui.PopID();
        return true;
    }

    private void DrawVolumeFieldRow(ImGuiContextManager contextManager, VolumeOverridesPropertyNode volumeProp, VolumeOverrideEntry entry, VolumeOverrideField field, in EditorColorTokens tokens)
    {
        ImGui.PushID(field.Name);
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        if (field.IsNullable)
        {
            var isOverridden = field.IsOverridden;
            if (ImGui.Checkbox("##ovr", ref isOverridden))
            {
                var capturedVal = field.ValueNode is AssetPropertyNode assetNode
                    ? (object)assetNode.AssetGuid
                    : (field.ValueNode.Value ?? field.DefaultFieldValue);
                CommandHistory.Instance.Execute(new ModifyVolumeFieldCommand(volumeProp, entry.SettingsType, field.Name, field.IsOverridden, capturedVal, isOverridden, capturedVal));
            }
        }

        ImGui.TableSetColumnIndex(1);
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(field.IsOverridden ? tokens.TextPrimary : tokens.TextDisabled, field.DisplayName);
        if (field.IsHardcut && ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Hardcut parameter (no smooth blending across volume weights)");
        }

        ImGui.TableSetColumnIndex(2);
        ImGui.SetNextItemWidth(-1.0f);

        if (!field.IsOverridden)
        {
            ImGui.BeginDisabled();
        }

        if (field.ValueNode is AssetPropertyNode assetField)
        {
            if (EditorObjectPickers.DrawAssetField(field.Name, assetField.AssetGuid, assetField.ExpectedAssetType, _assetCatalog, _projectService, in tokens, out var nextGuid, e => PingAssetEntry(e)))
            {
                if (nextGuid != assetField.AssetGuid)
                {
                    CommandHistory.Instance.Execute(new ModifyVolumeFieldCommand(volumeProp, entry.SettingsType, field.Name, field.IsOverridden, assetField.AssetGuid, true, nextGuid));
                }
            }
        }
        else
        {
            var currentVal = field.ValueNode.Value ?? field.DefaultFieldValue;
            if (!DrawScalarOrVectorWidget(contextManager, field.ValueNode, field.ValueType, currentVal, field.Name, in tokens, out var nextVal, out var isDiscrete))
            {
                DrawReadOnlyValue(contextManager, currentVal, in tokens);
            }
            else if (isDiscrete)
            {
                if (nextVal != null && !Equals(nextVal, currentVal))
                {
                    CommandHistory.Instance.Execute(new ModifyVolumeFieldCommand(volumeProp, entry.SettingsType, field.Name, field.IsOverridden, currentVal, true, nextVal));
                }
            }
            else
            {
                TrackVolumeFieldContinuousEdit(volumeProp, entry.SettingsType, field, currentVal, nextVal);
            }
        }

        if (!field.IsOverridden)
        {
            ImGui.EndDisabled();
        }

        ImGui.PopID();
    }

    private void TrackVolumeFieldContinuousEdit(VolumeOverridesPropertyNode volumeProp, Type settingsType, VolumeOverrideField field, object? currentVal, object? nextVal)
    {
        if (ImGui.IsItemActivated())
        {
            _activeVolumeProp = volumeProp;
            _activeVolumeSettingsType = settingsType;
            _activeVolumeFieldName = field.Name;
            _volumeEditStartOverridden = field.IsOverridden;
            _volumeEditStartValue = currentVal;
        }

        if (nextVal != null && !Equals(nextVal, currentVal))
        {
            volumeProp.SetFieldState(settingsType, field.Name, field.IsOverridden, nextVal);
        }

        if (ImGui.IsItemDeactivatedAfterEdit() && ReferenceEquals(_activeVolumeProp, volumeProp) && _activeVolumeSettingsType == settingsType && string.Equals(_activeVolumeFieldName, field.Name, StringComparison.Ordinal))
        {
            var startOverridden = _volumeEditStartOverridden;
            var startVal = _volumeEditStartValue;
            _activeVolumeProp = null;
            _activeVolumeSettingsType = null;
            _activeVolumeFieldName = null;
            _volumeEditStartValue = null;

            var finalVal = field.ValueNode.Value;
            if (startOverridden != field.IsOverridden || !Equals(startVal, finalVal))
            {
                CommandHistory.Instance.Execute(new ModifyVolumeFieldCommand(volumeProp, settingsType, field.Name, startOverridden, startVal, field.IsOverridden, finalVal));
            }
        }
    }

    private void DrawAddVolumeOverrideButton(VolumeOverridesPropertyNode volumeProp, in EditorColorTokens tokens)
    {
        var availW = ImGui.GetContentRegionAvail().X;
        var btnW = MathF.Max(120.0f, availW);
        if (ImGui.Button("Add Override", new Vector2(btnW, 0.0f)))
        {
            _addVolumeOverrideSearch = string.Empty;
            ImGui.OpenPopup("##AddVolumeOverridePopup");
        }

        if (!ImGui.BeginPopup("##AddVolumeOverridePopup"))
        {
            return;
        }

        ImGuiWidgets.SearchField("##AddVolOvrSearch", "Search overrides...", ref _addVolumeOverrideSearch, 230.0f, in tokens);
        ImGui.Separator();

        ImGui.BeginChild("##AddVolOvrList", new Vector2(230.0f, 200.0f), ImGuiChildFlags.None);
        var discovered = VolumeTypeRegistry.DiscoverVolumeTypes();
        var availableCount = 0;

        for (var i = 0; i < discovered.Count; i++)
        {
            var accessor = discovered[i];
            if (volumeProp.HasOverride(accessor.SettingsType))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(_addVolumeOverrideSearch) && !accessor.DisplayName.Contains(_addVolumeOverrideSearch, StringComparison.OrdinalIgnoreCase) && !accessor.SettingsType.Name.Contains(_addVolumeOverrideSearch, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            availableCount++;
            if (ImGui.Selectable(accessor.DisplayName))
            {
                CommandHistory.Instance.Execute(new AddVolumeOverrideCommand(volumeProp, accessor.SettingsType));
                ImGui.CloseCurrentPopup();
            }
        }

        if (availableCount == 0)
        {
            ImGui.TextColored(tokens.TextDisabled, "No overrides available.");
        }

        ImGui.EndChild();
        ImGui.EndPopup();
    }

    private static object BuildAllOverriddenBoxed(VolumeOverrideEntry entry)
    {
        var boxed = entry.BoxedValue;
        for (var i = 0; i < entry.Fields.Count; i++)
        {
            var field = entry.Fields[i];
            if (!field.IsOverridden)
            {
                field.Field.SetValue(boxed, field.ValueNode.Value ?? field.DefaultFieldValue);
            }
        }

        return boxed;
    }
}
