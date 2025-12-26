namespace Ghost.Shader.Concept;

/// <summary>
/// Represents a shader keyword that can toggle shader features.
/// Keywords are immutable and interned for fast comparison.
/// </summary>
public readonly struct ShaderKeyword : IEquatable<ShaderKeyword>
{
    private readonly int _id;
    private readonly KeywordScope _scope;

    public int Id => _id;
    public KeywordScope Scope => _scope;
    public bool IsValid => _id >= 0;

    internal ShaderKeyword(int id, KeywordScope scope)
    {
        _id = id;
        _scope = scope;
    }

    public bool Equals(ShaderKeyword other) => _id == other._id && _scope == other._scope;
    public override bool Equals(object? obj) => obj is ShaderKeyword other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(_id, _scope);

    public static bool operator ==(ShaderKeyword left, ShaderKeyword right) => left.Equals(right);
    public static bool operator !=(ShaderKeyword left, ShaderKeyword right) => !left.Equals(right);
}

public enum KeywordScope : byte
{
    /// <summary>Keywords set globally (e.g., platform, quality settings)</summary>
    Global,
    
    /// <summary>Keywords set per-material instance</summary>
    Local
}

/// <summary>
/// Manages keyword registration and fast lookup.
/// Thread-safe for registration, lock-free for lookups.
/// </summary>
public sealed class ShaderKeywordRegistry
{
    private readonly Dictionary<string, ShaderKeyword> _keywords = new();
    private readonly Dictionary<int, string> _idToName = new();
    private int _nextId = 0;
    private readonly object _lock = new();

    public static ShaderKeywordRegistry Instance { get; } = new();

    private ShaderKeywordRegistry() { }

    public ShaderKeyword GetOrRegister(string name, KeywordScope scope)
    {
        string key = $"{scope}:{name}";
        
        lock (_lock)
        {
            if (_keywords.TryGetValue(key, out var existing))
                return existing;

            var keyword = new ShaderKeyword(_nextId++, scope);
            _keywords[key] = keyword;
            _idToName[keyword.Id] = name;
            return keyword;
        }
    }

    public string? GetName(ShaderKeyword keyword)
    {
        lock (_lock)
        {
            return _idToName.TryGetValue(keyword.Id, out var name) ? name : null;
        }
    }
}
