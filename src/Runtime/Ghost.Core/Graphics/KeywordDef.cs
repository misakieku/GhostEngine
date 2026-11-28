namespace Ghost.Core.Graphics;

/// <summary>
/// Defines a single on/off shader keyword toggle whose permutations include both the disabled (no define) and enabled variants.
/// </summary>
public sealed class KeywordDef : IKeywordDef
{
    private string[] _keywords = Array.Empty<string>();

    /// <summary>
    /// Gets or initializes the logical name of this keyword toggle. Defaults to <see cref="Keyword"/> if unset.
    /// </summary>
    public string Name
    {
        get => string.IsNullOrEmpty(field) ? Keyword : field;
        init => field = value;
    } = string.Empty;

    /// <summary>
    /// Gets or initializes the preprocessor macro name compiled when this toggle is enabled.
    /// </summary>
    public string Keyword
    {
        get => field;
        init
        {
            field = value;
            _keywords = string.IsNullOrEmpty(value) ? Array.Empty<string>() : new[] { string.Empty, value };
        }
    } = string.Empty;

    /// <inheritdoc />
    public string[] Keywords => _keywords;
}
