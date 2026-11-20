using Ghost.Core;
using Ghost.Engine.Streaming;
using Ghost.Graphics.Core;
using Ghost.Graphics.Services;
using System.Reflection;

namespace Ghost.Editor.Core.Sync;

/// <summary>
/// Property node for <see cref="Identifier{MaterialPalette}"/> fields that stores per-slot material <see cref="Guid"/>s
/// and rebuilds the runtime <see cref="MaterialPalette"/> via <see cref="ResourceManager"/> on edit.
/// </summary>
public sealed class MaterialPalettePropertyNode : PropertyNode
{
    private readonly EditorSyncBridge _bridge;
    private readonly List<Guid> _materialGuids = new();
    private readonly List<IAssetEntry> _heldEntries = new();
    private readonly List<int> _heldVersions = new();
    private readonly List<Handle<Material>> _heldHandles = new();
    private readonly List<IAssetEntry> _entriesToRelease = new();

    private bool _pendingResolution;

    public IReadOnlyList<Guid> MaterialGuids => _materialGuids;

    public bool IsPendingLoad => _pendingResolution;

    public MaterialPalettePropertyNode(string name, Type propertyType, ComponentNode componentNode, object? value, Action<object?> onChange, EditorSyncBridge? bridge = null, bool isReadOnly = false, string? displayName = null, string? groupName = null, IReadOnlyList<Guid>? initialGuids = null)
        : base(name, propertyType, componentNode, value, onChange, isReadOnly, displayName, groupName)
    {
        _bridge = bridge ?? EditorSyncBridge.Instance;

        if (initialGuids != null)
        {
            for (var i = 0; i < initialGuids.Count; i++)
            {
                _materialGuids.Add(initialGuids[i]);
            }
        }
        else if (value is Identifier<MaterialPalette> paletteId && paletteId.IsValid && _bridge.ResourceManager != null)
        {
            SyncSlotCountFromPalette(paletteId);
        }

        _bridge.TrackResourceNode(this);
    }

    public override void ApplyValue(object? newValue)
    {
        if (newValue is IReadOnlyList<Guid> guidList)
        {
            SetMaterialGuids(guidList);
            return;
        }

        if (newValue is Identifier<MaterialPalette> paletteId)
        {
            Value = paletteId;
            OnChange(paletteId);
        }
    }

    public override void RefreshFromEcs(object boxedComponent, FieldInfo? field)
    {
        if (field == null)
        {
            return;
        }

        Value = field.GetValue(boxedComponent);
        if (_materialGuids.Count == 0 && Value is Identifier<MaterialPalette> paletteId && paletteId.IsValid && _bridge.ResourceManager != null)
        {
            SyncSlotCountFromPalette(paletteId);
        }
    }

    public override object? GetSerializableValue()
    {
        return _materialGuids.ToArray();
    }

    public override bool TryCompletePendingResolution()
    {
        DrainDeferredReleases();

        if (!_pendingResolution)
        {
            var needsPaletteRebuild = false;
            for (var i = 0; i < _heldEntries.Count; i++)
            {
                var entry = _heldEntries[i];
                if (entry.State != AssetState.Ready || i >= _heldVersions.Count || entry.ContentVersion == _heldVersions[i])
                {
                    continue;
                }

                _heldVersions[i] = entry.ContentVersion;
                var latestHandle = Handle<Material>.Invalid;
                entry.ReadAssetData(ref latestHandle);
                if (i >= _heldHandles.Count || latestHandle != _heldHandles[i])
                {
                    needsPaletteRebuild = true;
                }
            }

            if (needsPaletteRebuild)
            {
                CommitPalette();
                return true;
            }

            return false;
        }

        for (var i = 0; i < _heldEntries.Count; i++)
        {
            var entry = _heldEntries[i];
            if (entry.State == AssetState.Failed)
            {
                _pendingResolution = false;
                return false;
            }

            if (entry.State != AssetState.Ready)
            {
                return false;
            }
        }

        _pendingResolution = false;
        CommitPalette();
        return true;
    }

    public override void ReleaseResources()
    {
        for (var i = 0; i < _heldEntries.Count; i++)
        {
            _entriesToRelease.Add(_heldEntries[i]);
        }

        _heldEntries.Clear();
        _heldVersions.Clear();
        _heldHandles.Clear();
        _pendingResolution = false;

        DrainDeferredReleases();
        _entriesToRelease.Clear();
    }

    public override PropertyNode Clone(ComponentNode targetComponent)
    {
        return new MaterialPalettePropertyNode(Name, PropertyType, targetComponent, Value, OnChange, _bridge, IsReadOnly, DisplayName, GroupName, _materialGuids.ToArray());
    }

    private void SetMaterialGuids(IReadOnlyList<Guid> guids)
    {
        _materialGuids.Clear();
        for (var i = 0; i < guids.Count; i++)
        {
            _materialGuids.Add(guids[i]);
        }

        for (var i = 0; i < _heldEntries.Count; i++)
        {
            _entriesToRelease.Add(_heldEntries[i]);
        }
        _heldEntries.Clear();
        _heldVersions.Clear();
        _heldHandles.Clear();
        _pendingResolution = false;

        if (_materialGuids.Count == 0)
        {
            DrainDeferredReleases();
            var invalid = Identifier<MaterialPalette>.Invalid;
            Value = invalid;
            OnChange(invalid);
            return;
        }

        var assetManager = _bridge.AssetManager;
        if (assetManager == null || _bridge.ResourceManager == null)
        {
            return;
        }

        var allReady = true;
        for (var i = 0; i < _materialGuids.Count; i++)
        {
            var guid = _materialGuids[i];
            if (guid == Guid.Empty)
            {
                continue;
            }

            if (!assetManager.TryResolveAsset(guid, out var entry))
            {
                Logger.Warning($"MaterialPalettePropertyNode '{Name}': material '{guid}' was not found in AssetManager.");
                continue;
            }

            if (entry.AssetType != AssetType.Material)
            {
                Logger.Warning($"MaterialPalettePropertyNode '{Name}': asset '{guid}' is {entry.AssetType}, expected Material.");
                _entriesToRelease.Add(entry);
                continue;
            }

            _heldEntries.Add(entry);
            if (entry.State != AssetState.Ready)
            {
                allReady = false;
            }
        }

        if (allReady)
        {
            CommitPalette();
            DrainDeferredReleases();
        }
        else
        {
            _pendingResolution = true;
        }
    }

    private void CommitPalette()
    {
        var rm = _bridge.ResourceManager;
        if (rm == null)
        {
            return;
        }

        _heldVersions.Clear();
        _heldHandles.Clear();

        var handles = new Handle<Material>[_materialGuids.Count];
        var entryIndex = 0;
        for (var i = 0; i < _materialGuids.Count; i++)
        {
            var guid = _materialGuids[i];
            if (guid != Guid.Empty && entryIndex < _heldEntries.Count && _heldEntries[entryIndex].AssetId == guid)
            {
                var entry = _heldEntries[entryIndex++];
                var matHandle = Handle<Material>.Invalid;
                if (entry.State == AssetState.Ready)
                {
                    entry.ReadAssetData(ref matHandle);
                }

                handles[i] = matHandle;
                _heldVersions.Add(entry.ContentVersion);
                _heldHandles.Add(matHandle);
            }
        }

        var paletteIndex = rm.GetOrCreateMaterialPalette(handles);
        var nextPalette = new Identifier<MaterialPalette>(paletteIndex);
        Value = nextPalette;
        OnChange(nextPalette);
    }

    private void SyncSlotCountFromPalette(Identifier<MaterialPalette> paletteId)
    {
        var rm = _bridge.ResourceManager;
        if (rm == null || !paletteId.IsValid)
        {
            return;
        }

        var info = rm.GetMaterialPaletteInfo(paletteId);
        var count = info.materials.Length;
        for (var i = _materialGuids.Count; i < count; i++)
        {
            _materialGuids.Add(Guid.Empty);
        }
    }

    private void DrainDeferredReleases()
    {
        for (var i = _entriesToRelease.Count - 1; i >= 0; i--)
        {
            var entry = _entriesToRelease[i];
            if (entry.State == AssetState.Ready)
            {
                entry.Release();
                _entriesToRelease.RemoveAt(i);
            }
        }
    }
}
