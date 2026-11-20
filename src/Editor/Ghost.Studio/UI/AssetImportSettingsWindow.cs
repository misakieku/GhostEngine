using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Studio.Backend;
using Ghost.Studio.Services;
using Hexa.NET.ImGui;
using Misaki.HighPerformance.Mathematics;
using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Ghost.Studio.UI;

/// <summary>
/// Unreal-style floating window for viewing and editing an asset's <see cref="IBakeSettings"/>
/// stored in its <c>.meta</c> file, then applying and triggering an async reimport bake.
/// </summary>
internal sealed class AssetImportSettingsWindow : EditorWindow
{
    private readonly ProjectService _projectService;
    private readonly AssetImportService _assetImportService;
    private readonly EditorAssetCatalog _assetCatalog;

    private string _assetFilePath = string.Empty;
    private string _displayRelativePath = string.Empty;
    private AssetMetadata? _editingMetadata;
    private string _baselineJson = string.Empty;
    private string _statusMessage = string.Empty;
    private string _newRemapSlotName = string.Empty;

    public override bool ShowInWindowMenu => false;

    public override bool ScaleDefaultSizeByDpi => true;

    public override Vector2 DefaultSize => new(460.0f, 600.0f);

    public override bool IsDirty => _editingMetadata != null && !string.Equals(_projectService.SerializeMetadata(_editingMetadata), _baselineJson, StringComparison.Ordinal);

    public AssetImportSettingsWindow(ProjectService projectService, AssetImportService assetImportService, EditorAssetCatalog assetCatalog)
        : base("Asset Import Settings", "Asset Import Settings", openByDefault: false)
    {
        _projectService = projectService;
        _assetImportService = assetImportService;
        _assetCatalog = assetCatalog;
    }

    public void OpenForAsset(string assetFilePath)
    {
        if (string.IsNullOrWhiteSpace(assetFilePath) || !File.Exists(assetFilePath))
        {
            return;
        }

        var fullPath = Path.GetFullPath(assetFilePath);
        var loaded = _projectService.GetOrCreateMetadata(fullPath);
        if (loaded == null || loaded.Settings == null)
        {
            Logger.Info($"No configurable import settings for '{Path.GetFileName(fullPath)}'.");
            return;
        }

        var json = _projectService.SerializeMetadata(loaded);
        var workingCopy = _projectService.DeserializeMetadata(json);
        if (workingCopy == null)
        {
            return;
        }

        _assetFilePath = fullPath;
        _displayRelativePath = ComputeDisplayPath(fullPath);
        _editingMetadata = workingCopy;
        _baselineJson = json;
        _statusMessage = string.Empty;
        _newRemapSlotName = string.Empty;
        RequestFocus();
    }

    protected override void OnDraw(ImGuiContextManager contextManager)
    {
        if (_editingMetadata == null)
        {
            return;
        }

        ref readonly var tokens = ref contextManager.Tokens;
        var isDirty = IsDirty;

        DrawHeader(contextManager, isDirty, in tokens);
        ImGui.Separator();

        var footerHeight = ImGui.GetFrameHeightWithSpacing() * 2.0f + 10.0f * contextManager.DpiScale;
        ImGui.BeginChild("##ImportSettingsBody", new Vector2(0.0f, -footerHeight), ImGuiChildFlags.None);

        if (_editingMetadata.Settings != null)
        {
            if (_editingMetadata.Settings is MeshBakeSettings meshSettings)
            {
                DrawMeshExtractionActions(meshSettings, in tokens);
                ImGui.Separator();
            }

            var drawnAny = DrawSettingsObject(contextManager, _editingMetadata.Settings, in tokens, isRoot: true);
            if (!drawnAny)
            {
                ImGui.TextColored(tokens.TextDisabled, "No configurable import settings for this asset type.");
            }
        }
        else
        {
            ImGui.TextColored(tokens.TextDisabled, "This asset has no bake settings.");
        }

        ImGui.EndChild();

        ImGui.Separator();
        DrawFooter(contextManager, isDirty, in tokens);
    }

    private void DrawHeader(ImGuiContextManager contextManager, bool isDirty, in EditorColorTokens tokens)
    {
        var fileName = Path.GetFileName(_assetFilePath);
        ImGui.PushFont(contextManager.HeadingFont, EditorTheme.FONT_SIZE_HEADING);
        ImGui.TextUnformatted(isDirty ? $"{fileName} *" : fileName);
        ImGui.PopFont();

        if (!string.IsNullOrEmpty(_displayRelativePath))
        {
            ImGui.TextColored(tokens.TextSecondary, _displayRelativePath);
        }

        ImGui.PushFont(contextManager.MonospaceFont, EditorTheme.FONT_SIZE_MONO);
        var version = GetSettingsVersion(_editingMetadata!.Settings);
        var metaInfo = version >= 0
            ? $"Type: {_editingMetadata.Type}  |  v{version}  |  {_editingMetadata.Id:D}"
            : $"Type: {_editingMetadata.Type}  |  {_editingMetadata.Id:D}";
        ImGui.TextColored(tokens.TextDisabled, metaInfo);
        ImGui.PopFont();
    }

    private void DrawFooter(ImGuiContextManager contextManager, bool isDirty, in EditorColorTokens tokens)
    {
        if (ImGui.Button("Reset Defaults"))
        {
            var ext = Path.GetExtension(_assetFilePath);
            var defaults = _projectService.BakerRegistry.CreateDefaultSettings(ext);
            if (defaults != null)
            {
                _editingMetadata = _editingMetadata! with { Settings = defaults };
                _statusMessage = "Reset to baker defaults (not yet applied).";
            }
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(!isDirty);
        if (ImGui.Button("Revert"))
        {
            var reverted = _projectService.DeserializeMetadata(_baselineJson);
            if (reverted != null)
            {
                _editingMetadata = reverted;
                _statusMessage = "Reverted unsaved changes.";
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Apply"))
        {
            SaveMetadataToDisk();
            _statusMessage = "Saved .meta settings.";
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Apply & Reimport"))
        {
            if (isDirty || !File.Exists(_assetFilePath + ".meta"))
            {
                SaveMetadataToDisk();
            }

            _assetImportService.EnqueueReimport(_assetFilePath, forceRebake: true);
            _statusMessage = "Queued async reimport...";
        }

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            ImGui.TextColored(tokens.TextSecondary, _statusMessage);
        }
    }

    private void SaveMetadataToDisk()
    {
        if (_editingMetadata == null)
        {
            return;
        }

        var metaPath = _assetFilePath + ".meta";
        _projectService.SaveMetadata(metaPath, _editingMetadata);
        _baselineJson = _projectService.SerializeMetadata(_editingMetadata);
    }

    private string ComputeDisplayPath(string fullPath)
    {
        var primaryDir = _projectService.AssetDirectories.LastOrDefault();
        if (!string.IsNullOrEmpty(primaryDir))
        {
            return Path.GetRelativePath(Path.GetFullPath(primaryDir), fullPath).Replace('\\', '/');
        }

        return fullPath;
    }

    private static int GetSettingsVersion(IBakeSettings? settings)
    {
        if (settings == null)
        {
            return -1;
        }

        var prop = settings.GetType().GetProperty("Version", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null && prop.PropertyType == typeof(int) && prop.GetValue(settings) is int version)
        {
            return version;
        }

        return -1;
    }

    private bool DrawSettingsObject(ImGuiContextManager contextManager, object target, in EditorColorTokens tokens, bool isRoot)
    {
        var type = target.GetType();
        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        var scalarProps = new List<PropertyInfo>();
        var nestedProps = new List<PropertyInfo>();

        for (var i = 0; i < props.Length; i++)
        {
            var prop = props[i];
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (isRoot && string.Equals(prop.Name, "Version", StringComparison.Ordinal))
            {
                continue;
            }

            if (prop.GetCustomAttribute<HideInInspectorAttribute>() != null)
            {
                continue;
            }

            if (!EvaluateShowWhen(target, prop))
            {
                continue;
            }

            if (IsNestedSettingsObject(prop.PropertyType))
            {
                nestedProps.Add(prop);
            }
            else
            {
                scalarProps.Add(prop);
            }
        }

        var drawnAny = false;

        if (scalarProps.Count > 0)
        {
            DrawPropertyTable(contextManager, target, scalarProps, in tokens);
            drawnAny = true;
        }

        for (var i = 0; i < nestedProps.Count; i++)
        {
            var nestedProp = nestedProps[i];
            var nestedObj = nestedProp.GetValue(target);
            if (nestedObj == null)
            {
                continue;
            }

            var headerLabel = nestedProp.GetCustomAttribute<InspectorNameAttribute>()?.Name ?? FormatDisplayName(nestedProp.Name);
            ImGui.PushID(nestedProp.Name);
            if (ImGui.CollapsingHeader(headerLabel, ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (DrawSettingsObject(contextManager, nestedObj, in tokens, isRoot: false))
                {
                    drawnAny = true;
                }
            }
            else
            {
                drawnAny = true;
            }
            ImGui.PopID();
        }

        return drawnAny;
    }

    private void DrawMeshExtractionActions(MeshBakeSettings meshSettings, in EditorColorTokens tokens)
    {
        ImGui.TextColored(tokens.TextSecondary, "Sub-Asset Extraction");

        if (ImGui.Button("Extract Materials"))
        {
            meshSettings.ExtractMaterials = true;
            SaveMetadataToDisk();
            _assetImportService.EnqueueReimport(_assetFilePath, forceRebake: true);
            _statusMessage = "Queued material (.gmat) extraction and reimport...";
        }

        ImGui.SameLine();
        if (ImGui.Button("Extract Embedded Textures"))
        {
            meshSettings.ExtractEmbeddedTextures = true;
            SaveMetadataToDisk();
            _assetImportService.EnqueueReimport(_assetFilePath, forceRebake: true);
            _statusMessage = "Queued embedded texture extraction and reimport...";
        }
    }

    private void DrawPropertyTable(ImGuiContextManager contextManager, object target, List<PropertyInfo> props, in EditorColorTokens tokens)
    {
        const ImGuiTableFlags tableFlags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.PadOuterX;
        if (!ImGui.BeginTable("##AssetSettingsTable", 2, tableFlags))
        {
            return;
        }

        ImGui.TableSetupColumn("Property", ImGuiTableColumnFlags.WidthStretch, 0.45f);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch, 0.55f);

        string? lastGroup = null;
        for (var i = 0; i < props.Count; i++)
        {
            var prop = props[i];
            var groupName = prop.GetCustomAttribute<InspectorGroupAttribute>()?.GroupName;
            if (!string.IsNullOrEmpty(groupName) && !string.Equals(groupName, lastGroup, StringComparison.Ordinal))
            {
                lastGroup = groupName;
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.TextColored(tokens.TextSecondary, groupName);
                ImGui.TableSetColumnIndex(1);
            }

            DrawPropertyRow(contextManager, target, prop, in tokens);
        }

        ImGui.EndTable();
    }

    private void DrawPropertyRow(ImGuiContextManager contextManager, object target, PropertyInfo prop, in EditorColorTokens tokens)
    {
        var isReadOnly = !prop.CanWrite || prop.GetCustomAttribute<ReadOnlyInInspectorAttribute>() != null;
        var label = prop.GetCustomAttribute<InspectorNameAttribute>()?.Name ?? FormatDisplayName(prop.Name);
        var val = prop.GetValue(target);

        ImGui.PushID(prop.Name);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(isReadOnly ? tokens.TextDisabled : tokens.TextPrimary, label);

        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(-1.0f);

        if (isReadOnly)
        {
            DrawReadOnlyValue(contextManager, val, in tokens);
        }
        else
        {
            DrawEditableProperty(contextManager, target, prop, val, in tokens);
        }

        ImGui.PopID();
    }

    private void DrawEditableProperty(ImGuiContextManager contextManager, object target, PropertyInfo prop, object? val, in EditorColorTokens tokens)
    {
        var type = prop.PropertyType;
        var slider = prop.GetCustomAttribute<SliderAttribute>();

        if (type == typeof(bool) && val is bool bVal)
        {
            if (ImGui.Checkbox("##val", ref bVal))
            {
                prop.SetValue(target, bVal);
            }
            return;
        }

        if (type == typeof(byte) && val is byte byteVal)
        {
            var current = (int)byteVal;
            var min = slider != null ? (int)Math.Clamp(slider.Min, 0.0, 255.0) : 0;
            var max = slider != null ? (int)Math.Clamp(slider.Max, min, 255.0) : 255;
            var edited = slider != null ? ImGui.SliderInt("##val", ref current, min, max) : ImGui.DragInt("##val", ref current, 1.0f, 0, 255);
            if (edited)
            {
                prop.SetValue(target, (byte)Math.Clamp(current, 0, 255));
            }
            return;
        }

        if (type == typeof(int) && val is int iVal)
        {
            var current = iVal;
            var edited = slider != null
                ? ImGui.SliderInt("##val", ref current, (int)slider.Min, (int)slider.Max)
                : ImGui.DragInt("##val", ref current, 1.0f);
            if (edited)
            {
                prop.SetValue(target, current);
            }
            return;
        }

        if (type == typeof(uint) && val is uint uVal)
        {
            var current = (int)Math.Min(int.MaxValue, uVal);
            var min = slider != null ? (int)Math.Max(0.0, slider.Min) : 0;
            var max = slider != null ? (int)Math.Clamp(slider.Max, min, int.MaxValue) : int.MaxValue;
            var edited = slider != null ? ImGui.SliderInt("##val", ref current, min, max) : ImGui.DragInt("##val", ref current, 1.0f, 0, int.MaxValue);
            if (edited)
            {
                prop.SetValue(target, (uint)Math.Max(0, current));
            }
            return;
        }

        if (type == typeof(float) && val is float fVal)
        {
            var current = fVal;
            var edited = slider != null
                ? ImGui.SliderFloat("##val", ref current, (float)slider.Min, (float)slider.Max)
                : ImGui.DragFloat("##val", ref current, 0.05f);
            if (edited)
            {
                prop.SetValue(target, current);
            }
            return;
        }

        if (type == typeof(double) && val is double dVal)
        {
            var current = (float)dVal;
            var edited = slider != null
                ? ImGui.SliderFloat("##val", ref current, (float)slider.Min, (float)slider.Max)
                : ImGui.DragFloat("##val", ref current, 0.05f);
            if (edited)
            {
                prop.SetValue(target, (double)current);
            }
            return;
        }

        if (type == typeof(float2) && val is float2 f2Val)
        {
            var vec = new Vector2(f2Val.x, f2Val.y);
            if (ImGui.DragFloat2("##val", ref vec, 0.05f))
            {
                prop.SetValue(target, new float2(vec.X, vec.Y));
            }
            return;
        }

        if (type == typeof(float3) && val is float3 f3Val)
        {
            var vec = new Vector3(f3Val.x, f3Val.y, f3Val.z);
            var edited = IsColorProperty(prop.Name) ? ImGui.ColorEdit3("##val", ref vec, ImGuiColorEditFlags.Float) : ImGui.DragFloat3("##val", ref vec, 0.05f);
            if (edited)
            {
                prop.SetValue(target, new float3(vec.X, vec.Y, vec.Z));
            }
            return;
        }

        if (type == typeof(float4) && val is float4 f4Val)
        {
            var vec = new Vector4(f4Val.x, f4Val.y, f4Val.z, f4Val.w);
            var edited = IsColorProperty(prop.Name) ? ImGui.ColorEdit4("##val", ref vec, ImGuiColorEditFlags.Float) : ImGui.DragFloat4("##val", ref vec, 0.05f);
            if (edited)
            {
                prop.SetValue(target, new float4(vec.X, vec.Y, vec.Z, vec.W));
            }
            return;
        }

        if (type == typeof(int4) && val is int4 i4Val)
        {
            DrawInt4Property(target, prop, i4Val);
            return;
        }

        if (type.IsEnum && val != null)
        {
            DrawEnumProperty(target, prop, type, val);
            return;
        }

        if (type == typeof(string))
        {
            var str = val as string ?? string.Empty;
            if (ImGui.InputText("##val", ref str, 256))
            {
                prop.SetValue(target, str);
            }
            return;
        }

        if (type == typeof(string[]))
        {
            DrawStringArrayProperty(target, prop, val as string[]);
            return;
        }

        if (val is IDictionary<string, Guid> guidDict)
        {
            DrawGuidDictionaryProperty(prop.Name, guidDict, in tokens);
            return;
        }

        DrawReadOnlyValue(contextManager, val, in tokens);
    }

    private void DrawGuidDictionaryProperty(string propName, IDictionary<string, Guid> dict, in EditorColorTokens tokens)
    {
        ImGui.SetNextItemWidth(MathF.Max(60.0f, ImGui.GetContentRegionAvail().X - 58.0f));
        ImGui.InputTextWithHint("##newSlot", "Material slot name", ref _newRemapSlotName, 128);
        ImGui.SameLine();

        var trimmedSlot = _newRemapSlotName.Trim();
        var canAdd = !string.IsNullOrEmpty(trimmedSlot) && !dict.ContainsKey(trimmedSlot);
        ImGui.BeginDisabled(!canAdd);
        if (ImGui.Button("+ Add##addDict"))
        {
            dict[trimmedSlot] = Guid.Empty;
            _newRemapSlotName = string.Empty;
        }
        ImGui.EndDisabled();

        if (dict.Count == 0)
        {
            ImGui.TextColored(tokens.TextDisabled, "No slot overrides");
            return;
        }

        string? keyToRemove = null;
        var keys = new List<string>(dict.Keys);
        keys.Sort(StringComparer.OrdinalIgnoreCase);

        var style = ImGui.GetStyle();
        var delBtnWidth = ImGui.GetFrameHeight() + style.ItemSpacing.X;

        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            ImGui.PushID(key);
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(tokens.TextPrimary, key);

            if (EditorObjectPickers.DrawAssetField($"{propName}_{key}", dict[key], AssetType.Material, _assetCatalog, _projectService, in tokens,
                out var nextGuid, reservedRightWidth: delBtnWidth))
            {
                dict[key] = nextGuid;
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("X##delDict"))
            {
                keyToRemove = key;
            }
            ImGui.PopID();
        }

        if (keyToRemove != null)
        {
            dict.Remove(keyToRemove);
        }
    }

    private static unsafe void DrawInt4Property(object target, PropertyInfo prop, int4 i4Val)
    {
        var vec = stackalloc int[4];
        vec[0] = i4Val.x;
        vec[1] = i4Val.y;
        vec[2] = i4Val.z;
        vec[3] = i4Val.w;
        if (ImGui.DragInt4("##val", vec, 1.0f))
        {
            prop.SetValue(target, new int4(vec[0], vec[1], vec[2], vec[3]));
        }
    }

    private static void DrawEnumProperty(object target, PropertyInfo prop, Type enumType, object val)
    {
        if (enumType.GetCustomAttribute<FlagsAttribute>() != null)
        {
            var raw = Convert.ToUInt64(val);
            var preview = raw == 0 ? "None" : val.ToString() ?? string.Empty;
            if (ImGui.BeginCombo("##val", preview))
            {
                var names = Enum.GetNames(enumType);
                var values = Enum.GetValues(enumType);
                for (var i = 0; i < values.Length; i++)
                {
                    var flagVal = Convert.ToUInt64(values.GetValue(i)!);
                    if (flagVal == 0)
                    {
                        continue;
                    }

                    var isSet = (raw & flagVal) == flagVal;
                    if (ImGui.Checkbox(names[i], ref isSet))
                    {
                        raw = isSet ? (raw | flagVal) : (raw & ~flagVal);
                        prop.SetValue(target, Enum.ToObject(enumType, raw));
                    }
                }
                ImGui.EndCombo();
            }
            return;
        }

        var enumNames = Enum.GetNames(enumType);
        var enumValues = Enum.GetValues(enumType);
        var currentIdx = 0;
        for (var i = 0; i < enumValues.Length; i++)
        {
            if (Equals(enumValues.GetValue(i), val))
            {
                currentIdx = i;
                break;
            }
        }

        if (ImGui.Combo("##val", ref currentIdx, enumNames, enumNames.Length))
        {
            var nextEnum = enumValues.GetValue(currentIdx);
            if (nextEnum != null)
            {
                prop.SetValue(target, nextEnum);
            }
        }
    }

    private static void DrawStringArrayProperty(object target, PropertyInfo prop, string[]? array)
    {
        var items = array ?? Array.Empty<string>();
        if (ImGui.Button($"+ Add ({items.Length})##add"))
        {
            var expanded = new string[items.Length + 1];
            Array.Copy(items, expanded, items.Length);
            expanded[items.Length] = string.Empty;
            prop.SetValue(target, expanded);
            return;
        }

        for (var i = 0; i < items.Length; i++)
        {
            ImGui.PushID(i);
            var itemText = items[i] ?? string.Empty;
            ImGui.SetNextItemWidth(MathF.Max(40.0f, ImGui.GetContentRegionAvail().X - 28.0f));
            if (ImGui.InputText("##item", ref itemText, 256))
            {
                var updated = (string[])items.Clone();
                updated[i] = itemText;
                prop.SetValue(target, updated);
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("X##del"))
            {
                var shrunk = new string[items.Length - 1];
                if (i > 0)
                {
                    Array.Copy(items, 0, shrunk, 0, i);
                }
                if (i + 1 < items.Length)
                {
                    Array.Copy(items, i + 1, shrunk, i, items.Length - i - 1);
                }
                prop.SetValue(target, shrunk);
                ImGui.PopID();
                return;
            }

            ImGui.PopID();
        }
    }

    private static void DrawReadOnlyValue(ImGuiContextManager contextManager, object? val, in EditorColorTokens tokens)
    {
        ImGui.PushFont(contextManager.MonospaceFont, EditorTheme.FONT_SIZE_MONO);
        string text;
        if (val == null)
        {
            text = "<null>";
        }
        else if (val is IDictionary dict)
        {
            text = $"{dict.Count} entry/entries";
        }
        else
        {
            text = val.ToString() ?? string.Empty;
        }

        ImGui.TextColored(tokens.TextSecondary, text);
        ImGui.PopFont();
    }

    private static bool EvaluateShowWhen(object target, PropertyInfo prop)
    {
        var attrs = prop.GetCustomAttributes<ShowWhenAttribute>(inherit: true);
        var type = target.GetType();

        foreach (var attr in attrs)
        {
            var depProp = type.GetProperty(attr.PropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (depProp != null)
            {
                var currentVal = depProp.GetValue(target);
                if (!Equals(currentVal, attr.Value))
                {
                    return false;
                }
                continue;
            }

            var depField = type.GetField(attr.PropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (depField != null)
            {
                var currentVal = depField.GetValue(target);
                if (!Equals(currentVal, attr.Value))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsNestedSettingsObject(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type.IsArray || type == typeof(string) || type == typeof(decimal))
        {
            return false;
        }

        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        if (type == typeof(float2) || type == typeof(float3) || type == typeof(float4) || type == typeof(int4) || type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4))
        {
            return false;
        }

        return type.IsClass;
    }

    private static bool IsColorProperty(string propertyName)
    {
        return propertyName.Contains("color", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("tint", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("albedo", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatDisplayName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(name.Length + 4);
        sb.Append(name[0]);
        for (var i = 1; i < name.Length; i++)
        {
            var c = name[i];
            var prev = name[i - 1];
            var nextLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
            if (char.IsUpper(c) && (char.IsLower(prev) || (char.IsUpper(prev) && nextLower)))
            {
                sb.Append(' ');
            }
            sb.Append(c);
        }

        return sb.ToString();
    }
}
