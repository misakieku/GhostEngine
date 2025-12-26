namespace Ghost.Shader.Concept;

/// <summary>
/// Global keyword state manager. Singleton pattern for engine-wide keywords.
/// Keywords like platform settings, quality levels, etc.
/// </summary>
public sealed class GlobalKeywordState
{
    private static readonly Lazy<GlobalKeywordState> _instance = new(() => new GlobalKeywordState());
    public static GlobalKeywordState Instance => _instance.Value;

    private KeywordSet _keywords;
    private readonly object _lock = new();
    private int _version = 0;

    public int Version => _version;

    private GlobalKeywordState()
    {
        _keywords = new KeywordSet();
    }

    public void EnableKeyword(ShaderKeyword keyword)
    {
        if (keyword.Scope != KeywordScope.Global)
            throw new ArgumentException("Only global keywords can be set", nameof(keyword));

        lock (_lock)
        {
            _keywords.Enable(keyword);
            _version++;
        }
    }

    public void DisableKeyword(ShaderKeyword keyword)
    {
        if (keyword.Scope != KeywordScope.Global)
            throw new ArgumentException("Only global keywords can be set", nameof(keyword));

        lock (_lock)
        {
            _keywords.Disable(keyword);
            _version++;
        }
    }

    public bool IsKeywordEnabled(ShaderKeyword keyword)
    {
        lock (_lock)
        {
            return _keywords.IsEnabled(keyword);
        }
    }

    public KeywordSet GetKeywordSet()
    {
        lock (_lock)
        {
            return _keywords; // struct copy
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _keywords.Clear();
            _version++;
        }
    }
}
