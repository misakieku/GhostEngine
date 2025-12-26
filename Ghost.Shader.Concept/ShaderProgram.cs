namespace Ghost.Shader.Concept;

/// <summary>
/// Represents a single rendering pass within a shader program.
/// Each pass can have its own render state overrides.
/// </summary>
public sealed class ShaderPass
{
    public string Name { get; }
    public int PassId { get; }
    public RenderState RenderState { get; }
    public string VertexEntryPoint { get; }
    public string PixelEntryPoint { get; }

    public ShaderPass(
        string name,
        int passId,
        RenderState renderState,
        string vertexEntryPoint = "VSMain",
        string pixelEntryPoint = "PSMain")
    {
        Name = name;
        PassId = passId;
        RenderState = renderState;
        VertexEntryPoint = vertexEntryPoint;
        PixelEntryPoint = pixelEntryPoint;
    }
}

/// <summary>
/// Shader program containing multiple passes and keyword declarations.
/// Immutable after creation for thread-safety.
/// </summary>
public sealed class ShaderProgram
{
    private static int _nextId = 0;

    public int Id { get; }
    public string Name { get; }
    public ShaderPass[] Passes { get; }
    public ShaderKeyword[] DeclaredKeywords { get; }
    
    private readonly Dictionary<string, int> _passNameToIndex = new();

    public ShaderProgram(
        string name,
        ShaderPass[] passes,
        ShaderKeyword[] declaredKeywords)
    {
        Id = Interlocked.Increment(ref _nextId);
        Name = name;
        Passes = passes;
        DeclaredKeywords = declaredKeywords;

        for (int i = 0; i < passes.Length; i++)
        {
            _passNameToIndex[passes[i].Name] = i;
        }
    }

    public int GetPassIndex(string passName)
    {
        return _passNameToIndex.TryGetValue(passName, out int index) ? index : -1;
    }

    public ShaderVariantKey CreateVariantKey(in KeywordSet keywords)
    {
        return new ShaderVariantKey(Id, keywords.ComputeHash());
    }
}

/// <summary>
/// Builder pattern for creating shader programs fluently.
/// </summary>
public sealed class ShaderProgramBuilder
{
    private string _name = "Unnamed";
    private readonly List<ShaderPass> _passes = new();
    private readonly List<ShaderKeyword> _keywords = new();

    public ShaderProgramBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ShaderProgramBuilder AddPass(
        string passName,
        RenderState? renderState = null,
        string vertexEntry = "VSMain",
        string pixelEntry = "PSMain")
    {
        var pass = new ShaderPass(
            passName,
            _passes.Count,
            renderState ?? RenderState.Default,
            vertexEntry,
            pixelEntry);
        _passes.Add(pass);
        return this;
    }

    public ShaderProgramBuilder DeclareKeyword(ShaderKeyword keyword)
    {
        _keywords.Add(keyword);
        return this;
    }

    public ShaderProgramBuilder DeclareKeywords(params ShaderKeyword[] keywords)
    {
        _keywords.AddRange(keywords);
        return this;
    }

    public ShaderProgram Build()
    {
        if (_passes.Count == 0)
            throw new InvalidOperationException("Shader program must have at least one pass");

        return new ShaderProgram(_name, _passes.ToArray(), _keywords.ToArray());
    }
}
