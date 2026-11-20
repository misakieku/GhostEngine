using Ghost.Core;
using Ghost.Engine.Streaming;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using System.Reflection;

namespace Ghost.Editor.Core.Sync;

/// <summary>
/// Property node for <see cref="Handle{T}"/> fields that stores a persistent authoring asset <see cref="Guid"/>
/// and resolves the runtime <see cref="Handle{T}"/> via <see cref="AssetManager"/>.
/// </summary>
public sealed class AssetPropertyNode : PropertyNode
{
    private interface IHandleAccessor
    {
        object InvalidHandle { get; }
        object ReadHandle(IAssetEntry entry);
        bool IsValidHandle(object? value);
    }

    private sealed class TypedHandleAccessor<T> : IHandleAccessor
        where T : struct
    {
        public object InvalidHandle => Handle<T>.Invalid;

        public object ReadHandle(IAssetEntry entry)
        {
            var handle = Handle<T>.Invalid;
            entry.ReadAssetData(ref handle);
            return handle;
        }

        public bool IsValidHandle(object? value)
        {
            return value is Handle<T> handle && handle.IsValid;
        }
    }

    private readonly EditorSyncBridge _bridge;
    private readonly IHandleAccessor _handleAccessor;
    private readonly List<IAssetEntry> _entriesToRelease = new();

    private IAssetEntry? _heldEntry;
    private IAssetEntry? _pendingEntry;

    public Type ResourceType { get; }

    public AssetType ExpectedAssetType { get; }

    public Guid AssetGuid { get; private set; }

    public bool IsPendingLoad => _pendingEntry != null;

    public AssetPropertyNode(string name, Type propertyType, ComponentNode componentNode, object? value, Action<object?> onChange, EditorSyncBridge? bridge = null, bool isReadOnly = false, string? displayName = null, string? groupName = null, Guid initialGuid = default)
        : base(name, propertyType, componentNode, value, onChange, isReadOnly, displayName, groupName)
    {
        _bridge = bridge ?? EditorSyncBridge.Instance;
        ResourceType = propertyType.IsGenericType ? propertyType.GetGenericArguments()[0] : propertyType;
        ExpectedAssetType = ResolveAssetType(ResourceType);
        AssetGuid = initialGuid;

        var accessorType = typeof(TypedHandleAccessor<>).MakeGenericType(ResourceType);
        _handleAccessor = (IHandleAccessor)Activator.CreateInstance(accessorType)!;
        _bridge.TrackResourceNode(this);
    }

    public override void ApplyValue(object? newValue)
    {
        if (newValue is Guid newGuid)
        {
            SetAssetGuid(newGuid);
            return;
        }

        if (newValue == null)
        {
            SetAssetGuid(Guid.Empty);
            return;
        }

        if (PropertyType.IsInstanceOfType(newValue))
        {
            Value = newValue;
            OnChange(newValue);
        }
    }

    public override void RefreshFromEcs(object boxedComponent, FieldInfo? field)
    {
        if (field != null)
        {
            Value = field.GetValue(boxedComponent);
        }
    }

    public override object? GetSerializableValue()
    {
        return AssetGuid;
    }

    public override bool TryCompletePendingResolution()
    {
        DrainDeferredReleases();

        if (_pendingEntry == null)
        {
            return false;
        }

        var state = _pendingEntry.State;
        if (state == AssetState.Failed)
        {
            _entriesToRelease.Add(_pendingEntry);
            _pendingEntry = null;
            return false;
        }

        if (state != AssetState.Ready)
        {
            return false;
        }

        var readyEntry = _pendingEntry;
        _pendingEntry = null;

        if (_heldEntry != null)
        {
            _entriesToRelease.Add(_heldEntry);
        }

        _heldEntry = readyEntry;
        CommitResolvedEntry(readyEntry);
        DrainDeferredReleases();
        return true;
    }

    public override void ReleaseResources()
    {
        if (_heldEntry != null)
        {
            _entriesToRelease.Add(_heldEntry);
            _heldEntry = null;
        }

        if (_pendingEntry != null)
        {
            _entriesToRelease.Add(_pendingEntry);
            _pendingEntry = null;
        }

        DrainDeferredReleases();
        _entriesToRelease.Clear();
    }

    public override PropertyNode Clone(ComponentNode targetComponent)
    {
        var copy = new AssetPropertyNode(Name, PropertyType, targetComponent, Value, OnChange, _bridge, IsReadOnly, DisplayName, GroupName, AssetGuid);
        return copy;
    }

    private void SetAssetGuid(Guid newGuid)
    {
        AssetGuid = newGuid;
        if (_pendingEntry != null)
        {
            _entriesToRelease.Add(_pendingEntry);
            _pendingEntry = null;
        }

        if (newGuid == Guid.Empty)
        {
            if (_heldEntry != null)
            {
                _entriesToRelease.Add(_heldEntry);
                _heldEntry = null;
            }

            DrainDeferredReleases();
            var invalid = _handleAccessor.InvalidHandle;
            Value = invalid;
            OnChange(invalid);
            return;
        }

        var assetManager = _bridge.AssetManager;
        if (assetManager == null)
        {
            return;
        }

        if (!assetManager.TryResolveAsset(newGuid, out var entry))
        {
            Logger.Warning($"AssetPropertyNode '{Name}': asset '{newGuid}' was not found in AssetManager.");
            return;
        }

        if (ExpectedAssetType != AssetType.Unknown && entry.AssetType != ExpectedAssetType)
        {
            Logger.Warning($"AssetPropertyNode '{Name}': asset '{newGuid}' has type {entry.AssetType}, expected {ExpectedAssetType}.");
            _entriesToRelease.Add(entry);
            DrainDeferredReleases();
            return;
        }

        if (entry.State == AssetState.Ready)
        {
            if (_heldEntry != null)
            {
                _entriesToRelease.Add(_heldEntry);
            }

            _heldEntry = entry;
            CommitResolvedEntry(entry);
            DrainDeferredReleases();
        }
        else
        {
            _pendingEntry = entry;
        }
    }

    private void CommitResolvedEntry(IAssetEntry entry)
    {
        var handle = _handleAccessor.ReadHandle(entry);
        Value = handle;
        OnChange(handle);
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

    public static AssetType ResolveAssetType(Type resourceType)
    {
        if (resourceType == typeof(Mesh))
        {
            return AssetType.Mesh;
        }

        if (resourceType == typeof(GPUTexture))
        {
            return AssetType.Texture;
        }

        if (resourceType == typeof(Material))
        {
            return AssetType.Material;
        }

        if (resourceType == typeof(Shader))
        {
            return AssetType.Shader;
        }

        return AssetType.Unknown;
    }
}
