namespace Ghost.Shader.Concept;

/// <summary>
/// Represents a material instance with properties, keywords, and per-pass overrides.
/// Thread-safe for property updates, optimized for rendering.
/// </summary>
public sealed class Material : IDisposable
{
    private readonly ShaderProgram _shaderProgram;
    private readonly MaterialPropertyBlock _propertyBlock;
    private KeywordSet _localKeywords;
    private readonly RenderState[] _passOverrides;
    private readonly object _lock = new();
    private bool _isDirty = true;

    public ShaderProgram ShaderProgram => _shaderProgram;
    public bool IsDirty => _isDirty;

    public Material(ShaderProgram shaderProgram)
    {
        _shaderProgram = shaderProgram;
        _propertyBlock = new MaterialPropertyBlock();
        _localKeywords = new KeywordSet();
        _passOverrides = new RenderState[shaderProgram.Passes.Length];
        
        // Initialize pass overrides with shader defaults
        for (int i = 0; i < _passOverrides.Length; i++)
        {
            _passOverrides[i] = shaderProgram.Passes[i].RenderState;
        }
    }

    public void Dispose()
    {
        _propertyBlock?.Dispose();
    }

    #region Property Updates

    public void SetFloat(string name, float value)
    {
        _propertyBlock.SetFloat(name, value);
        MarkDirty();
    }

    public void SetVector2(string name, float x, float y)
    {
        _propertyBlock.SetVector2(name, x, y);
        MarkDirty();
    }

    public void SetVector3(string name, float x, float y, float z)
    {
        _propertyBlock.SetVector3(name, x, y, z);
        MarkDirty();
    }

    public void SetVector4(string name, float x, float y, float z, float w)
    {
        _propertyBlock.SetVector4(name, x, y, z, w);
        MarkDirty();
    }

    public void SetInt(string name, int value)
    {
        _propertyBlock.SetInt(name, value);
        MarkDirty();
    }

    public void SetMatrix4x4(string name, ReadOnlySpan<float> matrix)
    {
        _propertyBlock.SetMatrix4x4(name, matrix);
        MarkDirty();
    }

    public bool TryGetFloat(string name, out float value)
    {
        return _propertyBlock.TryGetFloat(name, out value);
    }

    #endregion

    #region Keyword Management

    public void EnableKeyword(ShaderKeyword keyword)
    {
        if (keyword.Scope != KeywordScope.Local)
            throw new ArgumentException("Only local keywords can be set on materials", nameof(keyword));

        lock (_lock)
        {
            _localKeywords.Enable(keyword);
            MarkDirty();
        }
    }

    public void DisableKeyword(ShaderKeyword keyword)
    {
        if (keyword.Scope != KeywordScope.Local)
            throw new ArgumentException("Only local keywords can be set on materials", nameof(keyword));

        lock (_lock)
        {
            _localKeywords.Disable(keyword);
            MarkDirty();
        }
    }

    public bool IsKeywordEnabled(ShaderKeyword keyword)
    {
        lock (_lock)
        {
            return _localKeywords.IsEnabled(keyword);
        }
    }

    public KeywordSet GetLocalKeywords()
    {
        lock (_lock)
        {
            return _localKeywords;
        }
    }

    #endregion

    #region Pass State Overrides

    public void SetPassRenderState(int passIndex, RenderState state)
    {
        if (passIndex < 0 || passIndex >= _passOverrides.Length)
            throw new ArgumentOutOfRangeException(nameof(passIndex));

        lock (_lock)
        {
            _passOverrides[passIndex] = state;
            MarkDirty();
        }
    }

    public void SetPassRenderState(string passName, RenderState state)
    {
        int index = _shaderProgram.GetPassIndex(passName);
        if (index < 0)
            throw new ArgumentException($"Pass '{passName}' not found in shader program", nameof(passName));
        
        SetPassRenderState(index, state);
    }

    public RenderState GetPassRenderState(int passIndex)
    {
        if (passIndex < 0 || passIndex >= _passOverrides.Length)
            throw new ArgumentOutOfRangeException(nameof(passIndex));

        lock (_lock)
        {
            return _passOverrides[passIndex];
        }
    }

    #endregion

    #region Pipeline Key Generation

    /// <summary>
    /// Generates a pipeline key for a specific pass, combining global and local keywords.
    /// </summary>
    public unsafe GraphicsPipelineKey GetPipelineKey(int passIndex, in KeywordSet globalKeywords)
    {
        if (passIndex < 0 || passIndex >= _passOverrides.Length)
            throw new ArgumentOutOfRangeException(nameof(passIndex));

        KeywordSet combined;
        RenderState state;

        lock (_lock)
        {
            fixed (KeywordSet* pGlobal = &globalKeywords)
            fixed (KeywordSet* pLocal = &_localKeywords)
            {
                combined = KeywordSet.Merge(pGlobal, pLocal);
            }
            state = _passOverrides[passIndex];
        }

        var variantKey = _shaderProgram.CreateVariantKey(combined);
        var pipelineKey = new GraphicsPipelineKey(
            variantKey,
            state.ComputeHash(),
            _shaderProgram.Passes[passIndex].PassId);

        return pipelineKey;
    }

    #endregion

    public unsafe void CopyPropertiesTo(byte* destination, int maxSize)
    {
        _propertyBlock.CopyTo(destination, maxSize);
    }

    public void ClearDirty()
    {
        _isDirty = false;
    }

    private void MarkDirty()
    {
        _isDirty = true;
    }

    public Material Clone()
    {
        var clone = new Material(_shaderProgram);
        
        lock (_lock)
        {
            clone._propertyBlock.CopyFrom(_propertyBlock);
            clone._localKeywords = _localKeywords;
            
            for (int i = 0; i < _passOverrides.Length; i++)
            {
                clone._passOverrides[i] = _passOverrides[i];
            }
        }

        return clone;
    }
}
