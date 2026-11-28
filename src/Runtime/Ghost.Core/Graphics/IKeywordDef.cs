namespace Ghost.Core.Graphics;

/// <summary>
/// Defines a shader keyword toggle or mutually exclusive keyword group whose permutations are expanded during shader baking.
/// </summary>
public interface IKeywordDef
{
    /// <summary>
    /// Gets the logical name of this keyword definition.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the keyword variants to permute; an empty string represents the disabled (no-define) variant.
    /// </summary>
    string[] Keywords { get; }
}
