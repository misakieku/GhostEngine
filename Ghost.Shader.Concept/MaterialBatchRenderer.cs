using System.Collections.Concurrent;

namespace Ghost.Shader.Concept;

/// <summary>
/// High-performance material batch system for rendering.
/// Groups materials by shader variant and pass for efficient draw call submission.
/// Uses lock-free data structures where possible.
/// </summary>
public sealed class MaterialBatchRenderer
{
    private readonly IShaderCompiler _shaderCompiler;
    private readonly IPipelineLibrary _pipelineLibrary;
    private readonly ConcurrentDictionary<ShaderVariantKey, IntPtr> _compiledVariants = new();
    private readonly ConcurrentDictionary<GraphicsPipelineKey, IntPtr> _cachedPipelines = new();

    public MaterialBatchRenderer(IShaderCompiler shaderCompiler, IPipelineLibrary pipelineLibrary)
    {
        _shaderCompiler = shaderCompiler;
        _pipelineLibrary = pipelineLibrary;
    }

    /// <summary>
    /// Batches draw calls by material and pass.
    /// Returns sorted batches ready for submission.
    /// </summary>
    public MaterialBatch[] BatchDrawCalls(ReadOnlySpan<DrawCommand> drawCalls)
    {
        var globalKeywords = GlobalKeywordState.Instance.GetKeywordSet();
        var batchMap = new Dictionary<GraphicsPipelineKey, List<DrawCommand>>();

        // Group by pipeline key
        foreach (var drawCall in drawCalls)
        {
            var material = drawCall.Material;
            var passIndex = drawCall.PassIndex;

            var pipelineKey = material.GetPipelineKey(passIndex, globalKeywords);

            if (!batchMap.TryGetValue(pipelineKey, out var batch))
            {
                batch = new List<DrawCommand>();
                batchMap[pipelineKey] = batch;
            }

            batch.Add(drawCall);
        }

        // Convert to array and ensure PSOs are ready
        var batches = new MaterialBatch[batchMap.Count];
        int index = 0;

        foreach (var kvp in batchMap)
        {
            var pipelineKey = kvp.Key;
            var drawCommands = kvp.Value;

            // Ensure shader variant is compiled
            EnsureVariantCompiled(pipelineKey.VariantKey, drawCommands[0].Material);

            // Get or create PSO
            var pso = GetOrCreatePipeline(pipelineKey);

            batches[index++] = new MaterialBatch
            {
                PipelineKey = pipelineKey,
                Pipeline = pso,
                DrawCommands = drawCommands.ToArray()
            };
        }

        // Sort batches for optimal state changes (PSO switches are expensive)
        Array.Sort(batches, (a, b) => a.PipelineKey.GetHashCode().CompareTo(b.PipelineKey.GetHashCode()));

        return batches;
    }

    private unsafe void EnsureVariantCompiled(ShaderVariantKey variantKey, Material material)
    {
        if (_compiledVariants.ContainsKey(variantKey))
            return;

        var global = GlobalKeywordState.Instance.GetKeywordSet();
        var local = material.GetLocalKeywords();
        KeywordSet keywords = KeywordSet.Merge(&global, &local);
        var compiledShader = _shaderCompiler.CompileVariant(variantKey, keywords);
        _compiledVariants.TryAdd(variantKey, compiledShader);
    }

    private IntPtr GetOrCreatePipeline(GraphicsPipelineKey pipelineKey)
    {
        if (_cachedPipelines.TryGetValue(pipelineKey, out var cached))
            return cached;

        var pso = _pipelineLibrary.GetOrCreatePipeline(pipelineKey);
        _cachedPipelines.TryAdd(pipelineKey, pso);
        return pso;
    }

    /// <summary>
    /// Clears compiled shader and pipeline caches.
    /// Call when shaders are reloaded or modified.
    /// </summary>
    public void ClearCache()
    {
        _compiledVariants.Clear();
        _cachedPipelines.Clear();
    }

    /// <summary>
    /// Pre-warms the cache by compiling common variants.
    /// Can be called asynchronously during loading.
    /// </summary>
    public async Task WarmupVariantsAsync(ShaderProgram shader, KeywordSet[] variantConfigurations)
    {
        var tasks = new List<Task>();

        foreach (var keywords in variantConfigurations)
        {
            var variantKey = shader.CreateVariantKey(keywords);
            
            if (!_compiledVariants.ContainsKey(variantKey))
            {
                tasks.Add(Task.Run(() =>
                {
                    var compiled = _shaderCompiler.CompileVariant(variantKey, keywords);
                    _compiledVariants.TryAdd(variantKey, compiled);
                }));
            }
        }

        await Task.WhenAll(tasks);
    }
}

/// <summary>
/// Represents a single draw command with material and instance data.
/// </summary>
public struct DrawCommand
{
    public Material Material;
    public int PassIndex;
    public IntPtr VertexBuffer;
    public IntPtr IndexBuffer;
    public int IndexCount;
    public int InstanceCount;
    public IntPtr InstanceData;
}

/// <summary>
/// A batch of draw commands sharing the same PSO.
/// </summary>
public struct MaterialBatch
{
    public GraphicsPipelineKey PipelineKey;
    public IntPtr Pipeline;
    public DrawCommand[] DrawCommands;
}
