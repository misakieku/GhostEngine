using System.Reflection;
using Ghost.Core;
using Ghost.Entities;
using Ghost.Graphics.Services;

namespace Ghost.Editor.Core.Sync;

/// <summary>
/// Stateless reflection helper for discovering ECS component types and building <see cref="ComponentNode"/> graphs.
/// </summary>
public static class ComponentNodeFactory
{
    private static readonly object s_cacheLock = new();
    private static int s_cachedAssemblyCount = -1;
    private static IReadOnlyList<Type> s_cachedComponentTypes = Array.Empty<Type>();

    /// <summary>
    /// Discovers all editor-visible <see cref="IComponentData"/> and <see cref="ISharedComponent"/> structs across loaded assemblies.
    /// </summary>
    public static IReadOnlyList<Type> DiscoverComponentTypes()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        lock (s_cacheLock)
        {
            if (s_cachedAssemblyCount == assemblies.Length && s_cachedComponentTypes.Count > 0)
            {
                return s_cachedComponentTypes;
            }

            var types = new List<Type>();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                if (assembly.IsDynamic)
                {
                    continue;
                }

                Type[] exported;
                try
                {
                    exported = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    var valid = new List<Type>();
                    for (var j = 0; j < ex.Types.Length; j++)
                    {
                        if (ex.Types[j] != null)
                        {
                            valid.Add(ex.Types[j]!);
                        }
                    }

                    exported = valid.ToArray();
                }

                for (var j = 0; j < exported.Length; j++)
                {
                    var t = exported[j];
                    if (!t.IsValueType || t.IsAbstract || t.IsGenericTypeDefinition || !t.IsPublic)
                    {
                        continue;
                    }

                    if (!typeof(IComponentData).IsAssignableFrom(t) && !typeof(ISharedComponent).IsAssignableFrom(t))
                    {
                        continue;
                    }

                    if (HasHideEditorAttribute(t))
                    {
                        continue;
                    }

                    types.Add(t);
                }
            }

            types.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            s_cachedComponentTypes = types;
            s_cachedAssemblyCount = assemblies.Length;
            return s_cachedComponentTypes;
        }
    }

    /// <summary>
    /// Creates a default boxed instance of <paramref name="componentType"/>, honoring static <c>Default</c> or <c>Identity</c> properties if defined.
    /// </summary>
    public static object? CreateDefaultValue(Type componentType)
    {
        var defaultProp = componentType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
        if (defaultProp != null && defaultProp.CanRead && componentType.IsAssignableFrom(defaultProp.PropertyType))
        {
            return defaultProp.GetValue(null);
        }

        var identityProp = componentType.GetProperty("Identity", BindingFlags.Public | BindingFlags.Static);
        if (identityProp != null && identityProp.CanRead && componentType.IsAssignableFrom(identityProp.PropertyType))
        {
            return identityProp.GetValue(null);
        }

        return Activator.CreateInstance(componentType);
    }

    /// <summary>
    /// Builds a <see cref="ComponentNode"/> and its reflected <see cref="PropertyNode"/> children for <paramref name="entityNode"/>.
    /// </summary>
    public static ComponentNode CreateComponentNode(Type componentType, EntityNode entityNode, object? initialValue = null, EditorSyncBridge? bridge = null)
    {
        var liveValue = initialValue ?? CreateDefaultValue(componentType);
        var properties = new List<PropertyNode>();
        var compNode = new ComponentNode(componentType.Name, componentType, entityNode, properties, liveValue);

        var fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            if (field.GetCustomAttribute<HideInInspectorAttribute>() != null || HasHideEditorAttribute(field))
            {
                continue;
            }

            var fieldValue = liveValue != null ? field.GetValue(liveValue) : null;
            var propNode = CreatePropertyNode(field, compNode, entityNode, fieldValue, bridge ?? EditorSyncBridge.Instance);
            properties.Add(propNode);
        }

        return compNode;
    }

    private static PropertyNode CreatePropertyNode(FieldInfo field, ComponentNode compNode, EntityNode entityNode, object? fieldValue, EditorSyncBridge bridge, PropertyNode? template = null)
    {
        var isReadOnly = field.GetCustomAttribute<ReadOnlyInInspectorAttribute>() != null;
        var displayName = field.GetCustomAttribute<InspectorNameAttribute>()?.Name;
        var groupName = field.GetCustomAttribute<InspectorGroupAttribute>()?.GroupName;
        var componentType = compNode.ComponentType;
        var capturedField = field;

        Action<object?> onChange = newValue =>
        {
            if (compNode.LiveValue != null)
            {
                var boxed = compNode.LiveValue;
                capturedField.SetValue(boxed, newValue);
                compNode.LiveValue = boxed;
            }

            if (entityNode.EcsEntity.IsValid)
            {
                bridge.EnqueueMutation(entityNode.EcsEntity, componentType, capturedField.Name, newValue);
            }
        };

        var fieldType = field.FieldType;
        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Handle<>))
        {
            var initialGuid = template is AssetPropertyNode assetTpl ? assetTpl.AssetGuid : default;
            return new AssetPropertyNode(field.Name, fieldType, compNode, fieldValue, onChange, bridge, isReadOnly, displayName, groupName, initialGuid);
        }

        if (fieldType == typeof(Identifier<MaterialPalette>))
        {
            var initialGuids = template is MaterialPalettePropertyNode paletteTpl ? paletteTpl.MaterialGuids : null;
            return new MaterialPalettePropertyNode(field.Name, fieldType, compNode, fieldValue, onChange, bridge, isReadOnly, displayName, groupName, initialGuids);
        }

        if (fieldType == typeof(Entity))
        {
            var targetGuid = template is EntityRefPropertyNode entTpl ? entTpl.TargetEditorGuid : default;
            var targetId = template is EntityRefPropertyNode entTplId ? entTplId.TargetNodeId : 0;
            return new EntityRefPropertyNode(field.Name, fieldType, compNode, fieldValue, onChange, bridge, isReadOnly, displayName, groupName, targetGuid, targetId);
        }

        return new ValuePropertyNode(field.Name, fieldType, compNode, fieldValue, onChange, isReadOnly, displayName, groupName);
    }

    /// <summary>
    /// Creates a deep copy of <paramref name="source"/> with a new entity ID, name, and parent.
    /// </summary>
    public static EntityNode CloneEntityNode(EntityNode source, int newId, string newName, int? newParentId, EditorSyncBridge? bridge = null)
    {
        var resolvedBridge = bridge ?? EditorSyncBridge.Instance;
        var clonedComponents = new List<ComponentNode>(source.Components.Count);
        var clone = new EntityNode(newId, newName, clonedComponents, newParentId);

        for (var i = 0; i < source.Components.Count; i++)
        {
            var srcComp = source.Components[i];
            var clonedProps = new List<PropertyNode>(srcComp.Properties.Count);
            var clonedComp = new ComponentNode(srcComp.TypeName, srcComp.ComponentType, clone, clonedProps, srcComp.LiveValue);

            if (srcComp.LiveValue != null)
            {
                var fields = srcComp.ComponentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                for (var f = 0; f < fields.Length; f++)
                {
                    var field = fields[f];
                    if (field.GetCustomAttribute<HideInInspectorAttribute>() != null || HasHideEditorAttribute(field))
                    {
                        continue;
                    }

                    PropertyNode? srcTemplate = null;
                    for (var p = 0; p < srcComp.Properties.Count; p++)
                    {
                        if (string.Equals(srcComp.Properties[p].Name, field.Name, StringComparison.Ordinal))
                        {
                            srcTemplate = srcComp.Properties[p];
                            break;
                        }
                    }

                    var fieldValue = field.GetValue(srcComp.LiveValue);
                    clonedProps.Add(CreatePropertyNode(field, clonedComp, clone, fieldValue, resolvedBridge, srcTemplate));
                }
            }
            else
            {
                for (var p = 0; p < srcComp.Properties.Count; p++)
                {
                    clonedProps.Add(srcComp.Properties[p].Clone(clonedComp));
                }
            }

            clonedComponents.Add(clonedComp);
        }

        return clone;
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
