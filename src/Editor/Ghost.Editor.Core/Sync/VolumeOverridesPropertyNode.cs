using Ghost.AssetForge.Core.Models;
using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Entities;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Ghost.Editor.Core.Sync;

/// <summary>
/// Represents a single field inside an active <see cref="VolumeOverrideEntry"/>, pairing its override flag with a backing <see cref="PropertyNode"/>.
/// </summary>
public sealed class VolumeOverrideField
{
    public FieldInfo Field { get; }
    public string Name => Field.Name;
    public string DisplayName { get; }
    public Type ValueType { get; }
    public bool IsNullable { get; }
    public bool IsHardcut { get; }
    public object DefaultFieldValue { get; }
    public bool IsOverridden { get; internal set; }
    public PropertyNode ValueNode { get; }

    internal VolumeOverrideField(FieldInfo field, string displayName, Type valueType, bool isNullable, bool isHardcut, object defaultFieldValue, bool isOverridden, PropertyNode valueNode)
    {
        Field = field;
        DisplayName = displayName;
        ValueType = valueType;
        IsNullable = isNullable;
        IsHardcut = isHardcut;
        DefaultFieldValue = defaultFieldValue;
        IsOverridden = isOverridden;
        ValueNode = valueNode;
    }
}

/// <summary>
/// Managed editor representation of one <see cref="Ghost.Engine.Volumes.IVolumeComponent{T}"/> stored in a <see cref="RenderVolumeDataContainer"/>.
/// </summary>
public sealed class VolumeOverrideEntry
{
    internal readonly List<VolumeOverrideField> _fields;

    public IVolumeOverrideAccessor Accessor { get; }
    public Type SettingsType => Accessor.SettingsType;
    public string DisplayName => Accessor.DisplayName;
    public object BoxedValue { get; internal set; }
    public IReadOnlyList<VolumeOverrideField> Fields => _fields;
    public bool IsExpanded { get; set; }

    internal VolumeOverrideEntry(IVolumeOverrideAccessor accessor, object boxedValue, List<VolumeOverrideField> fields, bool isExpanded = false)
    {
        Accessor = accessor;
        BoxedValue = boxedValue;
        _fields = fields;
        IsExpanded = isExpanded;
    }

    public VolumeOverrideField? FindField(string fieldName)
    {
        for (var i = 0; i < _fields.Count; i++)
        {
            if (string.Equals(_fields[i].Name, fieldName, StringComparison.Ordinal))
            {
                return _fields[i];
            }
        }

        return null;
    }
}

/// <summary>
/// Managed mirror and ECS synchronization bridge for the unmanaged overrides held by a <see cref="RenderVolumeDataContainer"/>.
/// Never caches native buffer pointers; all runtime updates are marshaled safely onto the simulation thread.
/// </summary>
public sealed unsafe class VolumeOverridesPropertyNode : PropertyNode
{
    private readonly EditorSyncBridge _bridge;
    private readonly List<VolumeOverrideEntry> _overrides = new();
    private bool _suppressInnerCallback;

    public IReadOnlyList<VolumeOverrideEntry> Overrides => _overrides;

    public VolumeOverridesPropertyNode(ComponentNode componentNode, EditorSyncBridge? bridge = null)
        : base("Overrides", typeof(RenderVolumeDataContainer), componentNode, null, _ => { })
    {
        _bridge = bridge ?? EditorSyncBridge.Instance;
    }

    public VolumeOverrideEntry? FindOverride(Type settingsType)
    {
        for (var i = 0; i < _overrides.Count; i++)
        {
            if (_overrides[i].SettingsType == settingsType)
            {
                return _overrides[i];
            }
        }

        return null;
    }

    public int IndexOfOverride(Type settingsType)
    {
        for (var i = 0; i < _overrides.Count; i++)
        {
            if (_overrides[i].SettingsType == settingsType)
            {
                return i;
            }
        }

        return -1;
    }

    public bool HasOverride(Type settingsType) => IndexOfOverride(settingsType) >= 0;

    /// <summary>
    /// Refreshes the managed override mirror from a live <see cref="RenderVolumeDataContainer"/> without clobbering UI state or authoring asset GUIDs.
    /// </summary>
    public void RefreshFromContainer(in RenderVolumeDataContainer container)
    {
        var entries = container.Entries;
        if (MatchesOrder(entries))
        {
            for (var i = 0; i < _overrides.Count; i++)
            {
                var ovr = _overrides[i];
                if (ovr.Accessor.TryRead(in container, out var boxed))
                {
                    SyncEntryFromBoxed(ovr, boxed);
                }
            }

            return;
        }

        var previousByType = new Dictionary<Type, VolumeOverrideEntry>(_overrides.Count);
        for (var i = 0; i < _overrides.Count; i++)
        {
            previousByType[_overrides[i].SettingsType] = _overrides[i];
        }

        _overrides.Clear();
        for (var i = 0; i < entries.Length; i++)
        {
            if (!VolumeTypeRegistry.TryGetAccessor(entries[i].typeId, out var accessor))
            {
                continue;
            }

            if (!accessor.TryRead(in container, out var boxed))
            {
                continue;
            }

            if (previousByType.Remove(accessor.SettingsType, out var reused))
            {
                SyncEntryFromBoxed(reused, boxed);
                _overrides.Add(reused);
            }
            else
            {
                _overrides.Add(CreateEntry(accessor, boxed, isExpanded: false));
            }
        }

        foreach (var stale in previousByType.Values)
        {
            ReleaseEntryResources(stale);
        }
    }

    /// <summary>
    /// Adds or replaces a volume override on this node and schedules the ECS container update.
    /// </summary>
    public VolumeOverrideEntry SetOverride(Type settingsType, object? boxedValue = null, int? insertIndex = null, bool? expanded = null, IReadOnlyDictionary<string, Guid>? assetGuids = null)
    {
        if (!VolumeTypeRegistry.TryGetAccessor(settingsType, out var accessor))
        {
            throw new ArgumentException($"Type '{settingsType.Name}' is not a registered IVolumeComponent.", nameof(settingsType));
        }

        var resolvedValue = boxedValue ?? accessor.EmptyValue;
        var existingIdx = IndexOfOverride(settingsType);
        VolumeOverrideEntry entry;

        if (existingIdx >= 0)
        {
            entry = _overrides[existingIdx];
            if (expanded.HasValue)
            {
                entry.IsExpanded = expanded.Value;
            }

            SyncEntryFromBoxed(entry, resolvedValue);
        }
        else
        {
            entry = CreateEntry(accessor, resolvedValue, expanded ?? true);
            var targetIdx = insertIndex.HasValue ? Math.Clamp(insertIndex.Value, 0, _overrides.Count) : _overrides.Count;
            _overrides.Insert(targetIdx, entry);
        }

        if (assetGuids != null)
        {
            ApplyAssetGuids(entry, assetGuids);
        }

        PushOverrideToEcs(entry);
        return entry;
    }

    /// <summary>
    /// Updates the override state and inner value of a single field on an existing volume override and pushes it to ECS.
    /// </summary>
    public void SetFieldState(Type settingsType, string fieldName, bool isOverridden, object? value)
    {
        var entry = FindOverride(settingsType);
        if (entry == null)
        {
            return;
        }

        var field = entry.FindField(fieldName);
        if (field == null)
        {
            return;
        }

        field.IsOverridden = !field.IsNullable || isOverridden;

        if (field.ValueNode is AssetPropertyNode assetNode)
        {
            if (value is Guid guid && guid != assetNode.AssetGuid)
            {
                assetNode.ApplyValue(guid);
                UpdateBoxedFieldFromNode(entry, field);
                PushOverrideToEcs(entry);
                return;
            }

            if (value is not Guid && value != null && assetNode.PropertyType.IsInstanceOfType(value))
            {
                assetNode.ApplyValue(value);
                UpdateBoxedFieldFromNode(entry, field);
                PushOverrideToEcs(entry);
                return;
            }

            UpdateBoxedFieldFromNode(entry, field);
            PushOverrideToEcs(entry);
            return;
        }

        if (value != null)
        {
            _suppressInnerCallback = true;
            try
            {
                field.ValueNode.ApplyValue(value);
            }
            finally
            {
                _suppressInnerCallback = false;
            }
        }

        UpdateBoxedFieldFromNode(entry, field);
        PushOverrideToEcs(entry);
    }

    /// <summary>
    /// Removes a volume override by type from this node and schedules removal from the ECS container.
    /// </summary>
    public bool RemoveOverride(Type settingsType)
    {
        var idx = IndexOfOverride(settingsType);
        if (idx < 0)
        {
            return false;
        }

        var entry = _overrides[idx];
        _overrides.RemoveAt(idx);
        ReleaseEntryResources(entry);

        var entity = ComponentNode.EntityNode.EcsEntity;
        if (entity.IsValid)
        {
            var accessor = entry.Accessor;
            _bridge.EnqueueWorldAction(world =>
            {
                if (!world.EntityManager.Exists(entity))
                {
                    return;
                }

                if (TryGetContainerPtr(world, entity, out var pContainer))
                {
                    accessor.Remove(ref *pContainer);
                }
            });
        }

        return true;
    }

    /// <summary>
    /// Captures the asset GUIDs for any <see cref="Handle{T}"/> fields on <paramref name="entry"/> so undo/redo can restore assets losslessly.
    /// </summary>
    public static Dictionary<string, Guid>? CaptureAssetGuids(VolumeOverrideEntry entry)
    {
        Dictionary<string, Guid>? guids = null;
        for (var i = 0; i < entry._fields.Count; i++)
        {
            if (entry._fields[i].ValueNode is AssetPropertyNode assetNode && assetNode.AssetGuid != Guid.Empty)
            {
                guids ??= new Dictionary<string, Guid>();
                guids[entry._fields[i].Name] = assetNode.AssetGuid;
            }
        }

        return guids;
    }

    /// <summary>
    /// Writes every active override in this node into <paramref name="container"/>.
    /// </summary>
    public void WriteToContainer(ref RenderVolumeDataContainer container)
    {
        for (var i = 0; i < _overrides.Count; i++)
        {
            var ovr = _overrides[i];
            ovr.Accessor.Write(ref container, ovr.BoxedValue);
        }
    }

    public override void ApplyValue(object? newValue)
    {
        if (newValue is not IReadOnlyList<(Type Type, object Value, Dictionary<string, Guid>? AssetGuids)> snapshot)
        {
            return;
        }

        for (var i = _overrides.Count - 1; i >= 0; i--)
        {
            RemoveOverride(_overrides[i].SettingsType);
        }

        for (var i = 0; i < snapshot.Count; i++)
        {
            var (type, val, assetGuids) = snapshot[i];
            SetOverride(type, val, null, false, assetGuids);
        }
    }

    public override void RefreshFromEcs(object boxedComponent, FieldInfo? field)
    {
    }

    public override object? GetSerializableValue()
    {
        var snapshot = new List<(Type Type, object Value, Dictionary<string, Guid>? AssetGuids)>(_overrides.Count);
        for (var i = 0; i < _overrides.Count; i++)
        {
            var ovr = _overrides[i];
            snapshot.Add((ovr.SettingsType, ovr.BoxedValue, CaptureAssetGuids(ovr)));
        }

        return snapshot;
    }

    public List<SceneVolumeOverrideRecord> SerializeOverrides(JsonSerializerOptions options)
    {
        var result = new List<SceneVolumeOverrideRecord>(_overrides.Count);
        for (var i = 0; i < _overrides.Count; i++)
        {
            var ovr = _overrides[i];
            var record = new SceneVolumeOverrideRecord
            {
                Type = ovr.SettingsType.FullName ?? ovr.SettingsType.Name
            };

            for (var f = 0; f < ovr._fields.Count; f++)
            {
                var field = ovr._fields[f];
                if (!field.IsOverridden)
                {
                    continue;
                }

                var elem = field.ValueNode.SerializeToJson(options);
                if (elem.ValueKind != JsonValueKind.Undefined)
                {
                    record.Fields[field.Name] = elem;
                }
            }

            result.Add(record);
        }

        return result;
    }

    public void ApplySerializedOverrides(IReadOnlyList<SceneVolumeOverrideRecord>? records, JsonSerializerOptions options)
    {
        for (var i = _overrides.Count - 1; i >= 0; i--)
        {
            RemoveOverride(_overrides[i].SettingsType);
        }

        if (records == null)
        {
            return;
        }

        for (var i = 0; i < records.Count; i++)
        {
            var rec = records[i];
            if (!VolumeTypeRegistry.TryGetAccessor(rec.Type, out var accessor))
            {
                continue;
            }

            var entry = SetOverride(accessor.SettingsType, null, null, false);
            foreach (var kvp in rec.Fields)
            {
                var field = entry.FindField(kvp.Key);
                if (field == null)
                {
                    continue;
                }

                field.IsOverridden = true;
                field.ValueNode.DeserializeFromJson(kvp.Value, options);
                UpdateBoxedFieldFromNode(entry, field);
            }

            PushOverrideToEcs(entry);
        }
    }

    public override void WriteToComponentRecord(SceneComponentRecord record, JsonSerializerOptions options)
    {
        record.VolumeOverrides = SerializeOverrides(options);
    }

    public override void ReadFromComponentRecord(SceneComponentRecord record, JsonSerializerOptions options)
    {
        ApplySerializedOverrides(record.VolumeOverrides, options);
    }

    public override bool TryCompletePendingResolution()
    {
        var resolvedAny = false;
        for (var i = 0; i < _overrides.Count; i++)
        {
            var fields = _overrides[i]._fields;
            for (var f = 0; f < fields.Count; f++)
            {
                if (fields[f].ValueNode.TryCompletePendingResolution())
                {
                    resolvedAny = true;
                }
            }
        }

        return resolvedAny;
    }

    public override void ReleaseResources()
    {
        for (var i = 0; i < _overrides.Count; i++)
        {
            ReleaseEntryResources(_overrides[i]);
        }
    }

    public override PropertyNode Clone(ComponentNode targetComponent)
    {
        var clone = new VolumeOverridesPropertyNode(targetComponent, _bridge);
        for (var i = 0; i < _overrides.Count; i++)
        {
            var src = _overrides[i];
            var guids = CaptureAssetGuids(src);
            var clonedEntry = clone.CreateEntry(src.Accessor, src.BoxedValue, src.IsExpanded);
            if (guids != null)
            {
                clone.ApplyAssetGuids(clonedEntry, guids);
            }

            clone._overrides.Add(clonedEntry);
        }

        return clone;
    }

    private VolumeOverrideEntry CreateEntry(IVolumeOverrideAccessor accessor, object boxedValue, bool isExpanded)
    {
        var fieldsMeta = accessor.SettingsType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        var defaultStruct = accessor.DefaultValue;
        var fields = new List<VolumeOverrideField>(fieldsMeta.Length);
        VolumeOverrideEntry? entry = null;

        for (var i = 0; i < fieldsMeta.Length; i++)
        {
            var f = fieldsMeta[i];
            if (f.GetCustomAttribute<HideInInspectorAttribute>() != null || HasHideEditorAttribute(f))
            {
                continue;
            }

            var nullableInner = Nullable.GetUnderlyingType(f.FieldType);
            var isNullable = nullableInner != null;
            var valueType = nullableInner ?? f.FieldType;
            var isHardcut = f.GetCustomAttribute<HardcutAttribute>() != null;
            var displayName = f.GetCustomAttribute<InspectorNameAttribute>()?.Name ?? PrettifyFieldName(f.Name);
            var defaultVal = f.GetValue(defaultStruct) ?? Activator.CreateInstance(valueType)!;
            var rawVal = f.GetValue(boxedValue);
            var isOverridden = !isNullable || rawVal != null;
            var initVal = rawVal ?? defaultVal;

            var capturedFieldName = f.Name;
            Action<object?> onChange = newInnerValue =>
            {
                if (_suppressInnerCallback || entry == null)
                {
                    return;
                }

                var targetField = entry.FindField(capturedFieldName);
                if (targetField == null)
                {
                    return;
                }

                if (targetField.IsOverridden)
                {
                    var boxed = entry.BoxedValue;
                    targetField.Field.SetValue(boxed, newInnerValue ?? targetField.DefaultFieldValue);
                    entry.BoxedValue = boxed;
                    PushOverrideToEcs(entry);
                }
            };

            var valueNode = valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Handle<>)
                ? new AssetPropertyNode(f.Name, valueType, ComponentNode, initVal, onChange, _bridge, displayName: displayName)
                : (PropertyNode)new ValuePropertyNode(f.Name, valueType, ComponentNode, initVal, onChange, displayName: displayName);

            fields.Add(new VolumeOverrideField(f, displayName, valueType, isNullable, isHardcut, defaultVal, isOverridden, valueNode));
        }

        entry = new VolumeOverrideEntry(accessor, boxedValue, fields, isExpanded);
        return entry;
    }

    private void SyncEntryFromBoxed(VolumeOverrideEntry entry, object boxedValue)
    {
        entry.BoxedValue = boxedValue;
        _suppressInnerCallback = true;
        try
        {
            for (var i = 0; i < entry._fields.Count; i++)
            {
                var field = entry._fields[i];
                var rawVal = field.Field.GetValue(boxedValue);
                var keepPendingAssetOverride = field.ValueNode is AssetPropertyNode assetNode && (assetNode.IsPendingLoad || assetNode.AssetGuid != Guid.Empty);
                field.IsOverridden = !field.IsNullable || rawVal != null || (field.IsOverridden && keepPendingAssetOverride);
                if (field.IsOverridden && rawVal != null)
                {
                    field.ValueNode.Value = rawVal;
                }
            }
        }
        finally
        {
            _suppressInnerCallback = false;
        }
    }

    private void ApplyAssetGuids(VolumeOverrideEntry entry, IReadOnlyDictionary<string, Guid> assetGuids)
    {
        foreach (var pair in assetGuids)
        {
            var field = entry.FindField(pair.Key);
            if (field?.ValueNode is AssetPropertyNode assetNode)
            {
                assetNode.ApplyValue(pair.Value);
            }
        }
    }

    private static void UpdateBoxedFieldFromNode(VolumeOverrideEntry entry, VolumeOverrideField field)
    {
        var boxed = entry.BoxedValue;
        field.Field.SetValue(boxed, field.IsOverridden ? (field.ValueNode.Value ?? field.DefaultFieldValue) : null);
        entry.BoxedValue = boxed;
    }

    private void PushOverrideToEcs(VolumeOverrideEntry entry)
    {
        var entity = ComponentNode?.EntityNode.EcsEntity ?? Entity.Invalid;
        if (!entity.IsValid)
        {
            return;
        }

        var accessor = entry.Accessor;
        var boxedSnapshot = entry.BoxedValue;
        _bridge.EnqueueWorldAction(world =>
        {
            var em = world.EntityManager;
            if (!em.Exists(entity))
            {
                return;
            }

            if (!TryGetContainerPtr(world, entity, out var pContainer))
            {
                var created = RenderVolumeDataContainer.Create();
                accessor.Write(ref created, boxedSnapshot);
                em.AddComponent(entity, in created);
                return;
            }

            accessor.Write(ref *pContainer, boxedSnapshot);
        });
    }

    private bool MatchesOrder(ReadOnlySpan<VolumeDataEntry> entries)
    {
        if (entries.Length != _overrides.Count)
        {
            return false;
        }

        for (var i = 0; i < entries.Length; i++)
        {
            if (entries[i].typeId != _overrides[i].Accessor.TypeId)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryGetContainerPtr(World world, Entity entity, out RenderVolumeDataContainer* pContainer)
    {
        pContainer = null;
        var compId = ComponentTypeID<RenderVolumeDataContainer>.Value;
        if (!world.EntityManager.TryGetEntityLocation(entity, out var location))
        {
            return false;
        }

        ref var archetype = ref world.ComponentManager.GetArchetypeReference(location.archetypeID);
        if (!archetype.HasComponent(compId))
        {
            return false;
        }

        pContainer = (RenderVolumeDataContainer*)archetype.GetComponentData(location.chunkIndex, location.rowIndex, compId);
        return pContainer != null;
    }

    private static void ReleaseEntryResources(VolumeOverrideEntry entry)
    {
        for (var i = 0; i < entry._fields.Count; i++)
        {
            entry._fields[i].ValueNode.ReleaseResources();
        }
    }

    private static string PrettifyFieldName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i == 0)
            {
                sb.Append(char.ToUpperInvariant(c));
                continue;
            }

            if (char.IsUpper(c))
            {
                var prev = name[i - 1];
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                if (!char.IsUpper(prev) || (nextIsLower && !char.IsDigit(prev)))
                {
                    sb.Append(' ');
                }
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    private static bool HasHideEditorAttribute(MemberInfo member)
    {
        var attrs = member.GetCustomAttributes(inherit: false);
        for (var i = 0; i < attrs.Length; i++)
        {
            if (attrs[i].GetType().Name == "HideEditorAttribute")
            {
                return true;
            }
        }

        return false;
    }
}
