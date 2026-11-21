using Ghost.Engine.Components;
using Ghost.Engine.Volumes;
using Ghost.Entities;
using System.Reflection;
using System.Text;

namespace Ghost.Editor.Core.Sync;

/// <summary>
/// Non-generic accessor contract for reading, writing, and removing a specific <see cref="IVolumeComponent{T}"/> inside a <see cref="RenderVolumeDataContainer"/>.
/// </summary>
public interface IVolumeOverrideAccessor
{
    Type SettingsType { get; }
    ushort TypeId { get; }
    string DisplayName { get; }
    object DefaultValue { get; }
    object EmptyValue { get; }
    bool TryRead(in RenderVolumeDataContainer container, out object value);
    void Write(ref RenderVolumeDataContainer container, object value);
    bool Remove(ref RenderVolumeDataContainer container);
}

/// <summary>
/// Discovers <see cref="IVolumeComponent{T}"/> types across loaded assemblies and caches typed unmanaged container accessors.
/// </summary>
public static class VolumeTypeRegistry
{
    private sealed class TypedVolumeAccessor<T> : IVolumeOverrideAccessor
        where T : unmanaged, IVolumeComponent<T>
    {
        public Type SettingsType => typeof(T);

        public ushort TypeId => (ushort)ComponentTypeID<T>.Value.Value;

        public string DisplayName { get; } = PrettifyVolumeName(typeof(T).Name);

        public object DefaultValue => T.Default;

        public object EmptyValue => default(T);

        public bool TryRead(in RenderVolumeDataContainer container, out object value)
        {
            if (container.TryGet<T>(out var typed))
            {
                value = typed;
                return true;
            }

            value = default(T);
            return false;
        }

        public void Write(ref RenderVolumeDataContainer container, object value)
        {
            var typed = (T)value;
            container.Set(in typed);
        }

        public bool Remove(ref RenderVolumeDataContainer container)
        {
            return container.Remove<T>();
        }
    }

    private static readonly object s_lock = new();
    private static int s_cachedAssemblyCount = -1;
    private static IReadOnlyList<IVolumeOverrideAccessor> s_accessors = Array.Empty<IVolumeOverrideAccessor>();
    private static readonly Dictionary<Type, IVolumeOverrideAccessor> s_byType = new();
    private static readonly Dictionary<ushort, IVolumeOverrideAccessor> s_byTypeId = new();

    /// <summary>
    /// Discovers and caches all editor-visible <see cref="IVolumeComponent{T}"/> settings structs.
    /// </summary>
    public static IReadOnlyList<IVolumeOverrideAccessor> DiscoverVolumeTypes()
    {
        EnsureDiscovered();
        return s_accessors;
    }

    /// <summary>
    /// Looks up a cached volume accessor by settings struct <see cref="Type"/>.
    /// </summary>
    public static bool TryGetAccessor(Type settingsType, out IVolumeOverrideAccessor accessor)
    {
        EnsureDiscovered();
        lock (s_lock)
        {
            if (s_byType.TryGetValue(settingsType, out accessor!))
            {
                return true;
            }
        }

        accessor = null!;
        return false;
    }

    /// <summary>
    /// Looks up a cached volume accessor by ECS component <paramref name="typeId"/>.
    /// </summary>
    public static bool TryGetAccessor(ushort typeId, out IVolumeOverrideAccessor accessor)
    {
        EnsureDiscovered();
        lock (s_lock)
        {
            if (s_byTypeId.TryGetValue(typeId, out accessor!))
            {
                return true;
            }
        }

        accessor = null!;
        return false;
    }

    /// <summary>
    /// Formats a volume settings struct type name into a user-friendly title (e.g. <c>BloomSettings</c> -> <c>Bloom</c>).
    /// </summary>
    public static string PrettifyVolumeName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return string.Empty;
        }

        var trimmed = typeName.EndsWith("Settings", StringComparison.Ordinal) && typeName.Length > "Settings".Length
            ? typeName[..^"Settings".Length]
            : typeName;

        var sb = new StringBuilder(trimmed.Length + 4);
        for (var i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            if (i > 0 && char.IsUpper(c))
            {
                var prev = trimmed[i - 1];
                var nextIsLower = i + 1 < trimmed.Length && char.IsLower(trimmed[i + 1]);
                if (!char.IsUpper(prev) || nextIsLower)
                {
                    sb.Append(' ');
                }
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    private static void EnsureDiscovered()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        lock (s_lock)
        {
            if (s_cachedAssemblyCount == assemblies.Length && s_accessors.Count > 0)
            {
                return;
            }

            var list = new List<IVolumeOverrideAccessor>();
            s_byType.Clear();
            s_byTypeId.Clear();

            for (var i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                if (assembly.IsDynamic)
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
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

                    types = valid.ToArray();
                }

                for (var j = 0; j < types.Length; j++)
                {
                    var t = types[j];
                    if (!t.IsValueType || t.IsAbstract || t.IsGenericTypeDefinition || !t.IsPublic)
                    {
                        continue;
                    }

                    if (!ImplementsVolumeComponent(t) || HasHideEditorAttribute(t))
                    {
                        continue;
                    }

                    var accessorType = typeof(TypedVolumeAccessor<>).MakeGenericType(t);
                    var accessor = (IVolumeOverrideAccessor)Activator.CreateInstance(accessorType)!;
                    list.Add(accessor);
                    s_byType[t] = accessor;
                    s_byTypeId[accessor.TypeId] = accessor;
                }
            }

            list.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));
            s_accessors = list;
            s_cachedAssemblyCount = assemblies.Length;
        }
    }

    private static bool ImplementsVolumeComponent(Type type)
    {
        var interfaces = type.GetInterfaces();
        for (var i = 0; i < interfaces.Length; i++)
        {
            var iface = interfaces[i];
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IVolumeComponent<>) && iface.GetGenericArguments()[0] == type)
            {
                return true;
            }
        }

        return false;
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
