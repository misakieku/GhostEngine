using System.Reflection;

namespace Ghost.Editor.Core.Menus;

/// <summary>
/// Represents a single registered menu entry before path grouping.
/// </summary>
public sealed class MenuEntry
{
    public string Location { get; }
    public string Path { get; }
    public int Priority { get; }
    public string? Icon { get; }
    public string? Shortcut { get; }
    public Func<MenuContext, bool>? IsVisible { get; }
    public Func<MenuContext, bool>? CanExecute { get; }
    public Action<MenuContext> Execute { get; }

    public MenuEntry(string location, string path, int priority, Action<MenuContext> execute, Func<MenuContext, bool>? isVisible = null, Func<MenuContext, bool>? canExecute = null, string? icon = null, string? shortcut = null)
    {
        Location = location;
        Path = path;
        Priority = priority;
        Execute = execute;
        IsVisible = isVisible;
        CanExecute = canExecute;
        Icon = icon;
        Shortcut = shortcut;
    }
}

/// <summary>
/// Reflection-driven registry that discovers <see cref="EditorMenuItemAttribute"/> methods and builds dynamic menu trees.
/// </summary>
public sealed class EditorMenuRegistry
{
    private readonly object _lock = new();
    private readonly List<MenuEntry> _manualEntries = new();
    private readonly List<MenuEntry> _discoveredEntries = new();
    private readonly HashSet<Assembly> _scannedAssemblies = new();
    private int _lastAssemblyCount = -1;

    public static EditorMenuRegistry Default { get; } = new();

    public void Register(MenuEntry entry)
    {
        lock (_lock)
        {
            _manualEntries.Add(entry);
        }
    }

    public void RegisterAssembly(Assembly assembly)
    {
        lock (_lock)
        {
            ScanAssemblyLocked(assembly);
        }
    }

    public IReadOnlyList<MenuNode> BuildMenu(string location, MenuContext context)
    {
        EnsureLoadedAssembliesScanned();

        List<MenuEntry> matching;
        lock (_lock)
        {
            matching = new List<MenuEntry>();
            CollectMatchingLocked(_discoveredEntries, location, context, matching);
            CollectMatchingLocked(_manualEntries, location, context, matching);
        }

        if (matching.Count == 0)
        {
            return Array.Empty<MenuNode>();
        }

        matching.Sort((a, b) =>
        {
            var cmp = a.Priority.CompareTo(b.Priority);
            return cmp != 0 ? cmp : string.Compare(a.Path, b.Path, StringComparison.Ordinal);
        });

        var root = new MutableGroup(string.Empty, null, int.MaxValue);
        for (var i = 0; i < matching.Count; i++)
        {
            var entry = matching[i];
            var segments = entry.Path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
            {
                continue;
            }

            InsertEntry(root, segments, 0, entry, context);
        }

        return FinalizeGroup(root);
    }

    private void EnsureLoadedAssembliesScanned()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        lock (_lock)
        {
            if (_lastAssemblyCount == assemblies.Length)
            {
                return;
            }

            for (var i = 0; i < assemblies.Length; i++)
            {
                ScanAssemblyLocked(assemblies[i]);
            }

            _lastAssemblyCount = assemblies.Length;
        }
    }

    private void ScanAssemblyLocked(Assembly assembly)
    {
        if (assembly.IsDynamic || !_scannedAssemblies.Add(assembly))
        {
            return;
        }

        var name = assembly.GetName().Name;
        if (name != null && (name.StartsWith("System.", StringComparison.Ordinal) || name.StartsWith("Microsoft.", StringComparison.Ordinal) || name == "mscorlib" || name == "netstandard"))
        {
            return;
        }

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            var list = new List<Type>();
            for (var i = 0; i < ex.Types.Length; i++)
            {
                if (ex.Types[i] != null)
                {
                    list.Add(ex.Types[i]!);
                }
            }

            types = list.ToArray();
        }

        for (var t = 0; t < types.Length; t++)
        {
            var type = types[t];
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            for (var m = 0; m < methods.Length; m++)
            {
                var method = methods[m];
                var attrs = method.GetCustomAttributes<EditorMenuItemAttribute>(inherit: false);
                foreach (var attr in attrs)
                {
                    var entry = CreateEntryFromMethod(type, method, attr);
                    if (entry != null)
                    {
                        _discoveredEntries.Add(entry);
                    }
                }
            }
        }
    }

    private static MenuEntry? CreateEntryFromMethod(Type declaringType, MethodInfo method, EditorMenuItemAttribute attr)
    {
        var parameters = method.GetParameters();
        Action<MenuContext>? execute = null;

        if (parameters.Length == 1 && parameters[0].ParameterType == typeof(MenuContext))
        {
            execute = ctx => method.Invoke(null, new object[] { ctx });
        }
        else if (parameters.Length == 0)
        {
            execute = _ => method.Invoke(null, null);
        }

        if (execute == null)
        {
            return null;
        }

        Func<MenuContext, bool>? isVisible = null;
        if (!string.IsNullOrEmpty(attr.ValidateMethod))
        {
            var validator = declaringType.GetMethod(attr.ValidateMethod, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (validator != null && validator.ReturnType == typeof(bool))
            {
                var vParams = validator.GetParameters();
                if (vParams.Length == 1 && vParams[0].ParameterType == typeof(MenuContext))
                {
                    isVisible = ctx => (bool)validator.Invoke(null, new object[] { ctx })!;
                }
                else if (vParams.Length == 0)
                {
                    isVisible = _ => (bool)validator.Invoke(null, null)!;
                }
            }
        }

        return new MenuEntry(attr.Location, attr.Path, attr.Priority, execute, isVisible, null, attr.Icon, attr.Shortcut);
    }

    private static void CollectMatchingLocked(List<MenuEntry> source, string location, MenuContext context, List<MenuEntry> dest)
    {
        for (var i = 0; i < source.Count; i++)
        {
            var entry = source[i];
            if (!string.Equals(entry.Location, location, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (entry.IsVisible != null && !entry.IsVisible(context))
            {
                continue;
            }

            dest.Add(entry);
        }
    }

    private static void InsertEntry(MutableGroup group, string[] segments, int depth, MenuEntry entry, MenuContext context)
    {
        var segment = segments[depth];
        if (depth == segments.Length - 1)
        {
            var isEnabled = entry.CanExecute == null || entry.CanExecute(context);
            group.Items.Add(new MutableLeaf(segment, entry.Priority, new MenuActionNode(segment, entry.Icon, entry.Shortcut, isEnabled, () => entry.Execute(context))));
            if (entry.Priority < group.MinPriority)
            {
                group.MinPriority = entry.Priority;
            }

            return;
        }

        MutableGroup? subGroup = null;
        for (var i = 0; i < group.Items.Count; i++)
        {
            if (group.Items[i] is MutableGroup existing && string.Equals(existing.Label, segment, StringComparison.Ordinal))
            {
                subGroup = existing;
                break;
            }
        }

        if (subGroup == null)
        {
            subGroup = new MutableGroup(segment, null, entry.Priority);
            group.Items.Add(subGroup);
        }

        if (entry.Priority < subGroup.MinPriority)
        {
            subGroup.MinPriority = entry.Priority;
        }

        if (entry.Priority < group.MinPriority)
        {
            group.MinPriority = entry.Priority;
        }

        InsertEntry(subGroup, segments, depth + 1, entry, context);
    }

    private static IReadOnlyList<MenuNode> FinalizeGroup(MutableGroup group)
    {
        group.Items.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        var result = new List<MenuNode>(group.Items.Count);
        int? previousPriority = null;
        for (var i = 0; i < group.Items.Count; i++)
        {
            var item = group.Items[i];
            if (previousPriority.HasValue && item.Priority - previousPriority.Value >= 10)
            {
                result.Add(MenuSeparatorNode.Instance);
            }

            if (item is MutableLeaf leaf)
            {
                result.Add(leaf.Node);
            }
            else if (item is MutableGroup sub)
            {
                var children = FinalizeGroup(sub);
                if (children.Count > 0)
                {
                    result.Add(new MenuSubMenuNode(sub.Label, sub.Icon, children));
                }
            }

            previousPriority = item.Priority;
        }

        return result;
    }

    private abstract class MutableItem
    {
        public abstract int Priority { get; }
    }

    private sealed class MutableLeaf : MutableItem
    {
        public string Label { get; }
        public override int Priority { get; }
        public MenuActionNode Node { get; }

        public MutableLeaf(string label, int priority, MenuActionNode node)
        {
            Label = label;
            Priority = priority;
            Node = node;
        }
    }

    private sealed class MutableGroup : MutableItem
    {
        public string Label { get; }
        public string? Icon { get; }
        public int MinPriority;
        public List<MutableItem> Items { get; } = new();
        public override int Priority => MinPriority;

        public MutableGroup(string label, string? icon, int minPriority)
        {
            Label = label;
            Icon = icon;
            MinPriority = minPriority;
        }
    }
}
