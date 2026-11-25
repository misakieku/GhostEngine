using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;

namespace Ghost.Editor.Core.Commands;

/// <summary>
/// Undoable command that adds a <see cref="Ghost.Engine.Volumes.IVolumeComponent{T}"/> override entry to a <see cref="VolumeOverridesPropertyNode"/>.
/// </summary>
public sealed class AddVolumeOverrideCommand : IEditorCommand
{
    private readonly VolumeOverridesPropertyNode _node;
    private readonly Type _settingsType;
    private readonly string _displayName;
    private readonly object? _initialValue;
    private readonly IReadOnlyDictionary<string, Guid>? _assetGuids;
    private int _insertedIndex = -1;

    public ushort? TargetSceneId => _node.ComponentNode?.EntityNode?.SceneId;

    public string Name => $"Add {_displayName} Override";

    public AddVolumeOverrideCommand(VolumeOverridesPropertyNode node, Type settingsType, object? initialValue = null, IReadOnlyDictionary<string, Guid>? assetGuids = null)
    {
        _node = node;
        _settingsType = settingsType;
        _displayName = VolumeTypeRegistry.TryGetAccessor(settingsType, out var accessor) ? accessor.DisplayName : settingsType.Name;
        _initialValue = initialValue;
        _assetGuids = assetGuids;
    }

    public void Execute()
    {
        if (_node.HasOverride(_settingsType))
        {
            return;
        }

        var entry = _node.SetOverride(_settingsType, _initialValue, _insertedIndex >= 0 ? _insertedIndex : null, expanded: true, assetGuids: _assetGuids);
        _insertedIndex = _node.IndexOfOverride(entry.SettingsType);
        SelectOwnerEntity();
    }

    public void Undo()
    {
        _insertedIndex = _node.IndexOfOverride(_settingsType);
        _node.RemoveOverride(_settingsType);
        SelectOwnerEntity();
    }

    public void Redo() => Execute();

    private void SelectOwnerEntity()
    {
        if (_node.ComponentNode?.EntityNode != null)
        {
            EditorSelection.Instance.Select(_node.ComponentNode.EntityNode);
        }
    }
}

/// <summary>
/// Undoable command that removes a <see cref="Ghost.Engine.Volumes.IVolumeComponent{T}"/> override entry from a <see cref="VolumeOverridesPropertyNode"/>.
/// </summary>
public sealed class RemoveVolumeOverrideCommand : IEditorCommand
{
    private readonly VolumeOverridesPropertyNode _node;
    private readonly Type _settingsType;
    private readonly string _displayName;
    private object? _removedValue;
    private Dictionary<string, Guid>? _removedAssetGuids;
    private int _removedIndex = -1;
    private bool _wasExpanded;

    public ushort? TargetSceneId => _node.ComponentNode?.EntityNode?.SceneId;

    public string Name => $"Remove {_displayName} Override";

    public RemoveVolumeOverrideCommand(VolumeOverridesPropertyNode node, Type settingsType)
    {
        _node = node;
        _settingsType = settingsType;
        _displayName = VolumeTypeRegistry.TryGetAccessor(settingsType, out var accessor) ? accessor.DisplayName : settingsType.Name;
    }

    public void Execute()
    {
        var idx = _node.IndexOfOverride(_settingsType);
        if (idx < 0)
        {
            return;
        }

        var existing = _node.Overrides[idx];
        _removedIndex = idx;
        _removedValue = existing.BoxedValue;
        _wasExpanded = existing.IsExpanded;
        _removedAssetGuids = VolumeOverridesPropertyNode.CaptureAssetGuids(existing);

        _node.RemoveOverride(_settingsType);
        SelectOwnerEntity();
    }

    public void Undo()
    {
        if (_removedIndex < 0)
        {
            return;
        }

        _node.SetOverride(_settingsType, _removedValue, _removedIndex, _wasExpanded, _removedAssetGuids);
        SelectOwnerEntity();
    }

    public void Redo() => Execute();

    private void SelectOwnerEntity()
    {
        if (_node.ComponentNode?.EntityNode != null)
        {
            EditorSelection.Instance.Select(_node.ComponentNode.EntityNode);
        }
    }
}

/// <summary>
/// Undoable command that replaces the entire boxed override struct (used for All/None/Reset actions).
/// </summary>
public sealed class SetVolumeOverrideBoxedCommand : IEditorCommand
{
    private readonly VolumeOverridesPropertyNode _node;
    private readonly Type _settingsType;
    private readonly object _oldBoxedValue;
    private readonly object _newBoxedValue;
    private readonly string _actionLabel;

    public ushort? TargetSceneId => _node.ComponentNode?.EntityNode?.SceneId;

    public string Name => _actionLabel;

    public SetVolumeOverrideBoxedCommand(VolumeOverridesPropertyNode node, Type settingsType, object oldBoxedValue, object newBoxedValue, string actionLabel)
    {
        _node = node;
        _settingsType = settingsType;
        _oldBoxedValue = oldBoxedValue;
        _newBoxedValue = newBoxedValue;
        _actionLabel = actionLabel;
    }

    public void Execute()
    {
        _node.SetOverride(_settingsType, _newBoxedValue);
        SelectOwnerEntity();
    }

    public void Undo()
    {
        _node.SetOverride(_settingsType, _oldBoxedValue);
        SelectOwnerEntity();
    }

    public void Redo() => Execute();

    private void SelectOwnerEntity()
    {
        if (_node.ComponentNode?.EntityNode != null)
        {
            EditorSelection.Instance.Select(_node.ComponentNode.EntityNode);
        }
    }
}

/// <summary>
/// Undoable command that modifies a single parameter's override flag or value inside a volume override struct, merging rapid drag edits.
/// </summary>
public sealed class ModifyVolumeFieldCommand : IEditorCommand
{
    private readonly VolumeOverridesPropertyNode _node;
    private readonly Type _settingsType;
    private readonly string _fieldName;
    private readonly bool _oldOverridden;
    private readonly object? _oldValue;
    private bool _newOverridden;
    private object? _newValue;
    private DateTime _timestamp;

    public ushort? TargetSceneId => _node.ComponentNode?.EntityNode?.SceneId;

    public string Name => $"Set {_settingsType.Name}.{_fieldName}";

    public ModifyVolumeFieldCommand(VolumeOverridesPropertyNode node, Type settingsType, string fieldName, bool oldOverridden, object? oldValue, bool newOverridden, object? newValue)
    {
        _node = node;
        _settingsType = settingsType;
        _fieldName = fieldName;
        _oldOverridden = oldOverridden;
        _oldValue = oldValue;
        _newOverridden = newOverridden;
        _newValue = newValue;
        _timestamp = DateTime.UtcNow;
    }

    public void Execute()
    {
        _node.SetFieldState(_settingsType, _fieldName, _newOverridden, _newValue);
    }

    public void Undo()
    {
        _node.SetFieldState(_settingsType, _fieldName, _oldOverridden, _oldValue);
        if (_node.ComponentNode?.EntityNode != null)
        {
            EditorSelection.Instance.Select(_node.ComponentNode.EntityNode);
        }
    }

    public void Redo()
    {
        _node.SetFieldState(_settingsType, _fieldName, _newOverridden, _newValue);
        if (_node.ComponentNode?.EntityNode != null)
        {
            EditorSelection.Instance.Select(_node.ComponentNode.EntityNode);
        }
    }

    public bool CanMergeWith(IEditorCommand other)
    {
        if (other is ModifyVolumeFieldCommand mod && ReferenceEquals(mod._node, _node) && mod._settingsType == _settingsType && string.Equals(mod._fieldName, _fieldName, StringComparison.Ordinal))
        {
            return (DateTime.UtcNow - _timestamp).TotalMilliseconds < 500.0;
        }

        return false;
    }

    public void MergeWith(IEditorCommand other)
    {
        if (other is ModifyVolumeFieldCommand mod)
        {
            _newOverridden = mod._newOverridden;
            _newValue = mod._newValue;
            _timestamp = DateTime.UtcNow;
        }
    }
}
