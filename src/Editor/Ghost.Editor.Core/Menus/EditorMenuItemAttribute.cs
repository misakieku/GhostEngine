namespace Ghost.Editor.Core.Menus;

/// <summary>
/// Marks a static method as an editor menu or context menu item discovered via reflection by <see cref="EditorMenuRegistry"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class EditorMenuItemAttribute : Attribute
{
    /// <summary>
    /// Target menu slot (e.g. "Hierarchy", "Entity", "Inspector/Component").
    /// </summary>
    public string Location { get; }

    /// <summary>
    /// Slash-delimited item path (e.g. "Create/Light/Directional Light").
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Sort priority; a jump of 10 or more between adjacent items inserts a separator automatically.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Optional icon glyph (e.g. Segoe Fluent Icons codepoint).
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Optional shortcut display hint (e.g. "Del", "Ctrl+D").
    /// </summary>
    public string? Shortcut { get; set; }

    /// <summary>
    /// Optional static validation method name on the declaring type taking <see cref="MenuContext"/> and returning <see cref="bool"/>.
    /// </summary>
    public string? ValidateMethod { get; set; }

    public EditorMenuItemAttribute(string location, string path, int priority = 0)
    {
        Location = location;
        Path = path;
        Priority = priority;
    }
}
