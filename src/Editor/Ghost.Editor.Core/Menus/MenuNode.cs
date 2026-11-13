namespace Ghost.Editor.Core.Menus;

/// <summary>
/// Base node in a resolved editor menu tree.
/// </summary>
public abstract record MenuNode;

/// <summary>
/// Leaf menu command node.
/// </summary>
public sealed record MenuActionNode(string Label, string? Icon, string? Shortcut, bool IsEnabled, Action Execute) : MenuNode;

/// <summary>
/// Nested submenu containing child <see cref="MenuNode"/> entries.
/// </summary>
public sealed record MenuSubMenuNode(string Label, string? Icon, IReadOnlyList<MenuNode> Children) : MenuNode;

/// <summary>
/// Visual separator between menu groups.
/// </summary>
public sealed record MenuSeparatorNode : MenuNode
{
    public static readonly MenuSeparatorNode Instance = new();
}
