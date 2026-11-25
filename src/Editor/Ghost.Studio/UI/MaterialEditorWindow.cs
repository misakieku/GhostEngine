using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.DSL.ShaderCompiler;
using Ghost.Studio.Backend;
using Ghost.Studio.Services;
using Hexa.NET.ImGui;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;
using System.Text.Json;

namespace Ghost.Studio.UI;

/// <summary>
/// Dedicated multi-tab asset editor for <c>.gmat</c> material documents, driving shader selection,
/// dynamic shader properties, and <c>Texture2D</c> slots from <see cref="MaterialPropertyPacker"/>.
/// </summary>
internal sealed class MaterialEditorWindow : AssetEditorWindow
{
    private readonly ProjectService _projectService;
    private readonly AssetImportService _assetImportService;
    private readonly EditorAssetCatalog _assetCatalog;
    private readonly Action<string>? _onPingAsset;

    private MaterialAssetDocument _document = new();
    private string _baselineJson = string.Empty;
    private bool _isDirty;
    private string _loadError = string.Empty;

    private Guid _cachedSemanticsShader = Guid.Empty;
    private bool _semanticsLoaded;
    private List<PropertySemantic> _cachedProperties = new();
    private string _cachedShaderName = string.Empty;

    public override bool IsDirty => _isDirty;

    public MaterialEditorWindow(string assetPath, ProjectService projectService, AssetImportService assetImportService,
        EditorAssetCatalog assetCatalog, Action<string>? onPingAsset = null)
        : base(assetPath, projectService, "MaterialEditor")
    {
        _projectService = projectService;
        _assetImportService = assetImportService;
        _assetCatalog = assetCatalog;
        _onPingAsset = onPingAsset;

        LoadFromDisk();
    }

    public override bool Save()
    {
        try
        {
            var json = SerializeDocument(_document);
            File.WriteAllText(AssetPath, json);
            _baselineJson = json;
            _isDirty = false;
            _assetImportService.EnqueueReimport(AssetPath, forceRebake: true);
            StatusMessage = "Saved and queued live reimport.";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
            Logger.Error($"Failed to save material '{AssetPath}': {ex.Message}");
            return false;
        }
    }

    public override void Revert()
    {
        if (string.IsNullOrEmpty(_baselineJson))
        {
            return;
        }

        try
        {
            _document = JsonSerializer.Deserialize(_baselineJson, MaterialAssetDocumentContext.Default.MaterialAssetDocument) ?? new MaterialAssetDocument();
            _semanticsLoaded = false;
            _isDirty = false;
            StatusMessage = "Reverted unsaved changes.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Revert failed: {ex.Message}";
        }
    }

    protected override void OnDrawAssetEditor(ImGuiContextManager contextManager)
    {
        ref readonly var tokens = ref contextManager.Tokens;

        var meta = _projectService.GetOrCreateMetadata(AssetPath);
        var subtitle = meta != null ? $"Material  |  {meta.Id:D}" : "Material";
        DrawStandardHeader(contextManager, subtitle, in tokens);
        ImGui.Separator();

        if (!string.IsNullOrEmpty(_loadError))
        {
            ImGuiWidgets.StatusBadge(_loadError, tokens.SemanticError);
            ImGui.Separator();
        }

        var footerHeight = ImGui.GetFrameHeightWithSpacing() * 2.0f + 10.0f * contextManager.DpiScale;
        ImGui.BeginChild("##MaterialEditorBody", new Vector2(0.0f, -footerHeight), ImGuiChildFlags.None);

        DrawShaderSection(in tokens);
        ImGui.Separator();
        DrawParametersAndTextures(in tokens);

        ImGui.EndChild();

        ImGui.Separator();
        DrawFooter(in tokens);
    }

    private void LoadFromDisk()
    {
        try
        {
            var rawJson = File.ReadAllText(AssetPath);
            _document = JsonSerializer.Deserialize(rawJson, MaterialAssetDocumentContext.Default.MaterialAssetDocument) ?? new MaterialAssetDocument();
            _baselineJson = SerializeDocument(_document);
            _isDirty = false;
            _loadError = string.Empty;
        }
        catch (Exception ex)
        {
            _document = new MaterialAssetDocument();
            _baselineJson = SerializeDocument(_document);
            _isDirty = false;
            _loadError = $"Failed to parse .gmat: {ex.Message}";
        }

        _semanticsLoaded = false;
    }

    private void EnsureSemanticsLoaded()
    {
        var effectiveShader = _document.Shader == Guid.Empty ? MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID : _document.Shader;
        if (_semanticsLoaded && _cachedSemanticsShader == effectiveShader)
        {
            return;
        }

        _cachedSemanticsShader = effectiveShader;
        _semanticsLoaded = true;
        _cachedProperties.Clear();
        _cachedShaderName = string.Empty;

        var semantics = MaterialPropertyPacker.GetShaderSemantics(effectiveShader, _projectService.AssetDirectories);
        if (semantics != null)
        {
            _cachedShaderName = semantics.name ?? string.Empty;
            _cachedProperties = MaterialPropertyPacker.CollectAllProperties(semantics);
        }
    }

    private void DrawShaderSection(in EditorColorTokens tokens)
    {
        EnsureSemanticsLoaded();

        var effectiveShader = _document.Shader == Guid.Empty ? MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID : _document.Shader;
        var tableFlags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.PadOuterX;
        if (ImGui.BeginTable("##MatShaderTable", 2, tableFlags))
        {
            ImGui.TableSetupColumn("Property", ImGuiTableColumnFlags.WidthStretch, 0.36f);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch, 0.64f);

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(tokens.TextPrimary, "Shader");

            ImGui.TableSetColumnIndex(1);
            if (EditorObjectPickers.DrawAssetField("MatShader", effectiveShader, AssetType.Shader, _assetCatalog, _projectService,
                in tokens, out var nextShader, PingEntry, allowClear: true))
            {
                _document.Shader = nextShader == MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID ? Guid.Empty : nextShader;
                _semanticsLoaded = false;
                MarkDirtyState();
            }

            if (!string.IsNullOrEmpty(_cachedShaderName))
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.TextColored(tokens.TextDisabled, "Shader Name");
                ImGui.TableSetColumnIndex(1);
                ImGui.TextColored(tokens.TextSecondary, _cachedShaderName);
            }

            ImGui.EndTable();
        }
    }

    private void DrawParametersAndTextures(in EditorColorTokens tokens)
    {
        EnsureSemanticsLoaded();

        if (_cachedProperties.Count == 0)
        {
            ImGui.TextColored(tokens.SemanticWarning, "Unable to resolve shader properties for the selected shader GUID.");
            return;
        }

        var scalarProps = new List<PropertySemantic>();
        var textureProps = new List<PropertySemantic>();
        var samplerProps = new List<PropertySemantic>();

        for (var i = 0; i < _cachedProperties.Count; i++)
        {
            var prop = _cachedProperties[i];
            if (MaterialPropertyPacker.IsTextureType(prop.type))
            {
                textureProps.Add(prop);
            }
            else if (MaterialPropertyPacker.IsSamplerType(prop.type))
            {
                samplerProps.Add(prop);
            }
            else
            {
                scalarProps.Add(prop);
            }
        }

        if (scalarProps.Count > 0 && ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var tableFlags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.PadOuterX;
            if (ImGui.BeginTable("##MatScalarProps", 2, tableFlags))
            {
                ImGui.TableSetupColumn("Property", ImGuiTableColumnFlags.WidthStretch, 0.36f);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch, 0.64f);

                for (var i = 0; i < scalarProps.Count; i++)
                {
                    DrawScalarPropertyRow(scalarProps[i], in tokens);
                }

                ImGui.EndTable();
            }
        }

        if (textureProps.Count > 0 && ImGui.CollapsingHeader("Textures", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var tableFlags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.PadOuterX;
            if (ImGui.BeginTable("##MatTexProps", 2, tableFlags))
            {
                ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.WidthStretch, 0.36f);
                ImGui.TableSetupColumn("Texture", ImGuiTableColumnFlags.WidthStretch, 0.64f);

                for (var i = 0; i < textureProps.Count; i++)
                {
                    DrawTexturePropertyRow(textureProps[i], in tokens);
                }

                ImGui.EndTable();
            }
        }

        if (samplerProps.Count > 0 && ImGui.CollapsingHeader("Samplers", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var tableFlags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.PadOuterX;
            if (ImGui.BeginTable("##MatSamplerProps", 2, tableFlags))
            {
                ImGui.TableSetupColumn("Sampler", ImGuiTableColumnFlags.WidthStretch, 0.36f);
                ImGui.TableSetupColumn("Filter & Wrap", ImGuiTableColumnFlags.WidthStretch, 0.64f);

                for (var i = 0; i < samplerProps.Count; i++)
                {
                    DrawSamplerPropertyRow(samplerProps[i], in tokens);
                }

                ImGui.EndTable();
            }
        }
    }

    private void DrawScalarPropertyRow(PropertySemantic prop, in EditorColorTokens tokens)
    {
        ImGui.PushID(prop.name);
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(tokens.TextPrimary, prop.name);

        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(-1.0f);

        var val = ResolveCurrentValue(prop);
        var typeSpan = prop.type.AsSpan().Trim();

        if (typeSpan.Equals("float", StringComparison.OrdinalIgnoreCase))
        {
            var current = val is float f ? f : (val is IConvertible c ? c.ToSingle(null) : 0.0f);
            if (ImGui.DragFloat("##val", ref current, 0.01f))
            {
                _document.Properties[prop.name] = JsonSerializer.SerializeToElement(current, MaterialAssetDocumentContext.Default.Single);
                MarkDirtyState();
            }
        }
        else if (typeSpan.Equals("float2", StringComparison.OrdinalIgnoreCase))
        {
            var vec = val is float2 f2 ? new Vector2(f2.x, f2.y) : Vector2.Zero;
            if (ImGui.DragFloat2("##val", ref vec, 0.01f))
            {
                _document.Properties[prop.name] = JsonSerializer.SerializeToElement(new[] { vec.X, vec.Y }, MaterialAssetDocumentContext.Default.SingleArray);
                MarkDirtyState();
            }
        }
        else if (typeSpan.Equals("float3", StringComparison.OrdinalIgnoreCase))
        {
            var vec = val is float3 f3 ? new Vector3(f3.x, f3.y, f3.z) : Vector3.Zero;
            var edited = IsColorProperty(prop.name)
                ? ImGui.ColorEdit3("##val", ref vec, ImGuiColorEditFlags.Float)
                : ImGui.DragFloat3("##val", ref vec, 0.01f);
            if (edited)
            {
                _document.Properties[prop.name] = JsonSerializer.SerializeToElement(new[] { vec.X, vec.Y, vec.Z }, MaterialAssetDocumentContext.Default.SingleArray);
                MarkDirtyState();
            }
        }
        else if (typeSpan.Equals("float4", StringComparison.OrdinalIgnoreCase) || typeSpan.Equals("quaternion", StringComparison.OrdinalIgnoreCase))
        {
            var vec = val is float4 f4 ? new Vector4(f4.x, f4.y, f4.z, f4.w) : Vector4.Zero;
            var edited = IsColorProperty(prop.name)
                ? ImGui.ColorEdit4("##val", ref vec, ImGuiColorEditFlags.Float)
                : ImGui.DragFloat4("##val", ref vec, 0.01f);
            if (edited)
            {
                _document.Properties[prop.name] = JsonSerializer.SerializeToElement(new[] { vec.X, vec.Y, vec.Z, vec.W }, MaterialAssetDocumentContext.Default.SingleArray);
                MarkDirtyState();
            }
        }
        else if (typeSpan.Equals("int", StringComparison.OrdinalIgnoreCase))
        {
            var current = val is int iVal ? iVal : (val is IConvertible c ? c.ToInt32(null) : 0);
            if (ImGui.DragInt("##val", ref current, 1.0f))
            {
                _document.Properties[prop.name] = JsonSerializer.SerializeToElement(current, MaterialAssetDocumentContext.Default.Int32);
                MarkDirtyState();
            }
        }
        else if (typeSpan.Equals("uint", StringComparison.OrdinalIgnoreCase))
        {
            var current = val is uint uVal ? (int)Math.Min(int.MaxValue, uVal) : 0;
            if (ImGui.DragInt("##val", ref current, 1.0f, 0, int.MaxValue))
            {
                _document.Properties[prop.name] = JsonSerializer.SerializeToElement((uint)Math.Max(0, current), MaterialAssetDocumentContext.Default.UInt32);
                MarkDirtyState();
            }
        }
        else if (typeSpan.Equals("bool", StringComparison.OrdinalIgnoreCase))
        {
            var current = val is uint uVal ? uVal != 0 : (val is bool bVal && bVal);
            if (ImGui.Checkbox("##val", ref current))
            {
                _document.Properties[prop.name] = JsonSerializer.SerializeToElement(current, MaterialAssetDocumentContext.Default.Boolean);
                MarkDirtyState();
            }
        }
        else
        {
            ImGui.TextColored(tokens.TextDisabled, $"{prop.type} (unsupported)");
        }

        ImGui.PopID();
    }

    private void DrawTexturePropertyRow(PropertySemantic prop, in EditorColorTokens tokens)
    {
        ImGui.PushID(prop.name);
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(tokens.TextPrimary, prop.name);

        ImGui.TableSetColumnIndex(1);
        var currentGuid = _document.Textures.TryGetValue(prop.name, out var existing) ? existing : Guid.Empty;
        if (EditorObjectPickers.DrawAssetField(prop.name, currentGuid, AssetType.Texture, _assetCatalog, _projectService, in tokens, out var nextGuid, PingEntry))
        {
            if (nextGuid == Guid.Empty)
            {
                _document.Textures.Remove(prop.name);
            }
            else
            {
                _document.Textures[prop.name] = nextGuid;
            }

            MarkDirtyState();
        }

        ImGui.PopID();
    }

    private void DrawSamplerPropertyRow(PropertySemantic prop, in EditorColorTokens tokens)
    {
        ImGui.PushID(prop.name);
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(tokens.TextPrimary, prop.name);

        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(-1.0f);

        var currentSampler = MaterialPropertyPacker.ResolveSamplerType(prop, _document.Properties);
        var currentIndex = (int)currentSampler;
        var preview = currentSampler.ToString();

        if (ImGui.BeginCombo("##sampler", preview))
        {
            for (var i = 0; i < (int)StaticSamplerType.Count; i++)
            {
                var option = (StaticSamplerType)i;
                var isSelected = i == currentIndex;
                if (ImGui.Selectable(option.ToString(), isSelected))
                {
                    _document.Properties[prop.name] = JsonSerializer.SerializeToElement(option.ToString(), MaterialAssetDocumentContext.Default.String);
                    MarkDirtyState();
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }

        ImGui.PopID();
    }

    private void DrawFooter(in EditorColorTokens tokens)
    {
        if (ImGui.Button("Reset Defaults"))
        {
            _document.Properties.Clear();
            _document.Textures.Clear();
            MarkDirtyState();
            StatusMessage = "Reset properties and textures to shader defaults.";
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(!_isDirty);
        if (ImGui.Button("Revert"))
        {
            Revert();
        }

        ImGui.SameLine();
        if (ImGui.Button("Save"))
        {
            Save();
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Save & Reimport"))
        {
            if (!_isDirty || Save())
            {
                _assetImportService.EnqueueReimport(AssetPath, forceRebake: true);
                StatusMessage = "Saved and queued async reimport...";
            }
        }

        if (!string.IsNullOrEmpty(StatusMessage))
        {
            ImGui.TextColored(tokens.TextSecondary, StatusMessage);
        }
    }

    private object? ResolveCurrentValue(PropertySemantic prop)
    {
        if (_document.Properties.TryGetValue(prop.name, out var elem))
        {
            try
            {
                var parsed = MaterialPropertyPacker.ParseJsonElement(prop.type, elem);
                if (parsed != null)
                {
                    return parsed;
                }
            }
            catch
            {
            }
        }

        if (!string.IsNullOrWhiteSpace(prop.defaultValue))
        {
            return MaterialPropertyPacker.ParseDefaultValueString(prop.type, prop.defaultValue);
        }

        return null;
    }

    private void PingEntry(EditorAssetCatalog.Entry entry)
    {
        if (!string.IsNullOrEmpty(entry.FullSourcePath))
        {
            _onPingAsset?.Invoke(entry.FullSourcePath);
        }
    }

    private void MarkDirtyState()
    {
        _isDirty = !string.Equals(SerializeDocument(_document), _baselineJson, StringComparison.Ordinal);
    }

    private static string SerializeDocument(MaterialAssetDocument doc)
    {
        return JsonSerializer.Serialize(doc, MaterialAssetDocumentContext.Default.MaterialAssetDocument);
    }

    private static bool IsColorProperty(string propertyName)
    {
        return propertyName.Contains("color", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("tint", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("albedo", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("emission", StringComparison.OrdinalIgnoreCase);
    }
}
