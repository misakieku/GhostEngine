using System.Reflection;
using Ghost.Core;
using Ghost.Entities;

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

            var isReadOnly = field.GetCustomAttribute<ReadOnlyInInspectorAttribute>() != null;
            var displayName = field.GetCustomAttribute<InspectorNameAttribute>()?.Name;
            var groupName = field.GetCustomAttribute<InspectorGroupAttribute>()?.GroupName;
            var fieldValue = liveValue != null ? field.GetValue(liveValue) : null;

            var capturedField = field;
            var propNode = new PropertyNode(
                field.Name,
                field.FieldType,
                compNode,
                fieldValue,
                newValue =>
                {
                    if (compNode.LiveValue != null)
                    {
                        var boxed = compNode.LiveValue;
                        capturedField.SetValue(boxed, newValue);
                        compNode.LiveValue = boxed;
                    }

                    if (entityNode.EcsEntity.IsValid)
                    {
                        (bridge ?? EditorSyncBridge.Instance).EnqueueMutation(entityNode.EcsEntity, componentType, capturedField.Name, newValue);
                    }
                },
                isReadOnly,
                displayName,
                groupName);

            properties.Add(propNode);
        }

        return compNode;
    }

    /// <summary>
    /// Creates a deep copy of <paramref name="source"/> with a new entity ID, name, and parent.
    /// </summary>
    public static EntityNode CloneEntityNode(EntityNode source, int newId, string newName, int? newParentId, EditorSyncBridge? bridge = null)
    {
        var clonedComponents = new List<ComponentNode>(source.Components.Count);
        var clone = new EntityNode(newId, newName, clonedComponents, newParentId);

        for (var i = 0; i < source.Components.Count; i++)
        {
            var srcComp = source.Components[i];
            if (srcComp.LiveValue != null)
            {
                clonedComponents.Add(CreateComponentNode(srcComp.ComponentType, clone, srcComp.LiveValue, bridge));
                continue;
            }

            var clonedProps = new List<PropertyNode>(srcComp.Properties.Count);
            var clonedComp = new ComponentNode(srcComp.TypeName, srcComp.ComponentType, clone, clonedProps);
            for (var p = 0; p < srcComp.Properties.Count; p++)
            {
                var srcProp = srcComp.Properties[p];
                var capturedProp = srcProp;
                var clonedProp = new PropertyNode(
                    srcProp.Name,
                    srcProp.PropertyType,
                    clonedComp,
                    srcProp.Value,
                    val => capturedProp.OnChange(val),
                    srcProp.IsReadOnly,
                    srcProp.DisplayName,
                    srcProp.GroupName);
                clonedProps.Add(clonedProp);
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
