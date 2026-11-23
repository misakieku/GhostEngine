using System.Reflection;
using System.Text.Json;
using Ghost.AssetForge.Core.Models;
using Ghost.Entities;

namespace Ghost.Editor.Core.Sync;

/// <summary>
/// Property node for <see cref="Entity"/> reference fields storing stable authoring target identity
/// (<see cref="TargetEditorGuid"/> / <see cref="TargetNodeId"/>) while syncing live <see cref="Entity"/> handles to ECS.
/// </summary>
public sealed class EntityRefPropertyNode : PropertyNode
{
    private readonly EditorSyncBridge _bridge;

    public Guid TargetEditorGuid { get; private set; }

    public int TargetNodeId { get; private set; }

    public EntityRefPropertyNode(string name, Type propertyType, ComponentNode componentNode, object? value, Action<object?> onChange, EditorSyncBridge? bridge = null, bool isReadOnly = false, string? displayName = null, string? groupName = null, Guid targetEditorGuid = default, int targetNodeId = 0)
        : base(name, propertyType, componentNode, value, onChange, isReadOnly, displayName, groupName)
    {
        _bridge = bridge ?? EditorSyncBridge.Instance;
        TargetEditorGuid = targetEditorGuid;
        TargetNodeId = targetNodeId;

        if (TargetNodeId == 0 && TargetEditorGuid == Guid.Empty && value is Entity ecs && ecs.IsValid)
        {
            SyncIdentityFromEntity(ecs);
        }
    }

    public override bool RequiresPostSnapshotBind => true;

    public override void ApplyValue(object? newValue)
    {
        if (newValue is int nodeId)
        {
            if (nodeId <= 0)
            {
                TargetNodeId = 0;
                TargetEditorGuid = Guid.Empty;
                Value = Entity.Invalid;
                OnChange(Entity.Invalid);
                return;
            }

            var targetNode = _bridge.FindEntityNode(nodeId);
            if (targetNode != null)
            {
                TargetNodeId = targetNode.Id;
                TargetEditorGuid = targetNode.EditorGuid;
                Value = targetNode.EcsEntity;
                OnChange(targetNode.EcsEntity);
            }
            return;
        }

        if (newValue is Guid editorGuid)
        {
            if (editorGuid == Guid.Empty)
            {
                TargetNodeId = 0;
                TargetEditorGuid = Guid.Empty;
                Value = Entity.Invalid;
                OnChange(Entity.Invalid);
                return;
            }

            var targetNode = _bridge.FindEntityNode(editorGuid);
            if (targetNode != null)
            {
                TargetNodeId = targetNode.Id;
                TargetEditorGuid = targetNode.EditorGuid;
                Value = targetNode.EcsEntity;
                OnChange(targetNode.EcsEntity);
            }
            return;
        }

        if (newValue is Entity ecsEntity)
        {
            SyncIdentityFromEntity(ecsEntity);
            Value = ecsEntity;
            OnChange(ecsEntity);
            return;
        }

        if (newValue == null)
        {
            TargetNodeId = 0;
            TargetEditorGuid = Guid.Empty;
            Value = Entity.Invalid;
            OnChange(Entity.Invalid);
        }
    }

    public override void RefreshFromEcs(object boxedComponent, FieldInfo? field)
    {
        if (field == null)
        {
            return;
        }

        Value = field.GetValue(boxedComponent);
        if (Value is Entity ecs)
        {
            SyncIdentityFromEntity(ecs);
        }
    }

    public override object? GetSerializableValue()
    {
        return TargetEditorGuid;
    }

    public override JsonElement SerializeToJson(JsonSerializerOptions options)
    {
        return SceneJsonTokens.CreateEntityRef(TargetEditorGuid);
    }

    public override void DeserializeFromJson(JsonElement element, JsonSerializerOptions options)
    {
        if (SceneJsonTokens.TryGetEntityRef(element, out var guid))
        {
            ApplyValue(guid);
        }
    }

    public override PropertyNode Clone(ComponentNode targetComponent)
    {
        return new EntityRefPropertyNode(Name, PropertyType, targetComponent, Value, OnChange, _bridge, IsReadOnly, DisplayName, GroupName, TargetEditorGuid, TargetNodeId);
    }

    private void SyncIdentityFromEntity(Entity ecsEntity)
    {
        if (!ecsEntity.IsValid)
        {
            TargetNodeId = 0;
            TargetEditorGuid = Guid.Empty;
            return;
        }

        var node = _bridge.FindEntityNode(ecsEntity);
        if (node != null)
        {
            TargetNodeId = node.Id;
            TargetEditorGuid = node.EditorGuid;
        }
        else if (_bridge.TryGetMappedNodeId(ecsEntity, out var mappedId))
        {
            TargetNodeId = mappedId;
        }
    }
}
